using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerService.Helpers
{
    internal class FileHelpers
    {
        public static string ReadFolder()
        {
            string results = string.Empty;
            var workingDirectory = string.Empty;

            try
            {
                workingDirectory = EnvironmentInfo.GetAutoDeploySuiteFolder();
                DirectoryInfo di = new DirectoryInfo(workingDirectory);

                var asList = di.GetFiles().ToList();
                var files = new List<string>();
                foreach (var x in asList)
                {
                    files.Add(x.Name);
                }

                results = ConvertToString(files);
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }

            return results;
        }

        public static string ReadConfig(string fileName)
        {
            string results = string.Empty;
            var workingDirectory = string.Empty;

            try
            {
                workingDirectory = EnvironmentInfo.GetAutoDeploySuiteFolder();
                FileInfo fi = new FileInfo(workingDirectory + fileName);

                if (fi.Exists)
                {
                    var s = SimpleFileReader.Read(workingDirectory + fileName);

                    results += ConvertToString(s);
                }
                else
                {
                    results = "Cannot find " + workingDirectory + "\\" + fileName;
                }
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
            }

            return results;
        }

        public static List<string> ReadConfigAsData(string fileName)
        {
            var results = new List<string>();
            var workingDirectory = string.Empty;

            try
            {
                workingDirectory = EnvironmentInfo.GetAutoDeploySuiteFolder();
                FileInfo fi = new FileInfo(workingDirectory + fileName);

                if (fi.Exists)
                {
                    results = SimpleFileReader.Read(workingDirectory + fileName);
                }
            }
            catch
            {
                // oops.
            }

            return results;
        }

        public static string ReadConfig(string fileName, string path)
        {
            string results = string.Empty;
            var workingDirectory = string.Empty;

            try
            {
                workingDirectory = path;
                FileInfo fi = new FileInfo(workingDirectory + fileName);

                if (fi.Exists)
                {
                    var s = SimpleFileReader.Read(workingDirectory + fileName);

                    results += ConvertToString(s);
                }
                else
                {
                    results = "Cannot find " + fileName;
                }
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
            }

            return results;
        }

        //public static string ChangeConfigItem(string fileName, string key, string value)
        //{
        //    string result = "Unknown";
        //    var filePath = string.Empty;
        //    FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
        //    if (fi.Exists)
        //    {
        //        var x = SimpleFileReader.Read(EnvironmentInfo.CONFIG_LOCATION);
        //        filePath = EnvironmentInfo.GetAutoDeploySuiteFolder() + fileName;

        //        fi = new FileInfo(filePath);

        //        if (fi.Exists)
        //        {
        //            result = RAW_UpdateFileAtPath(filePath, key, value);
        //        }
        //        else
        //        {
        //            result = "File not found: " + filePath;
        //        }
        //    }

        //    return result;
        //}

        public static string ChangeConfigItemWithWildcards(string fileName, string key, string value, bool deleteAllowed = false)
        {
            string result = "Unknown";
            var filePath = string.Empty;
            FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);

            if (deleteAllowed == false && key.StartsWith("&&"))
            {
                throw new InvalidOperationException("Deleting configuration is not allowed via this API");
            }

            if (fi.Exists)
            {
                var x = SimpleFileReader.Read(EnvironmentInfo.CONFIG_LOCATION);
                filePath = EnvironmentInfo.GetAutoDeploySuiteFolder();

                result = ChangeFiles(fileName, key, value, filePath);
            }

            return result;
        }

        public static string ChangeFiles(string fileName, string key, string value, string filePath)
        {
            string result = string.Empty;
            List<string> files = new List<string>();

            if (fileName.StartsWith("*"))
            {
                files.Add("volitleData.config");
                files.Add("roles.config");
            }
            else
            {
                files.Add(fileName);
            }

            foreach (var file in files)
            {
                string totalFilePath = filePath + file;
                FileInfo fi = new FileInfo(totalFilePath);

                if (fi.Exists)
                {
                    var s = SimpleFileReader.Read(totalFilePath);
                    var helperResult = FileHelperResult.UpsertWildcardKeys(s, key, value);

                    if (helperResult.IsSuccessful)
                    {
                        SimpleFileWriter.Write(totalFilePath, helperResult.NewFile);
                        result += "Success";
                    }
                    else
                    {
                        foreach (var message in helperResult.Messages)
                        {
                            result += message + " ";
                        }
                    }
                }
                else
                {
                    result += "File not found: " + totalFilePath;
                }
            }

            return result;
        }

        /// <summary>
        /// Do not call this with user-input values for the file path.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="filePath"></param>
        internal static string RAW_UpdateFileAtPath(string filePath, string key, string value)
        {
            List<string> newFile = new List<string>();
            var s = SimpleFileReader.Read(filePath);

            bool found = false;

            foreach (var str in s)
            {
                string modified = str;

                char delimiter = str.Contains('=') ? '=' : '|';

                if (str.Contains(key))
                {
                    modified = ReplaceValue(value, str, delimiter);
                    found = true;
                }
                newFile.Add(modified);
            }

            if (!found)
            {
                return "That key was not found, and this service does not allow new keys to be added, please contact support if you need a new key added.";
            }

            SimpleFileWriter.Write(filePath, newFile);

            return "Success";
        }

        private static string ReplaceValue(string value, string str, char delimiter)
        {
            string[] split = str.Split(delimiter);
            return split[0] + delimiter + "\"" + value + "\"";
        }

        public static string ConvertToString(List<string> str)
        {
            var s = string.Empty;
            foreach (var x in str)
            {
                s += "<p>" + x + "</p>";
            }
            return s;
        }

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
    }

    public class SimpleFileReader
    {
        public static List<string> Read(string fileName)
        {
            List<string> s = new List<string>();
            using (StreamReader stream = new StreamReader(fileName))
            {
                string input = null;
                while ((input = stream.ReadLine()) != null)
                {
                    s.Add(input);
                }
            }

            return s;
        }

        public static List<string> SafeRead(string filepath)
        {
            var results = new List<string>();
            var file = new FileInfo(filepath);
            if (file.Exists)
            {
                results = SimpleFileReader.Read(filepath);
            }
            return results;
        }
    }

    public class FileHelperResult
    {
        public static FileHelperResult UpsertWildcardKeys(List<string> contents, string keyOrPattern, string value)
        {
            FileHelperResult fhr = new FileHelperResult();
            fhr.NewFile = new List<string>();
            fhr.Messages = new List<string>();
            fhr.IsSuccessful = false;

            

            try
            {
                IReplacementRule rule = GetRule(contents, keyOrPattern);
                var result = rule.RunRule(contents, keyOrPattern, value);
                fhr.NewFile = result;
                fhr.IsSuccessful = true;
            }
            catch (Exception ex)
            {
                fhr.IsSuccessful = false;
                fhr.Messages = new List<string>();
                fhr.Messages.Add(ex.Message);
                fhr.Messages.Add(ex.StackTrace);
                fhr.NewFile = null;
            }

            return fhr;
        }

        interface IReplacementRule
        {
            List<string> RunRule(List<string> contents, string keyOrPattern, string value);
        }

        private static IReplacementRule GetRule(List<string> contents, string keyOrPattern)
        {
            bool wildCardMode = keyOrPattern.StartsWith("*");
            if (wildCardMode)
            {
                return new WildCard();
            }
            else
            {
                var deleteMode = keyOrPattern.StartsWith("&&");
                if (deleteMode)
                {
                    return new Delete();
                }
                else
                {
                    string matchPattern = keyOrPattern + "=";
                    if (contents.FindAll(x => x.Contains(matchPattern)).Count == 0)
                    {
                        return new Insert();
                    }
                    else
                    {
                        return new Change();
                    }
                }
            }

            throw new InvalidOperationException("IReplacementRule.GetRule has a situation that it doesn't have a rule for.");
        }

        private class Insert : IReplacementRule
        {
            public List<string> RunRule(List<string> contents, string keyOrPattern, string value)
            {
                var newFile = new List<string>();
                newFile.AddRange(contents);
                newFile.Add(keyOrPattern + "=\"" + value + "\"");
                return newFile;
            }
        }


        private class Delete : IReplacementRule
        {
            public List<string> RunRule(List<string> contents, string keyOrPattern, string value)
            {

                if (keyOrPattern.Length <= 2)
                {
                    return contents;
                }
                
                var newFile = new List<string>();

                foreach (var x in contents)
                {
                    string modified = x;

                    var matchKey = keyOrPattern.Substring(2, keyOrPattern.Length - 2);
                    if (!x.ToLower().Contains(matchKey.ToLower()))
                    {
                        newFile.Add(modified);
                    }
                }


                return newFile;
            }
        }

        private class WildCard : IReplacementRule
        {
            public List<string> RunRule(List<string> contents, string keyOrPattern, string value)
            {
                var newFile = new List<string>();
                string strippedKey = keyOrPattern.Substring(1, keyOrPattern.Length - 1);

                foreach (var x in contents)
                {
                    string modified = x;
                    if (x.ToLower().Contains(strippedKey))
                    {
                        modified = x.Replace(strippedKey, value);
                    }

                    newFile.Add(modified);
                }

                return newFile;
            }
        }

        private class Change : IReplacementRule
        {
            public List<string> RunRule(List<string> contents, string keyOrPattern, string value)
            {
                var newFile = new List<string>();

                foreach (var x in contents)
                {
                    string modified = x;
                    if (x.ToLower().Contains(keyOrPattern.ToLower()))
                    {
                        string[] split = x.Split('=');
                        modified = split[0] + '=' + "\"" + value + "\"";
                    }

                    newFile.Add(modified);
                }

                return newFile;
            }
        }

        public List<string> NewFile { get; set; }
        public List<string> Messages { get; set; }
        public bool IsSuccessful { get; set; }
    }
}
