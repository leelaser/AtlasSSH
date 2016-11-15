﻿using PSAtlasDatasetCommands.Utils;
using SharpSvn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Find a job options in the ATLAS repo
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "ATLASMCJobOptions")]
    public class FindATLASMCJobOptions : PSCmdlet
    {
        /// <summary>
        /// The MC run number, if we want a specific run number
        /// </summary>
        [Parameter(HelpMessage = "Run number to fetch", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public int MCJobNumber;

        /// <summary>
        /// Get/Set the mc campaign.
        /// </summary>
        [Parameter(HelpMessage = "MC Campaign. Defaults to MC15 (MC10, MC12, etc. are possible)", Mandatory = false)]
        public string MCCampaign { get; set; }

        /// <summary>
        /// Force a refresh
        /// </summary>
        [Parameter(HelpMessage = "Force a refresh of the run number database. This can take a consderable amount of time.", Mandatory = false)]
        public SwitchParameter RefreshDSDaabase { get; set; }

        public FindATLASMCJobOptions()
        {
            MCCampaign = "MC15";
        }

        /// <summary>
        /// See if we need to re-download the catalog
        /// </summary>
        protected override void BeginProcessing()
        {
            if (RefreshDSDaabase.IsPresent || !DatabaseIsPresent())
            {
                RefreshDatabase();
            }
            base.BeginProcessing();
        }

        /// <summary>
        /// Check to see if the database is present.
        /// </summary>
        /// <returns></returns>
        private bool DatabaseIsPresent()
        {
            return GetDatabaseFilename().Exists;
        }

        /// <summary>
        /// Build the database filename.
        /// </summary>
        /// <returns></returns>
        private FileInfo GetDatabaseFilename()
        {
            var path = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLASMCJobOptionsDB", $"{MCCampaign}.txt");
            var finfo = new FileInfo(path);
            return finfo;
        }

        /// <summary>
        /// Get the full listing from svn of all datasets. This isn't fast (hence the updates).
        /// </summary>
        private void RefreshDatabase()
        {
            var pr = new ProgressRecord(1, "ATLAS MC Dataset Listing", "Fetching from CERN");
            pr.PercentComplete = 0;
            WriteProgress(pr);

            var svnRootDir = MCJobSVNHelpers.BuildTarget("share", MCCampaign);
            var listing = MCJobSVNHelpers.FetchListing(svnRootDir, new SvnListArgs() { Depth = SvnDepth.Infinity });

            pr.PercentComplete = 50;
            pr.StatusDescription = $"Saving to local datafile ({listing.Count} datasets found)";
            WriteProgress(pr);

            // Write it all out to the database file.
            var dbFileInfo = GetDatabaseFilename();
            if (!dbFileInfo.Directory.Exists)
            {
                dbFileInfo.Directory.Create();
            }
            using (var writer = dbFileInfo.CreateText())
            {
                foreach (var svnFile in listing)
                {
                    writer.WriteLine(svnFile.Name);
                }
            }

            pr.PercentComplete = 100;
            WriteProgress(pr);
        }

        /// <summary>
        /// Look through and return anything we need
        /// </summary>
        protected override void ProcessRecord()
        {
            // Loop through the file, looking for what we need.
            var jobText = MCJobNumber.ToString("D6");

            var matches = GetDatabaseFilename()
                .ReadLines()
                .Where(l => l.Contains(jobText))
                .Select(l => new ATLASMCJobInfo() { RunNumber = MCJobNumber, FileName = l });

            foreach (var item in matches)
            {
                WriteObject(item);
            }
        }

        /// <summary>
        /// The info for this file
        /// </summary>
        public class ATLASMCJobInfo
        {
            public ATLASMCJobInfo()
            {
            }

            public string FileName { get; internal set; }
            public int RunNumber { get; internal set; }
        }

    }
}
