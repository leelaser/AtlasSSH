﻿using AtlasSSH;
using AtlasWorkFlows.Jobs;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    [Cmdlet(VerbsCommon.Get, "AMIDatasetInfo")]
    public class GetAMIDatasetInfo : PSCmdlet
    {
        /// <summary>
        /// Track the listener for verbose messages
        /// </summary>
        private PSListener _listener;

        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "AMI dataset to get back the info for", ValueFromPipeline = true, Position = 1)]
        public string DatasetName { get; set; }

        /// <summary>
        /// Setup!
        /// </summary>
        protected override void BeginProcessing()
        {
            // Setup for verbosity if we need it.
            _listener = new PSListener(this);
            Trace.Listeners.Add(_listener);

            base.BeginProcessing();
        }

        /// <summary>
        /// Cach a connection to a remote machine for linux command line access.
        /// One day can we use the local bash shell? :-)
        /// </summary>
        private SSHConnection _connection = null;

        /// <summary>
        /// Create the connection, and setup the appropriate stuff for using AMI.
        /// </summary>
        /// <returns></returns>
        private SSHConnection GetConnection()
        {
            if (_connection != null)
                return _connection;

            // Establish the connection
            var sm = JobParser.GetSubmissionMachine();
            var connection = new SSHConnection(sm.MachineName, sm.Username);

            // Setup for using ami.
            connection.Apply(() => DisplayStatus("Setting up ATLAS"))
                .setupATLAS()
                .Apply(() => DisplayStatus("Setting up pyAMI"))
                .ExecuteCommand("lsetup pyami")
                .Apply(() => DisplayStatus("Acquiring GRID credentials"))
                .VomsProxyInit("atlas", failNow: () => Stopping);

            // And the connection is now ready for use!
            _connection = connection;
            return _connection;
        }

        /// <summary>
        /// Get the informaiton for each dataset we are looking at.
        /// </summary>
        protected override void ProcessRecord()
        {
            var c = GetConnection();

            // Clean up the name. If it has a Rucio scope on it, then we want to ignore that.
            var dsName = DatasetName.Contains(":") ? DatasetName.Substring(DatasetName.IndexOf(":") + 1) : DatasetName;

            // Get the dump from the command, which is a set of dictionary pairings
            var responses = new List<string>();
            DisplayStatus($"Getting info for {dsName}");
            c.ExecuteCommand($"ami show dataset info {dsName}", l => responses.Add(l));

            // Parse everything into name-value pairs.
            var dict = responses
                .Select(l => l.Split(new[] { ':' }, 2))
                .ToDictionary(l => l[0].Trim(), l => l[1].Trim());

            this.WriteObject(dict);
            base.ProcessRecord();
        }

        /// <summary>
        /// Shut down the connection.
        /// </summary>
        protected override void EndProcessing()
        {
            // We can let it close.
            //if (_connection != null)
            //{
            //    _connection.Dispose();
            //    _connection = null;
            //}

            // Turn off verbosity
            Trace.Listeners.Remove(_listener);
            base.EndProcessing();
        }

        /// <summary>
        /// Generate a display message
        /// </summary>
        /// <param name="message"></param>
        private void DisplayStatus(string message)
        {
            var pr = new ProgressRecord(1, "Getting AMI Info", message);
            WriteProgress(pr);
        }

    }
}