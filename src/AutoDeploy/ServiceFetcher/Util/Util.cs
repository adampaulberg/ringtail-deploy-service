using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceFetcher.Util
{
    public class SimpleFileWriter
    {
        public static void Write(string fileName, List<string> s)
        {
            using (StreamWriter wr = new StreamWriter(fileName))
            {
                foreach (string str in s)
                {
                    wr.WriteLine(str);
                }
            }
        }
    }

    public class SimpleFileReader
    {
        public static List<string> Read(string fileName)
        {
            List<string> s = new List<string>();

            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
            {

                using (StreamReader stream = new StreamReader(fileName))
                {
                    string input = null;
                    while ((input = stream.ReadLine()) != null)
                    {
                        s.Add(input);
                    }
                }
            }

            return s;
        }
    }


    public static class DirectoryInfoExtensions
    {
        /// <summary>
        /// Checks whether the folder exists and has files.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static bool HasFiles(this DirectoryInfo folder)
        {
            return folder.Exists ? folder.GetFiles().Length != 0 : false;
        }

        /// <summary>
        /// Returns the folder, or if passed in a suffix, the subfolder with that name.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="suffix">optional param.  send in null if you want to repeat the folder back to you.</param>
        /// <returns></returns>
        public static DirectoryInfo NormalizeFolderBySuffix(this DirectoryInfo folder, string suffix)
        {
            DirectoryInfo working = folder;
            if (!String.IsNullOrEmpty(suffix))
            {
                working = new DirectoryInfo(folder.FullName + @"\" + suffix);
            }

            return working;
        }
    }

    public static class ListExtensions
    {
        public static void AppendToFront<T>(this List<T> list, T item)
        {
            list.Reverse();
            list.Add(item);
            list.Reverse();
        }
    }

}
