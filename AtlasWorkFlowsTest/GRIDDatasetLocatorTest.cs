﻿using AtlasWorkFlows;
using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasWorkFlowsTest
{
    [TestClass]
    public class GRIDDatasetLocatorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AskForWSDataset()
        {
            GRIDDatasetLocator.FetchDatasetUris(" ");
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            // Reset where we get the locations from!
            Locator._getLocations = null;
        }

        [TestMethod]
        public void DatasetAlreadyAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectory("DatasetAlreadyAtCERN", dsname);
            Locator._getLocations = () => GetLocal(d);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void DatasetAlreadyAtCERNLimited()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectory("DatasetAlreadyAtCERN", dsname);
            Locator._getLocations = () => GetLocal(d);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, fileFilter: fs => fs.OrderBy(f => f).Take(1).ToArray());

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
        }

        /// <summary>
        /// Generate a location that is local for this test environment.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, string>> GetLocal(DirectoryInfo location)
        {
            var r = new Dictionary<string, Dictionary<string, string>>();

            // CERN
            var c = new Dictionary<string, string>();
            c["DNSEndString"] = ".cern.ch";
            c["Name"] = "MyTestLocation";
            c["WindowsPath"] = location.FullName;
            c["LinuxPath"] = "/LLPData/GRIDDS";
            c["LocationType"] = "LinuxWithWindowsReflector";
            c["LinuxUserName"] = "gwatts";
            c["LinuxHost"] = "pcatuw4.cern.ch";

            r["MyTestLocation"] = c;

            return r;
        }

        [TestMethod]
        public void DatasetAlreadyLocal()
        {
            Assert.Inconclusive();
        }

        // ALso, need tests for small numbers of files.
    }
}
