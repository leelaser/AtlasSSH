﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    public static class UriUtils
    {
        /// <summary>
        /// Will extract the dataset from a gridds Uri
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string DataSetName(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"Expected a gridds Uri, but got a {uri.Scheme} one instead.");
            }

            // Extract the authority - but we can't do it case insensitive.
            var endOfScheme = uri.OriginalString.IndexOf("//");
            var endOfAuthority = uri.OriginalString.IndexOf("/", endOfScheme + 2);

            return uri.OriginalString.Substring(endOfScheme + 2, endOfAuthority - endOfScheme-2);
        }

        /// <summary>
        /// Return the dataset filename in this uri.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string DataSetFileName(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (uri.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"Expected a gridds Uri, but got a {uri.Scheme} one instead.");
            }

            return uri.Segments.Last();
        }
    }
}
