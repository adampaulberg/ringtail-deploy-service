using DataCamel.Data;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
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


        public void UpgradeDatabases(Options options)
        {
            var primaryAction = options.Actions[0];
            var databases = new List<string>();

            if (primaryAction == "upgrade")
            {
                databases.AddRange(options.Databases);
            }
            else if (primaryAction == "upgradeportal")
            {
                databases.AddRange(GetPortalDatabases(options));
            }

            RunUpgradeDatabases(options, databases);
        }

        void RunUpgradeDatabases(Options options, List<string> databases)
        {
            var tasks = new List<Task>();
            foreach (var database in databases)
            {
                var localdb = database.Trim();
                if (!string.IsNullOrWhiteSpace(localdb))
                {
                    tasks.Add(Task.Factory.StartNew(() => RunUpgradeDatabase(options, localdb, true)));
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        bool RunUpgradeDatabase(Options options, string database, bool async = false)
        {
            try
            {
                if (!async) Logger(string.Format("Updating database '{0}'... ", database));
                else Logger(string.Format("Starting update for database '{0}'\r\n", database));

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
                if (!async) Logger("Complete\r\n");
                else Logger(string.Format("Update for '{0}' Complete\r\n", database));
                return true;
            }
            catch (Exception ex)
            {
                if (!async) Logger(string.Format("Failed ({0})\r\n", ex.Message));
                else Logger(string.Format("Update for '{0}' Failed ({1})\r\n", database, ex.Message));
                return false;
            }
        }

        List<string> GetPortalDatabases(Options options)
        {
            var portalMapper = new PortalDataMapper();
            var coordinatorMapper = new CoordinatorDataMapper();

            var databases = new List<string>();
            var portalDb = options.Actions[1];
            databases.Add(portalDb);
            databases.Add("rs_tempdb");
            try
            {
                Logger("Finding RPF database... ");
                var rpfDb = coordinatorMapper.GetRpfDatabase(options.Server, portalDb, options.Username, options.Password);
                databases.Add(rpfDb);
                Logger("Complete\r\n");
            }
            catch (Exception ex)
            {
                Logger(string.Format("Failed ({0})\r\n", ex.Message));
            }

            try
            {
                Logger("Finding case databases... ");
                var caseDbs = portalMapper.GetCases(options.Server, portalDb, options.Username, options.Password);
                databases.AddRange(caseDbs);
                Logger("Complete\r\n");
            }
            catch (Exception ex)
            {
                Logger(string.Format("Failed ({0})\r\n", ex.Message));
            }
            return databases;
        }
    }
}
