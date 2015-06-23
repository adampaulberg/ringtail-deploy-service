using DataCamel.App;
using DataCamel.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataCamel.Data
{
    public class CoordinatorDataMapper
    {
        public string GetRpfDatabase(string server, string database, string username, string password)
        {
            var portal = new PortalDataMapper();
            var coordinator = portal.GetCoordinator(server, database, username, password);

            var uri = coordinator.CoordinatorUrl + "/GetRPFConnectionString";

            Console.WriteLine("  ....camel - RPF database uri: " + uri);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.UseDefaultCredentials = false;
            request.Credentials = WebRequestHelper.GetWebServiceCredentials(coordinator.Username, coordinator.Password, uri, true);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (var stream = response.GetResponseStream())
            using (var sr = new StreamReader(stream))
            {
                var output = sr.ReadToEnd();

                XElement el = XElement.Parse(output);

                byte[] textBytes = Convert.FromBase64String(el.Value);
                var connString = Encoding.Default.GetString(textBytes);

                connString = SecurityHelper.Decode(connString);

                var conn = new SqlConnectionStringBuilder(connString);
                return conn.InitialCatalog;
            }
        }
    }
}
