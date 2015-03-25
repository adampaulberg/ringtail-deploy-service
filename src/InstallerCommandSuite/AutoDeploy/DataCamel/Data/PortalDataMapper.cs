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
    }
}
