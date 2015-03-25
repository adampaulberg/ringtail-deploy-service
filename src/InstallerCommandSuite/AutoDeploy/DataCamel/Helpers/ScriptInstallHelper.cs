using DataCamel.App;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Helpers
{
    public class ScriptInstallHelper
    {
        const string SCRIPT_RESOURCE_PREFIX = "DataCamel.Sql.";
        public Action<string> Logger { get; set; }
        public Options Options { get; set; }

        /// <summary>
        /// Installs the scripts to the server in the options
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public static void InstallScripts(Options options, Action<string> logger)
        {
            var instance = new ScriptInstallHelper(options, logger);            
            var scriptResourceNames = instance.GetScriptResources();
            foreach (var scriptResourceName in scriptResourceNames)
            {
                instance.InstallScript(scriptResourceName);
            }
        }
        
        /// <summary>
        /// Private constructor for use by static methods only
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        ScriptInstallHelper(Options options, Action<string> logger)
        {
            this.Logger = logger;
            this.Options = options;
        }

        /// <summary>
        /// Filters the embed resources to only those in the Sql folder
        /// </summary>
        /// <returns></returns>
        string[] GetScriptResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            return resources
                .Where((x) => x.StartsWith(SCRIPT_RESOURCE_PREFIX))
                .ToArray();            
        }

        /// <summary>
        /// Retrieves the contents of the script resource
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        string GetScriptContents(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();            
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets the scripts name from the resource name
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        string GetScriptName(string resourceName)
        {
            var fileName = resourceName.Replace(SCRIPT_RESOURCE_PREFIX, string.Empty);
            return fileName.Replace(".sql", string.Empty);
        }

        /// <summary>
        /// Installs the script on the Sql Server
        /// </summary>
        /// <param name="resourceName"></param>
        void InstallScript(string resourceName)
        {
            var scriptName = GetScriptName(resourceName);
            var script = GetScriptContents(resourceName);
            var scriptParts = script.Split(new string[] { "\r\ngo" }, StringSplitOptions.RemoveEmptyEntries);

            Logger(string.Format("Creating master.dbo.{0}... ", scriptName));
            string connStr = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3}", Options.Server, "master", Options.Username, Options.Password);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                foreach (var scriptPart in scriptParts)
                {
                    using (SqlCommand command = new SqlCommand(scriptPart, conn))
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                }
            }            
            Logger("Completed\r\n");
        }

    }
}
