using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class HelpController : BaseController
    {
        [HttpGet]
        public string GetAPIs()
        {
            var api = ReadAPI();

            string result = string.Empty;

            foreach (var x in api)
            {
                result += "<p>" + x + "</p>";
            }

            return result;
        }

        public static IEnumerable<string> ReadAPI()
        {

            var methods = Assembly.GetExecutingAssembly().GetTypes()
                          .SelectMany(t => t.GetMethods())
                          .Where(m => m.GetCustomAttributes(typeof(HttpGetAttribute), false).Length > 0)
                          .ToArray();

            foreach (var x in methods)
            {
                string[] split = x.DeclaringType.ToString().Split('.');
                string cleand = split[split.Length - 1].Replace("Controller", string.Empty);

                string fullName = "api/" + cleand;
                var methodParams = x.GetParameters();
                if (methodParams.Length > 0)
                {
                    fullName += "?";
                    foreach (var z in methodParams)
                    {
                        fullName += z.Name + "=***&";
                    }

                    fullName = fullName.Substring(0, fullName.Length - 1);
                }


                yield return fullName;
            }
        }
    }
}
