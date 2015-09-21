﻿using AtlasWorkFlows.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// Find out info about a GRID dataset, including making it available for access.
    /// </summary>
    public class GRIDDatasetLocator
    {
        /// <summary>
        /// Main routine to find all the URI's that point to a dataset.
        /// </summary>
        /// <param name="datasetname">The GRID dataset name to find pointers to.</param>
        /// <param name="fileFilter">Filter the potential list of files to be returned or downloaded (can really speed up things if you want one file in a large dataset).</param>
        /// <param name="locationFilter">Filter out the locations that we are allowed to fetch the file from.</param>
        /// <param name="statusUpdate">Downloads from the grid can take a long time. Status updates will be posted here if not null.</param>
        /// <returns></returns>
        public static Uri[] FetchDatasetUris(string datasetname, Action<string> statusUpdate = null, Func<string[],string[]> fileFilter = null, Func<string, bool> locationFilter = null)
        {
            var dsinfo = FetchDSInfo(datasetname, fileFilter, locationFilter);

            // And delegate all the rest of our work to fetching.
            return dsinfo.LocationProvider.GetDS(dsinfo, statusUpdate, fileFilter);
        }

        /// <summary>
        /// Find the DSInfo with the best possible location provider (local, remote, etc.).
        /// </summary>
        /// <param name="datasetname">The GRID dataset name to find pointers to.</param>
        /// <param name="fileFilter">Filter the potential list of files to be returned or downloaded (can really speed up things if you want one file in a large dataset).</param>
        /// <param name="locationFilter">Filter out the locations that we are allowed to fetch the file from.</param>
        /// <returns></returns>
        public static DSInfo FetchDSInfo(string datasetname, Func<string[], string[]> fileFilter, Func<string, bool> locationFilter)
        {
            // Basic parameter checks
            if (string.IsNullOrWhiteSpace(datasetname))
            {
                throw new ArgumentException("Dataset name is empty");
            }

            // Get a location so we can see if we can fetch the dataset
            var locator = new Locator();
            var locationList = locator.FindBestLocations();
            if (locationFilter != null)
            {
                locationList = locationList.Where(loc => locationFilter(loc.Name)).ToArray();
            }
            if (locationList.Length == 0)
            {
                throw new InvalidOperationException(string.Format("There are no valid ways to download dataset '{0}' from your current location.", datasetname));
            }

            // Get the info for each dataset.
            var allDSInfo = (from loc in locationList
                             let dsi = loc.GetDSInfo(datasetname)
                             select new
                             {
                                 dsinfo = dsi,
                                 islocal = dsi.IsLocal(fileFilter)
                             }).ToArray();

            // First, if anyone has something local, then we should grab that.
            var localDS = allDSInfo.Where(d => d.islocal).OrderByDescending(d => d.dsinfo.LocationProvider.Priority).ToArray();

            // Ok, now we need the ones that can generate it.
            if (localDS.Length == 0)
            {
                localDS = allDSInfo.Where(d => d.dsinfo.CanBeGeneratedAutomatically).OrderByDescending(d => d.dsinfo.LocationProvider.Priority).ToArray();
            }

            // Did we strike out?
            var locationInfo = localDS.FirstOrDefault();
            if (locationInfo == null)
            {
                var locationListText = "";
                foreach (var l in locationList)
                {
                    locationListText += l.Name + " ";
                }
                throw new ArgumentException(string.Format("Do not know how to generate the dataset '{0}' at any of the locations {1} (which are the only ones working where the computer is located)", datasetname, locationListText));
            }
            var dsinfo = locationInfo.dsinfo;
            return dsinfo;
        }
    }
}
