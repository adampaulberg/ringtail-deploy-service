using Deployer.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deployer.App
{
    public class RegistryReaderScrubber
    {
        public static void EndToEnd(string fileName, string outFile)
        {
            string folder = Path.GetDirectoryName(fileName);
            var data = SimpleFileReader.Read(fileName);
            int getIndexOfHKey = FindIndexesOfItems(data, "HKEY_LOCAL");

            if (getIndexOfHKey > 0)
            {
                int start = GetIndexOfStartingBlock(data, getIndexOfHKey);
                int end = GetEndingBlockOfIndex(data, getIndexOfHKey);
                string key = @"SOFTWARE\Microsoft\IIS Extensions\MSDeploy\2";
                string value = "InstallPath_x86";
                string replacement = GetPathForRegKey(key, value);
                if (replacement == string.Empty)
                {
                    replacement = GetPathForRegKey(key, "InstallPath");
                }
                replacement = "strValue = \"" + replacement + "\"";
                var listReplacement = new List<string>();
                listReplacement.Add(replacement);
                listReplacement.Add("Return = 0");

                if (start > 0 && end < data.Count)
                {
                    var newText = ReplaceBlockWithNewText(data, start, end, listReplacement);
                    SimpleFileWriter.Write(folder + @"\" + outFile, newText);
                }
            }
        }

        public static string GetPathForRegKey(string key, string value)
        {
            var baseRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            RegistryKey keys = baseRegistryKey.OpenSubKey(key, true);


            var asLinst = keys.GetValueNames().ToList();

            foreach (var x in asLinst)
            {
                if (x == value)
                {
                    return keys.GetValue(value).ToString();
                }
            }

            return string.Empty;
        }

        public static List<string> ReplaceBlockWithNewText(List<string> str, int removeStart, int removeEnd, List<string> replacement)
        {
            List<string> masterString = str.GetRange(0, removeStart);

            masterString.AddRange(replacement);

            var endRange = str.GetRange(removeEnd + 1, str.Count - removeEnd - 1);

            masterString.AddRange(endRange);

            return masterString;
        }

        public static int FindIndexesOfItems(List<string> str, string key)
        {
            int findKey = str.FindIndex(x => x.Contains(key));
            return findKey;
        }

        public static int GetIndexOfStartingBlock(List<string> str, int index)
        {
            for (int i = index; i >= 0; i--)
            {
                if (str[i].StartsWith("'"))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int GetEndingBlockOfIndex(List<string> str, int index)
        {
            int lastLine = -1;
            int returnStatementIndex = -1;
            for (int i = index; i < str.Count; i++)
            {
                if (str[i].ToLower().StartsWith("return"))
                {
                    returnStatementIndex = i;
                    break;
                }
            }

            if (returnStatementIndex > 0)
            {
                lastLine = returnStatementIndex;
                if (str[returnStatementIndex].ToLower().EndsWith("_"))
                {
                    for (int i = returnStatementIndex; i < str.Count; i++)
                    {
                        lastLine = i;
                        if (!str[i].EndsWith("_"))
                        {
                            break;
                        }
                    }
                }
            }

            return lastLine;
        }


    }
}
