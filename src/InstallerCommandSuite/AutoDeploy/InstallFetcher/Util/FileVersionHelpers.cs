using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallFetcher.Util
{
    /// <summary>
    /// This will read the File Version property of all files that are found in a given Path.
    /// It will populate a hash on file name / file version.
    /// </summary>
    public class VersionByPath
    {
        #region Read-Only Properties

        public Dictionary<string, string> VersionsByBuild { get; private set; }
        public string Path { get; private set; }

        #endregion

        #region Members

        private string Version { get; set; }

        #endregion

        #region Construction

        private VersionByPath()
        {
            VersionsByBuild = new Dictionary<string, string>();
        }

        public static VersionByPath BuildVersionFromPath(string path)
        {
            return ReadVersionFromPath(path);
        }

        #endregion

        #region Static Public Methods

        public static List<VersionByPath> GetLatestPathForSubfolder(string path)
        {
            var di = new DirectoryInfo(path);

            List<VersionByPath> versions = new List<VersionByPath>();

            var version = ReadVersionFromPath(path);
            if (!String.IsNullOrEmpty(version.Version))
            {
                versions.Add(version);
            }

            foreach (var subPath in di.GetDirectories())
            {
                versions.AddRange(GetLatestPathForSubfolder(subPath.FullName));
            }
            return versions;
        }

        #endregion

        #region Subroutines

        private static VersionByPath ReadVersionFromPath(string path)
        {
            List<string> arrHeaders = new List<string>();

            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;

            objFolder = shell.NameSpace(path);

            for (int i = 0; i < short.MaxValue; i++)
            {
                string header = objFolder.GetDetailsOf(null, i);
                if (String.IsNullOrEmpty(header))
                    break;
                arrHeaders.Add(header);
            }

            List<string> versions = new List<string>();
            VersionByPath vbp = new VersionByPath();

            foreach (Shell32.FolderItem2 item in objFolder.Items())
            {
                for (int i = 0; i < arrHeaders.Count; i++)
                {
                    if (arrHeaders[i].ToLower().StartsWith("file version") || arrHeaders[i].ToLower().StartsWith("name"))
                    {
                        string str = objFolder.GetDetailsOf(item, i);
                        versions.Add(str);
                    }
                }
            }


            for (int i = 0; i < versions.Count; i += 2)
            {
                if (versions[i].ToLower().Contains("ringtail"))
                {
                    vbp.VersionsByBuild.Add(versions[i], versions[i + 1]);
                }
            }

            foreach (var value in vbp.VersionsByBuild.Values)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    vbp.Version = value;
                }
            }

            vbp.Path = path;


            return vbp;
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return "Path: " + Path + " Version: " + Version;
        }

        #endregion
    }
}
