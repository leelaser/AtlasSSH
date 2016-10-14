﻿using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Using our knowledge of the layout of the ATLAS job options svn package, fetch down the appropriate
    /// job options file.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ATLASMCJobOptions")]
    public class GetATLASMCJobOptions : PSCmdlet
    {
        /// <summary>
        /// The MC run number
        /// </summary>
        [Parameter(HelpMessage = "Run number to fetch", ValueFromPipeline = true ,Mandatory = true, Position = 1)]
        public int MCJobNumber;

        /// <summary>
        /// Get/Set the mc campaign.
        /// </summary>
        [Parameter(HelpMessage = "MC Campaign. Defaults to MC15 (MC10, MC12, etc. are possible)", Mandatory = false)]
        public string MCCampaign { get; set; }

        /// <summary>
        /// Get/Set if we are going to expand include lines.
        /// </summary>
        [Parameter(HelpMessage = "Should include files be expanded inline?", Mandatory = false)]
        public bool ExpandIncludeFiles { get; set; }

        public GetATLASMCJobOptions()
        {
            MCCampaign = "MC15";
            ExpandIncludeFiles = false;
        }

        /// <summary>
        /// Client we will use for quick access
        /// </summary>
        public SvnClient _client = new SvnClient();

        protected override void ProcessRecord()
        {
            // Make sure this is a 6 digit number as a string.
            var runID = MCJobNumber.ToString("D6");

            // First, for this run number, see if we can't figure out what DSID it is going to be
            // cached under.

            var DSIDDirectory = $"DSID{runID.Substring(0, 3)}xxx";
            var dsidListTarget = FetchListing(BuildTarget($"share/{DSIDDirectory}"));

            var myMCFile = dsidListTarget
                .Where(ads => ads.Name.Contains(runID))
                .ToArray();
            if (myMCFile.Length == 0)
            {
                var err = new ArgumentException($"Unable to find dataset run number for {runID}.");
                WriteError(new ErrorRecord(err, "NoSuchDataset", ErrorCategory.InvalidArgument, null));
            }
            if (myMCFile.Length != 1)
            {
                var err = new ArgumentException($"Found multiple dataset files for {runID} - giving up.");
                WriteError(new ErrorRecord(err, "MoreThanOneDataset", ErrorCategory.InvalidArgument, null));
            }
            var ds = myMCFile[0];

            // Next, fetch the file down.
            var targetTempPath = System.IO.Path.GetTempFileName();
            var args = new SvnExportArgs() { Overwrite = true };
            WriteVerbose($"Downloading svn file {ds.Uri.OriginalString}");
            _client.Export(BuildTarget(ds.Uri), targetTempPath, args);

            // Transfer process the lines
            var lines = new FileInfo(targetTempPath)
                .ReadLines()
                .SelectMany(l => l.ReplaceIncludeFiles(ExpandIncludeFiles));

            foreach (var line in lines)
            {
                WriteObject(line);
            }

            base.ProcessRecord();
        }

        /// <summary>
        /// Returns a list of the contents of the directory
        /// </summary>
        /// <param name="svnTarget"></param>
        /// <returns></returns>
        private Collection<SvnListEventArgs> FetchListing(SvnTarget svnTarget)
        {
            var result = new Collection<SvnListEventArgs>();
            WriteVerbose($"Fetching svn listing from {svnTarget.TargetName}");
            _client.GetList(svnTarget, out result);
            return result;
        }

        /// <summary>
        /// Build a target for a particular directory or file from the base given by the current
        /// arguments.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private SvnTarget BuildTarget(string path)
        {
            SvnTarget target;
            var url = $"https://svn.cern.ch/reps/atlasoff/Generators/{MCCampaign}JobOptions/trunk/{path}";
            if (!SvnTarget.TryParse(url, out target))
            {
                var err = new ArgumentException($"Unable to parse svn url {url}.");
                WriteError(new ErrorRecord(err, "SVNUrlError", ErrorCategory.InvalidArgument, null));
                throw err;
            }

            return target;
        }

        /// <summary>
        /// Build the target from a uri.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private SvnTarget BuildTarget (Uri path)
        {
            SvnTarget target;
            SvnTarget.TryParse(path.OriginalString, out target);
            return target;
        }
    }
}

internal static class PSAtlasMCJobOptionHelpers
{
    /// <summary>
    /// If we have an include, try to fetch the include files and download them.
    /// </summary>
    /// <param name="l"></param>
    /// <returns></returns>
    public static IEnumerable<string> ReplaceIncludeFiles(this string l, bool expandIncludeFiles)
    {
        if (!expandIncludeFiles)
        {
            yield return l;
        }

        var includeInfo = ExtractIncludeInformation(l);
        if (includeInfo == null)
        {
            yield return l;
        }
        else
        {
            yield return $"# -> {l}";
            var lines = ExtractIncludeContents(includeInfo)
                .SelectMany(ll => ll.ReplaceIncludeFiles(expandIncludeFiles));
                foreach (var ln in lines)
            {
                yield return ln;
            }
        }
    }


    /// <summary>
    /// Return the contents of an include file.
    /// </summary>
    /// <param name="includeInfo"></param>
    /// <returns></returns>
    private static IEnumerable<string> ExtractIncludeContents(IncludeInfo includeInfo)
    {
        yield return "hi";
    }

    /// <summary>
    /// Basic info about an include
    /// </summary>
    private class IncludeInfo
    {

    }

    private static IncludeInfo ExtractIncludeInformation(string l)
    {
        return new IncludeInfo();
    }
}
