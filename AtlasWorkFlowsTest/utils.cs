﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest
{
    class utils
    {

        /// <summary>
        /// Build some dummy local files.
        /// </summary>
        /// <param name="rootDirName"></param>
        /// <returns></returns>
        public static DirectoryInfo BuildSampleDirectory(string rootDirName, Func<string[], string[]> fileFilter, params string[] dsnames)
        {
            // Start clean!
            var root = new DirectoryInfo(rootDirName);
            if (root.Exists)
            {
                root.Delete(true);
            }

            // Do any filtering that needs to be done.
            var goodFileNames = Enumerable.Range(1, 5)
                .Select(index => string.Format("file.root.{0}", index))
                .ToArray();

            if (fileFilter != null) {
                goodFileNames = fileFilter(goodFileNames);
            }

            // Now, create a dataset(s).
            foreach (var ds in dsnames)
            {
                var dsDir = new DirectoryInfo(Path.Combine(root.FullName, ds));
                dsDir.Create();
                var dsDirSub1 = new DirectoryInfo(Path.Combine(dsDir.FullName, "sub1"));
                dsDirSub1.Create();
                var dsDirSub2 = new DirectoryInfo(Path.Combine(dsDir.FullName, "sub2"));
                dsDirSub2.Create();

                int index = 0;
                foreach (var gfname in goodFileNames)
                {
                    var dsDirForFile = index < goodFileNames.Length / 2 ? dsDirSub1 : dsDirSub2;
                    WriteShortRootFile(new FileInfo(Path.Combine(dsDirForFile.FullName, gfname)));
                }
            }

            return root;
        }
        public static DirectoryInfo BuildSampleDirectory(string rootDirName, params string[] dsnames)
        {
            return BuildSampleDirectory(rootDirName, dsnames);
        }

        /// <summary>
        /// Write out a short empty file.
        /// </summary>
        /// <param name="fileInfo"></param>
        private static void WriteShortRootFile(FileInfo fileInfo)
        {
            using (var wr = fileInfo.Create())
            { }
        }
    }
}
