using DataCamel.App;
using DataCamel.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Data
{
    public class PortalDataMapper
    {
        public Coordinator GetCoordinator(string server, string database, string username, string password)
        {
            string cmdText = @"select * from rpf";
            string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", server, database, username, password);

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand command = new SqlCommand(cmdText, conn))
            {
                conn.Open();
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    return new Coordinator()
                    {
                        RpfId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CoordinatorUrl = reader.GetString(2),
                        JobManagerUrl = reader.GetString(3),
                        Username = reader.GetString(4),
                        Password = reader.GetString(5)
                    };
                }
            }
        }

        public IEnumerable<string> GetCases(string server, string database, string username, string password)
        {
            var result = new List<string>();
            string cmdText = @"select doc_id from main";
            string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", server, database, username, password);

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand command = new SqlCommand(cmdText, conn))
            {
                conn.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        result.Add(reader.GetString(0));
                }
            }
            return result;
        }

        /// <summary>
        /// Reads out errors only from the database's upgrade table.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IList<string> ReadErrosFromUpgradeTable(string server, string database, string username, string password)
        {
            try
            {
                var result = new List<string>();
                string cmdText = @"select stepDetail from upgrade where stepLabel in ('error', 'fatal')";
                string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", server, database, username, password);

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand command = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            result.Add(reader.GetString(0));
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// Returns the feature_key column from the featureset_list table as a list of strings.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IList<string> ReadFeaturesetListTable(string server, string database, string username, string password)
        {
            try
            {
                var result = new List<string>();
                string cmdText = @"select feature_key from featureset_list";
                string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", server, database, username, password);

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand command = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            result.Add(reader.GetString(0));
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// Find out what type of database this is, whether it's a Portal, Case, or something else.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public DatabaseType GetDatabaseType(string server, string database, string username, string password)
        {
            try
            {
                var result = new List<string>();
                string cmdText = @"select theValue from list_variables where theLabel='RingtailDatabaseType'";
                string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", server, database, username, password);

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand command = new SqlCommand(cmdText, conn))
                {
                    conn.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            result.Add(reader.GetString(0));
                    }
                }

                // could be a switch.  whatever.
                if (result[0].ToLower() == "portal")
                {
                    return DatabaseType.PORTAL;
                }
                if (result[0].ToLower() == "rpf")
                {
                    return DatabaseType.RPF;
                }
                if (result[0].ToLower() == "case")
                {
                    return DatabaseType.CASE;
                }

                return DatabaseType.UNKNOWN;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return DatabaseType.UNKNOWN;
            }
        }
    }

    public enum DatabaseType
    {
        PORTAL = 1,
        CASE = 2,
        RPF = 3,
        UNKNOWN = 4
    }
}
