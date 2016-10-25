using DataCamel.Data;
using DataCamel.Helpers;
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
                List<string> portalDbs = new List<string>();
                foreach (var x in databases)
                {
                    PortalDataMapper dbMapper = new PortalDataMapper();

                    var dbType = dbMapper.GetDatabaseType(options.Server, x, options.Username, options.Password);
                    if (dbType == DatabaseType.PORTAL)
                    {
                        portalDbs.Add(x);
                        Logger("\r\nPreparing feature launch keys prior to portal upgrade for portal: " + x + "\r\n");
                        result = CreateLaunchKeysForPostProcessing(options, x);
                    }

                    if(result != 0)
                    {
                        break;
                    }
                }

                if (result == 0)
                {
                    result = RunUpgradeDatabases(options, databases);
                }

                if (result == 0)
                {
                    // check that featureset_list table is correct for portal dbs, if any.
                    result = VerifyFeaturesetListTable(options, portalDbs);
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
                    result = CreateLaunchKeysForPostProcessing(options, x);
                }

                if (result == 0)
                {
                    Logger("\r\nStarting portal upgrade.\r\n");
                    result = RunUpgradeDatabases(options, fetchPortalDatabasesResult.PortalDatabases);
                }

                if (result == 0)
                {
                    // check that featureset_list table is correct.
                    result = VerifyFeaturesetListTable(options, fetchPortalDatabasesResult.PortalDatabases);
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

        int CreateLaunchKeysForPostProcessing(Options options, string dbName)
        {
            int resultCode = 0;
            try
            {
                string keyfileDropLocation = options.InstallPath + @"\" + "SQL Component_v" + options.Version +  @"\Scripts\Portal\PostProcessing\" + dbName + "_generated_feature_keys.txt";
                var helper = new LaunchKeyRunnerHelper();
                resultCode = helper.RunFile(Logger, keyfileDropLocation);
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

        int VerifyFeaturesetListTable(Options options, List<string> databases)
        {
            try
            {
                var launchKeys = new PortalDataMapper();

                foreach (var database in databases)
                {
                    if (database == "rs_tempdb")
                    {
                        continue;
                    }
                    var featuresetList = launchKeys.ReadFeaturesetListTable(options.Server, database, options.Username, options.Password);
                    var problems = LaunchKeyRunnerHelper.ReconcileExpectedKeysWithPostUpgradeKeys(
                        ConfigHelper.GetLaunchKeysFromDefaultConfig(), 
                        featuresetList.ToList());

                    foreach (var x in problems)
                    {
                        Logger("UPGRADE WARNING - the following selected launch key was not in the database after the upgrade - " + x + "\r\n");
                    }

                }
            }
            catch (Exception ex)
            {
                Logger("Failure in checking launch keys: " + ex.Message + "\r\n");
                return 1;
            }
            return 0;
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

                ReadUpgradeTable(options, database);

            }
            catch (Exception ex)
            {
                Logger(string.Format("Update for '{0}' Failed ({1})\r\n", database, ex.Message));
                throw ex;
            }
        }

        /// <summary>
        /// Reads the database.upgrade table, and adds appropriate information to the logs.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="database"></param>
        public void ReadUpgradeTable(Options options, string database)
        {
            PortalDataMapper dbMapper = new PortalDataMapper();

            if (upgradeErrors.Count > 0)
            {
                bool loggedSomething = false;
                foreach (var error in upgradeErrors)
                {
                    // usually the line before 'Upgrade failed...' contains the info we want, but if 'upgrade failed' is the only line we get, use those as the details.
                    var split = error.Split('\n');
                    var errorText = split[0];
                    if (errorText.Length > 100)
                    {
                        errorText = errorText.Substring(0, 99);
                    }

                    if (!errorText.StartsWith("Upgrade failed") || !loggedSomething)
                    {
                        loggedSomething = true;
                        Logger(string.Format("  Details: {0}", errorText));
                    }
                }
                Logger(string.Format("UPGRADE WARNING - {0} database upgrade failed - see the {0}.upgrade table for more information.\r\n", database));
            }
            else
            {
                Logger(string.Format("Update for '{0}' Complete\r\n", database));
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
