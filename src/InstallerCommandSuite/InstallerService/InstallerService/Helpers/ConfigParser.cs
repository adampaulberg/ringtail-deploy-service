using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerService.Helpers
{
    public static class ConfigParser
    {
        public static Dictionary<string, string> Parse(List<string> rows)
        {
            var results = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                var parts = row.Split('|');
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = parts[1];
                    results[key] = value;
                }
            }
            return results;
        }
    }
}
