﻿using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest.Location
{
    class UtilsForBuildingLinuxDatasets
    {
        /// <summary>
        /// internet name of machine we will be accessing to run tests against.
        /// </summary>
        public string RemoteHostInfo;

        /// <summary>
        /// Absolute path on that machine of where the repro is to be put.
        /// </summary>
        public string RemotePath;

        /// <summary>
        /// Initalize to a location.
        /// </summary>
        /// <param name="testMachineName"></param>
        public UtilsForBuildingLinuxDatasets(string testMachineName = "LinuxRemoteTest")
        {
            // Clean out all caches.
            DiskCache.RemoveCache("PlaceLinuxDatasetFileList");
            DiskCache.RemoveCache("PlaceGRIDDSCatalog");

            // Load parameters that we can use to access the test machine.
            var cFile = new FileInfo("location_test_params.txt");
            Assert.IsTrue(cFile.Exists, $"Unable to locate test file {cFile.FullName}");
            var p = Config.ParseConfigFile(cFile);
            Assert.IsTrue(p.ContainsKey("LinuxRemoteTest"), "Unable to find machine info in LinuxRemoteTest");
            var lrtInfo = p[testMachineName];

            RemoteHostInfo = lrtInfo["LinuxHost"];
            RemotePath = lrtInfo["LinuxPath"];
        }

        /// <summary>
        /// The connection to the remote computer where we build and destroy datasets.
        /// </summary>
        private ISSHConnection Connection { get; set; }

        /// <summary>
        /// Close down the ssh connection
        /// </summary>
        public void TestCleanup()
        {
            if (Connection != null)
            {
                Connection.Dispose();
            }
        }

        public void CreateDS(string ds, params string[] filenames)
        {
            var dsDir = $"{RemotePath}/{ds}";
            var dsFileDir = $"{dsDir}/files";

            // Create the directories.
            Connection.ExecuteLinuxCommand($"mkdir -p {dsFileDir}");

            // For the files create them and add them to the whole thing.
            foreach (var f in filenames)
            {
                Connection
                    .ExecuteLinuxCommand($"echo {f} >> {dsDir}/aa_dataset_complete_file_list.txt")
                    .ExecuteLinuxCommand($"echo hi > {dsFileDir}/{f}");
            }
        }

        /// <summary>
        /// Remove a file in a dataset
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public void RemoveFileInDS(string dsname, string fname)
        {
            Connection.ExecuteLinuxCommand($"rm {RemotePath}/{dsname}/files/{fname}");
        }

        /// <summary>
        /// Add a file to the dataset - the physical file.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        internal void AddFileToDS(string dsname, string fname)
        {
            Connection.ExecuteLinuxCommand($"touch {RemotePath}/{dsname}/files/{fname}");
        }

        /// <summary>
        /// Create a fresh, clean, repro on the remote machine
        /// </summary>
        public void CreateRepro(string remote_path = null)
        {
            if (remote_path == null)
                remote_path = RemotePath;

            // Make sure no one is debing a dick by accident.
            Assert.AreNotEqual(".", remote_path);
            Assert.IsFalse(string.IsNullOrWhiteSpace(remote_path));
            Assert.IsFalse(remote_path.Contains("*"));

            // Create the new repro
            Connection = new SSHConnectionTunnel(RemoteHostInfo);
            Connection.ExecuteLinuxCommand($"rm -rf {remote_path}")
                .ExecuteLinuxCommand($"mkdir -p {remote_path}");
        }

        /// <summary>
        /// Get a list of all files in the repro.
        /// </summary>
        /// <param name="repro"></param>
        /// <returns></returns>
        public string[] GetAllFilesInRepro(string repro)
        {
            var lines = new List<string>();
            Connection.ExecuteLinuxCommand($"find {RemotePath}/{repro} -print", l => lines.Add(l));
            return lines.ToArray();
        }
    }
}
