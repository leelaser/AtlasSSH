﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.Linq;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class CERNTest
    {
        [TestMethod]
        public void CERNLocationName()
        {
            var c = CERN.GetLocation();
            Assert.AreEqual("CERN", c.Name);
        }

        [TestMethod]
        public void CERNLocationAtUW()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.washington.phys.washington.edu");
            var c = CERN.GetLocation();
            Assert.IsFalse(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = CERN.GetLocation();
            Assert.IsTrue(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtWhereWeAre()
        {
            AtlasWorkFlows.Utils.IPLocationTests.ResetIpName();
            var c = CERN.GetLocation();
            Console.WriteLine("Are we at CERN? : {0}", c.LocationIsGood());
        }

        [TestMethod]
        public void CERNFetchDatasetInfo()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = CERN.GetLocation();
            var dsinfo = c.GetDSInfo("bogus.dataset.version.1");
            Assert.IsNotNull(dsinfo);
            Assert.AreEqual(true, dsinfo.CanBeGenerated);
            Assert.AreEqual(false, dsinfo.IsLocal);
            Assert.AreEqual("bogus.dataset.version.1", dsinfo.Name);
            Assert.AreEqual(0, dsinfo.NumberOfFiles);
        }

        [TestMethod]
        public void CERNFetchDatasetGood()
        {
            // We can't really test the full cern fetch here, we'll do that elsewhere.
            // But do make sure we get something valid back here.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = CERN.GetLocation();
            Assert.IsNotNull(c.GetDS);
        }
    }
}