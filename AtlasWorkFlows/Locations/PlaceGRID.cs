﻿using AtlasSSH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// We represent a GRID endpoint, that we can access via an ssh connection.
    /// We are paired with a local Linux end point - basically a place where we can
    /// copy files and store them.
    /// </summary>
    class PlaceGRID : IPlace
    {
        private PlaceLinuxRemote _linuxRemote;

        /// <summary>
        /// Initialize the GRID location.
        /// </summary>
        /// <param name="name">Name of the GRID re-pro</param>
        /// <param name="linuxRemote">All GRID sites are paired with some sort of Linux local access where their downloads are sent to.</param>
        public PlaceGRID(string name, PlaceLinuxRemote linuxRemote)
        {
            Name = name;
            _linuxRemote = linuxRemote;
            _connection = new Lazy<ISSHConnection>(() => InitConnection(null));
        }

        /// <summary>
        /// The connection to our remote end-point
        /// </summary>
        private Lazy<ISSHConnection> _connection;

        private ISSHConnection InitConnection(Action<string> statusUpdater)
        {
            if (statusUpdater != null)
                statusUpdater("Setting up GRID Environment");
            var r = new SSHConnection(_linuxRemote.RemoteHost, _linuxRemote.RemoteUsername);
            r
                .setupATLAS()
                .setupRucio(_linuxRemote.RemoteUsername)
                .VomsProxyInit("atlas");

            return r;
        }

        /// <summary>
        /// We are a GRID site, so a high data tier number.
        /// </summary>
        public int DataTier { get { return 100; } }

        /// <summary>
        /// No local files can be reached here!
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// The name of the GRID access point.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// We can't be copied to... yet.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return true; } }

        /// <summary>
        /// We can only source a copy to our partner place!
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            return destination == _linuxRemote;
        }

        /// <summary>
        /// Since we can't create a GRID dataset, this is not supported!
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public void CopyDataSetInfo(string dsName, string[] files)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// We can't copy data into the GRID.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// Download files from the grid to the linux local site.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a list of files from the GRID for a dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns>List of the files, with namespace removed.</returns>
        /// <remarks>
        /// Consider all datasets on the GRID frozen, so once they have been downloaded
        /// we cache them locally.
        /// </remarks>
        public string[] GetListOfFilesForDataset(string dsname)
        {
            return NonNullCacheInDisk("PlaceGRIDDSCatalog", dsname, () =>
            {
                try
                {
                    return _connection.Value.FilelistFromGRID(dsname)
                        .Select(f => f.Split(':').Last())
                        .ToArray();
                } catch (DatasetDoesNotExistException)
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Since we aren't visible on windows, this is just not possible.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be directly accessed from Windows - so no local path names!");
        }

        /// <summary>
        /// Check to see if a particular file exists. As long as that file is a member
        /// of the dataset, then it will as the GRID has EVERYTHING. ;-)
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u)
        {
            // Get the list of files for the dataset and just look.
            var files = GetListOfFilesForDataset(u.Authority);
            return files == null
                ? false
                : files.Contains(u.Segments.Last());
        }
    }
}
