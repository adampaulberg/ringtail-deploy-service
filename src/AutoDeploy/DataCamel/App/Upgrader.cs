using DataCamel.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.App
{
    public class Upgrader
    {
        Action<string> Logger;

        public Upgrader(Action<string> logger)
        {
            this.Logger = logger;
        }

        public string FindLatestVersion(Options options)
        {
            string prefix = "SQL Component_v";
            string result = "";

            DirectoryInfo di = new DirectoryInfo(options.InstallPath);
            var dirs = di.GetDirectories(prefix + "*", SearchOption.TopDirectoryOnly);
            var dir = dirs.OrderByDescending(p => p.CreationTime).FirstOrDefault();

            if (dir != null)
                result = dir.Name.Substring(prefix.Length);

            return result;
        }

        public bool ValidateSqlComponent(Options options)
        {
            string prefix = "SQL Component_v";
            DirectoryInfo di = new DirectoryInfo(options.InstallPath);
            var dirs = di.GetDirectories(prefix + options.Version, SearchOption.TopDirectoryOnly);

            if (dirs.Length != 1)
                return false;
            else
                return true;
        }


        public static int resultCode;

        public int UpgradeDatabases(Options options)
        {
            int result = 0;
            int maxConcurrent = Int32.Parse(options.Max);
            var primaryAction = options.Actions[0];
            var databases = new List<string>();

            if (primaryAction == "upgrade")
            {
                databases.AddRange(options.Databases);
                foreach (var x in databases)
                {
                    Logger("\r\nPreparing feature launch keys prior to portal upgrade.\r\n");
                    result = InsertLaunchKeysPriorToUpgrade(options, x);

                    if(result != 0)
                    {
                        break;
                    }
                }

                if (result == 0)
                {
                    result = RunUpgradeDatabases(options, databases);
                }
                return result;

            }
            else if (primaryAction == "upgradeportal")
            {
                var fetchPortalDatabasesResult = GetPortalDatabases(options);

                if (fetchPortalDatabasesResult.ErrorCode != 0)
                {
                    return fetchPortalDatabasesResult.ErrorCode;
                }

                foreach (var x in fetchPortalDatabasesResult.PortalDatabases)
                {
                    Logger("\r\nPreparing feature launch keys prior to portal upgrade for portal: " + x + "\r\n");
                    result = InsertLaunchKeysPriorToUpgrade(options, x);
                }

                if (result == 0)
                {
                    Logger("\r\nStarting portal upgrade.\r\n");
                    result = RunUpgradeDatabases(options, fetchPortalDatabasesResult.PortalDatabases);
                }

                if (result == 0)
                {
                    Logger("Complete\r\n");

                    Logger("\r\nStarting RPF upgrade.\r\n");
                    result = RunUpgradeDatabases(options, fetchPortalDatabasesResult.RPFDatabases);

                    if (result == 0)
                    {
                        Logger("Complete\r\n");
                        var casePartitions = Extensions.Partition(fetchPortalDatabasesResult.CaseDatabases, maxConcurrent);

                        Logger("\r\nStarting Case upgrades.\r\n");
                        foreach (var partition in casePartitions)
                        {
                            Logger("Starting case upgrade batch of batch size: " + partition.ToList().Count + "\r\n");

                            if (result == 0)
                            {
                                result = RunUpgradeDatabases(options, partition.ToList());
                            }
                            else
                            {
                                Logger(string.Format("A database upgrade error happened.... stopping futher case upgrades.\r\n"));
                                break;
                            }
                        }

                        Logger("Complete\r\n");
                    }
                }
            }

            return result;

        }

        int InsertLaunchKeysPriorToUpgrade(Options options, string dbName)
        {
            int resultCode = 0;
            try
            {
                string keyfileDropLocation = options.InstallPath + @"\" + "SQL Component_v" + options.Version +  @"\Scripts\Portal\PostProcessing\" + dbName + "_generated_feature_keys.txt";

                FileInfo f = new FileInfo(keyfileDropLocation);
                if (f.Exists)
                {
                    var helper = new LaunchKeyRunnerHelper();
                    resultCode = helper.RunFile(Logger, keyfileDropLocation);
                }
                else
                {
                    Logger("There was no key file in this SQL component, so there are no keys to add.\r\n");
                }
            }
            catch(Exception ex)
            {
                Logger(string.Format("\r\n"));
                Logger(ex.Message);
                Logger(ex.StackTrace);
                Logger(string.Format("ERROR: failed to add launch keys to database.  See above.\r\n"));
                resultCode = 7;
            }

            return resultCode;
        }

        int RunUpgradeDatabases(Options options, List<string> databases)
        {
            var tasks = new List<Task>();
            int resultCode = 0;

            try
            {
                foreach (var database in databases)
                {
                    var localdb = database.Trim();
                    if (!string.IsNullOrWhiteSpace(localdb))
                    {
                        tasks.Add(Task.Factory.StartNew(() => RunUpgradeDatabase2(options, localdb)));
                    }
                }

                Task.WaitAll(tasks.ToArray());

                foreach (var x in tasks)
                {
                    if (x.Status == TaskStatus.Canceled || x.Status == TaskStatus.Faulted)
                    {
                        resultCode = 6;
                    }
                }
            }
            catch
            {
                Logger(string.Format("\r\n"));
                Logger(string.Format("ERROR: one or more databases failed to update.  See above.\r\n"));
                resultCode = 6;
            }

            return resultCode;
        }

        public class DatabaseUpgradeError : Exception
        {
            public DatabaseUpgradeError(string message)
                : base(message)
            {

            }

        }

        void RunUpgradeDatabase2(Options options, string database)
        {
            try
            {
                Logger(string.Format("Starting update for database '{0}'\r\n", database));

                string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", options.Server, "master", options.Username, options.Password);

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand command = new SqlCommand("rp_case_upgrade", conn))
                {
                    conn.Open();

                    command.CommandTimeout = 0;
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@database_name", database);
                    command.Parameters.AddWithValue("@username", options.Username);
                    command.Parameters.AddWithValue("@ringtail_app_version", options.Version);
                    command.ExecuteNonQuery();
                }
                Logger(string.Format("Update for '{0}' Complete\r\n", database));

            }
            catch (Exception ex)
            {
                Logger(string.Format("Update for '{0}' Failed ({1})\r\n", database, ex.Message));
                throw ex;
            }
        }

        GetDatabaseResult GetPortalDatabases(Options options)
        {
            var portalMapper = new PortalDataMapper();
            var coordinatorMapper = new CoordinatorDataMapper();

            var portalDb = options.Actions[1];
            var portalDbs = new List<string>();
            portalDbs.Add(portalDb);
            portalDbs.Add("rs_tempdb");

            var rpfDbs = new List<string>();
            var caseDbs = new List<string>();
            try
            {
                Logger("Finding RPF database... ");
                var rpfDb = coordinatorMapper.GetRpfDatabase(options.Server, portalDb, options.Username, options.Password);
                rpfDbs.Add(rpfDb);
                Logger("Complete\r\n");
            }
            catch (Exception ex)
            {
                Logger(string.Format("Failed ({0})\r\n", ex.Message));
                return new GetDatabaseResult(null, null, null, 5);
            }

            try
            {
                Logger("Finding case databases... ");
                var caseDatabaseSearchResult = portalMapper.GetCases(options.Server, portalDb, options.Username, options.Password);
                caseDbs.AddRange(caseDatabaseSearchResult);
                Logger("...found " + caseDbs.Count + "\r\n");
                Logger("Complete\r\n");
            }
            catch (Exception ex)
            {
                Logger(string.Format("Failed ({0})\r\n", ex.Message));
                return new GetDatabaseResult(null, null, null, 6);
            }
            return new GetDatabaseResult(portalDbs, rpfDbs, caseDbs, 0);
        }
    }


    public class LaunchKeyRunnerHelper
    {
        internal string keysFile = @"C:\Upgrade\AutoDeploy\launchKeys.json";
        internal string workingFolder = @"C:\upgrade\autodeploy\";

        public int RunFile(Action<string> Logger, string targetFilePath)
        {
            string filename = "ringtail-deploy-feature-utility.exe --bulkdatapath=\"" + targetFilePath + "\" --keysfile=\"" + keysFile + "\"";

            FileInfo fi = new FileInfo(workingFolder + "ringtail-deploy-feature-utility.exe");
            if (!fi.Exists)
            {
                return 0;
            }

            var keyFileContents = DataCamel.Helpers.SimpleFileReader.Read(keysFile);
            if (keyFileContents.Count == 0)
            {
                Logger("Launch Keys: No keys needed to be added.\r\n");
                return 0;
            }

            Helpers.ConfigHelper.WriteLaunchKeysAsJson(keysFile);

            int exitCode = SpawnAndLog(Logger, filename, workingFolder, null, null);
            if (exitCode == 0)
            {
                Logger("* Launch Keys: SUCCESS\r\n");
            }
            else
            {
                Logger("* Launch Keys: FAILED\r\n");
            }
            return exitCode;

        }

        public int SpawnAndLog(Action<string> Logger, string command, string workingFolder, string username, string password)
        {
            int exitCode = 0;
            try
            {
                var startingString = "*starting: " + workingFolder + command + "\r\n";
                var result = SpawnProcess(command, workingFolder, username, password);

                if (result.ExitCode != 0 && !result.ExitOk)
                {
                    Logger(startingString);
                    if (result.Output.Length > 0)
                    {
                        Logger("*Output text: ");
                        Logger(result.Output + "\r\n");
                    }
                    if (result.Error.Length > 0)
                    {
                        Logger("*Error text: ");
                        Logger(result.Error + "\r\n");
                    }
                    Logger("*Exit code: " + result.ExitCode + "\r\n");
                    exitCode = result.ExitCode;
                }

            }
            catch (Exception ex)
            {
                Logger("*RunFile error - trying to run the process threw an exception." + "\r\n");
                Logger(ex.Message);
                Logger(ex.StackTrace);
                exitCode = 2;
            }

            if (exitCode != 0)
            {
                Logger("*finished time: " + DateTime.Now + "\r\n");
            }

            return exitCode;
        }


        public class ProcessOutcome
        {
            public int ExitCode { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }
            public bool ExitOk { get; private set; }

            public ProcessOutcome(string error, string output, int exitCode, bool exitOk)
            {
                this.ExitCode = exitCode;
                this.Error = error;
                this.Output = output;
                this.ExitOk = exitOk;
            }
        }

        private ProcessOutcome SpawnProcess(string commandName, string workingDirectory, string username, string password)
        {
            var index = commandName.IndexOf(' ');

            string file = commandName;
            string args = string.Empty;
            if (index != -1)
            {
                file = commandName.Substring(0, index);
                args = commandName.Substring(index + 1, commandName.Length - index - 1);
            }

            string cmd = "/c " + workingDirectory + commandName;
            var processInfo = new ProcessStartInfo("cmd.exe", cmd);

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = file;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var nameParts = username.Split('\\');
                string domain = null;
                string user = null;
                if (nameParts.Length == 2)
                {
                    domain = nameParts[0];
                    user = nameParts[1];
                }
                else
                {
                    user = username;
                }

                process.StartInfo.Domain = domain;
                process.StartInfo.UserName = user;
                SecureString securePassword = new SecureString();
                foreach (char c in password.ToCharArray()) securePassword.AppendChar(c);
                process.StartInfo.Password = securePassword;
            }

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            return new ProcessOutcome(error, output, process.ExitCode, false);
        }
    }

    public class GetDatabaseResult
    {
        public List<string> PortalDatabases { get; private set; }
        public List<string> RPFDatabases { get; private set; }
        public List<string> CaseDatabases { get; private set; }
        public int ErrorCode { get; private set; }

        public GetDatabaseResult(List<string> portalDbs, List<string> rpfDatabases, List<string> caseDatabases, int errorCode)
        {
            this.PortalDatabases = portalDbs;
            this.RPFDatabases = rpfDatabases;
            this.CaseDatabases = caseDatabases;
            this.ErrorCode = errorCode;
        }
    }


    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int size)
        {
            T[] array = null;
            int count = 0;
            foreach (T item in source)
            {
                if (array == null)
                {
                    array = new T[size];
                }
                array[count] = item;
                count++;
                if (count == size)
                {
                    yield return new ReadOnlyCollection<T>(array);
                    array = null;
                    count = 0;
                }
            }
            if (array != null)
            {
                Array.Resize(ref array, count);
                yield return new ReadOnlyCollection<T>(array);
            }
        }
    }
}
