﻿using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Location info for CERN
    /// </summary>
    class LinuxWithWindowsReflector
    {
        /// <summary>
        /// Return the location for CERN
        /// </summary>
        /// <param name="props">Property bag to configure the object</param>
        /// <returns></returns>
        public static Location GetLocation(Dictionary<string, string> props)
        {
            var l = new Location();
            l.Name = props["Name"];
            l.LocationTests.Add(() => IPLocationTests.FindLocalIpName().EndsWith(props["DNSEndString"]));

            var fetcher = new LinuxFetcher(props["LinuxHost"], props["LinuxUserName"]);
            var dsfinder = new GRIDFetchToLinuxVisibleOnWindows(new DirectoryInfo(props["WindowsPath"]), fetcher, props["LinuxPath"]);

            l.GetDSInfo = name =>
            {
                var nfiles = dsfinder.CheckNumberOfFiles(name);
                return new DSInfo()
                {
                    Name = name,
                    IsLocal = filter => dsfinder.CheckIfLocal(name, filter),
                    CanBeGeneratedAutomatically = true,
                    ListOfFiles = () => dsfinder.ListOfFiles(name),
                    LocationProvider = l,
                };
            };

            l.GetDS = dsfinder.GetDS;

            return l;
        }
    }
}
