﻿using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;
using System.IO;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// This is a remote linux machine.
    /// - Files are accessible only as a repository - copy in or out
    /// - If the disks are availible on SAMBA, then create a WindowsLocal as well
    /// - If GRID can download here, then that should also create a new place.
    /// </summary>
    class PlaceLinuxRemote : IPlace, ISCPTarget
    {
        /// <summary>
        /// The remote host we are connected to
        /// </summary>
        public string RemoteHost { get; private set; }

        private string _remote_path;
        
        /// <summary>
        /// Username that we use to access the remote host.
        /// </summary>
        public string RemoteUsername { get; private set; }

        /// <summary>
        /// Create a new repro located on a Linux machine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remote_ipaddr"></param>
        /// <param name="username"></param>
        /// <param name="remote_path"></param>
        public PlaceLinuxRemote(string name, string remote_ipaddr, string username, string remote_path)
        {
            Name = name;
            this.RemoteHost = remote_ipaddr;
            this._remote_path = remote_path;
            this.RemoteUsername = username;
            DataTier = 50;

            _connection = new Lazy<SSHConnection>(() => new SSHConnection(RemoteHost, RemoteUsername));
        }

        /// <summary>
        /// Get/Set the data tier. We default to 50 as we have no local access.
        /// </summary>
        public int DataTier { get; set; }

        /// <summary>
        /// We do not implement any local access from this end point
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// Get the name of this repro.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Does not need confirmation to use.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return false; } }

        /// <summary>
        /// The machine name for accessing this SCP end point.
        /// </summary>
        public string SCPMachineName { get { return RemoteHost; } }

        /// <summary>
        /// The username for accessing this machine via SCP
        /// </summary>
        public string SCPUser { get { return RemoteUsername; } }

        /// <summary>
        /// We can start a copy from here to other places that have a SSH destination availible.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            var scpTarget = destination as ISCPTarget;
            return !(scpTarget == null || !scpTarget.SCPIsVisibleFrom(RemoteHost));
        }

        /// <summary>
        /// Start a copy from the <paramref name="origin"/> via SSH.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris)
        {
            // Make sure we have a target we can deal with for the copy.
            var scpTarget = origin as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {origin.Name} is not an SCP target.");
            }

            // Do things a single dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // Get the remote user, path, and password.
                var remoteUser = scpTarget.SCPUser;
                var remoteMachine = scpTarget.SCPMachineName;
                var passwd = GetPasswordForHost(remoteMachine, remoteUser);

                // Get the catalog over
                CopyDataSetInfo(fsGroup.Key, origin.GetListOfFilesForDataset(fsGroup.Key));

                // The file path where we will store it all
                var destLocation = GetPathToCopyFiles(fsGroup.Key);

                // Next, queue up the copies, one at a time.
                foreach (var f in fsGroup)
                {
                    var remoteLocation = scpTarget.GetSCPFilePath(f);
                    _connection.Value.ExecuteLinuxCommand($"echo {passwd} | scp {remoteUser}@{remoteMachine}:{remoteLocation} {destLocation}");
                }
            }
        }

        /// <summary>
        /// Returns the dataset path
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public string GetLinuxDatasetDirectoryPath(string dsname)
        {
            return $"{_remote_path}/{dsname}";
        }

        /// <summary>
        /// Extract a password in the standard way.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        private string GetPasswordForHost(string host, string username)
        {
            var sclist = new CredentialSet(host);
            var passwordInfo = sclist.Load().Where(c => c.Username == username).FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, username));
            }

            return passwordInfo.Password;
        }

        /// <summary>
        /// Start a copy pushing data to a particular destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            // Make sure we have something we can deal with.
            var scpTarget = destination as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {destination.Name} is not an SCP target.");
            }

            // Do things one dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // Get the remote user, path, and password.
                var remoteUser = scpTarget.SCPUser;
                var remoteMachine = scpTarget.SCPMachineName;
                var passwd = GetPasswordForHost(remoteMachine, remoteUser);

                // Get the catalog over
                destination.CopyDataSetInfo(fsGroup.Key, GetListOfFilesForDataset(fsGroup.Key));

                // The file path where we will store it all
                var destLocation = scpTarget.GetPathToCopyFiles(fsGroup.Key);

                // Next, queue up the copies, one at a time.
                foreach (var f in fsGroup)
                {
                    var localFilePath = GetSCPFilePath(f);
                    _connection.Value.ExecuteLinuxCommand($"echo {passwd} | scp {localFilePath} {remoteUser}@{remoteMachine}:{destLocation}");
                }
            }
        }

        /// <summary>
        /// Track the connection to the remote computer. Once opened we keep it open.
        /// </summary>
        private Lazy<SSHConnection> _connection;

        /// <summary>
        /// The full list of all files that belong to a particular dataset. This is regardless
        /// of weather or not the files are in this repro.
        /// </summary>
        /// <param name="dsname">Name of the dataset we are looking at</param>
        /// <returns>List of the files in the dataset, or null if the dataset is not known in this repro</returns>
        public string[] GetListOfFilesForDataset(string dsname)
        {
            // The list of files for a dataset can't change over time, so we can
            // cache it locally.
            return NonNullCacheInDisk("PlaceLinuxDatasetFileList", dsname, () =>
            {
                var files = new List<string>();
                try
                {
                    _connection.Value.ExecuteLinuxCommand($"cat {_remote_path}/{dsname}/aa_dataset_complete_file_list.txt", l => files.Add(l));
                } catch (LinuxCommandErrorException) when (files.Count > 0 && files[0].Contains("aa_dataset_complete_file_list.txt: No such file or directory"))
                {
                    // if there was an error accessing it, then it isn't there... Lets hope.
                    return null;
                }

                return files.Select(f => f.Split(':').Last()).ToArray();
            });
        }

        /// <summary>
        /// Since we aren't local, we never return file locations. Always throw.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            throw new NotSupportedException($"The Linux dataset repository {Name} can't furnish local paths as it is on a remote machine!");
        }

        /// <summary>
        /// Track the locations of all the files for a particular dataset.
        /// </summary>
        private InMemoryObjectCache<string[]> _filePaths = new InMemoryObjectCache<string[]>();

        /// <summary>
        /// Fetch and return the full file paths of every file in the dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private string[] GetAbosluteLinuxFilePaths(string dsname)
        {
            return _filePaths.GetOrCalc(dsname, () =>
            {
                var files = new List<string>();
                _connection.Value.ExecuteLinuxCommand($"find {_remote_path}/{dsname} -print", l => files.Add(l));
                return files.ToArray();
            });
        }

        /// <summary>
        /// See if we have a particular file in our dataset
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u)
        {
            // Simpmle check.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"The uri '{u.OriginalString}' is not a gridds:// uri - can't map it to a file!");
            }

            return GetAbosluteLinuxFilePaths(u.DatasetName())
                .Select(f => f.Split('/').Last())
                .Where(f => f == u.Segments.Last())
                .Any();
        }

        /// <summary>
        /// We only represent a node that is visible from everywhere.
        /// </summary>
        /// <param name="internetLocation"></param>
        /// <returns></returns>
        public bool SCPIsVisibleFrom(string internetLocation)
        {
            return true;
        }

        /// <summary>
        /// Get a path for a file.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public string GetSCPFilePath(Uri f)
        {
            var file = GetAbosluteLinuxFilePaths(f.DatasetName())
                .Where(rf => rf.EndsWith("/" + f.Segments.Last()))
                .FirstOrDefault();
            if (file == null)
            {
                throw new ArgumentException($"Place {Name} was expected to have file {f}, but does not!");
            }

            return file;
        }

        /// <summary>
        /// Make this dataset known to us at the Linux repro
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filesInDataset">List of files that are in this dataset.</param>
        public void CopyDataSetInfo(string key, string[] filesInDataset)
        {
            if (filesInDataset == null)
            {
                throw new ArgumentNullException("List of files for the dataset can't be null");
            }

            // Prep for running
            var dsDir = $"{_remote_path}/{key}";
            var infoFile = $"{dsDir}/aa_dataset_complete_file_list.txt";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {dsDir}");
            _connection.Value.ExecuteLinuxCommand($"rm -rf {infoFile}");

            // Just add every file in.
            foreach (var f in filesInDataset)
            {
                _connection.Value.ExecuteLinuxCommand($"echo {f} >> {infoFile}");
            }
        }

        /// <summary>
        /// Return the path where files can be copied. It must be ready for that too.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public string GetPathToCopyFiles(string dsname)
        {
            // Get the location, and make sure it exists.
            var copiedPath = $"{_remote_path}/{dsname}/copied";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {copiedPath}");

            // Assume we are going to update - so clear the internal cache.
            _filePaths.Remove(dsname);

            return copiedPath;
        }

        /// <summary>
        /// Copy files from here down.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="ourpath"></param>
        /// <remarks>
        /// Use our scp connection to do this.
        /// </remarks>
        public void CopyFromRemoteToLocal(string dsName, string[] files, DirectoryInfo ourpath)
        {
            // Turn them into linux file locations by doing matching. The files should be identical.
            var linuxLocations = GetAbosluteLinuxFilePaths(dsName);
            var linuxFiles = files
                .Select(f => linuxLocations.Where(lx => lx.EndsWith("/" + f)).FirstOrDefault())
                .Throw<string>(s => s == null, s => new DatasetFileNotLocalException($"File '{s}' is not in place {Name}, so we can't copy it locally!"));

            foreach(var lx in linuxFiles)
            {
                _connection.Value.CopyRemoteFileLocally(lx, ourpath);
            }
        }

        /// <summary>
        /// Copy a file from a local location to a remote location via SCP.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public void CopyFromLocalToRemote(string dsName, IEnumerable<FileInfo> files)
        {
            // The the dest direction setup
            var lxlocation = GetLinuxDatasetDirectoryPath(dsName);
            var copiedLocation = $"{lxlocation}/copied";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {copiedLocation}");

            // Now copy the files
            foreach (var f in files)
            {
                _connection.Value.CopyLocalFileRemotely(f, $"{copiedLocation}/{f.Name}");
            }
        }
    }
}
