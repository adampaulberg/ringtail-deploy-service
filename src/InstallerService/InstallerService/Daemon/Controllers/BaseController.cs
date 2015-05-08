using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    [ApiLoggerActionFilter]
    public class BaseController : ApiController
    {        
        public Options Options
        {
            get { return this.Configuration.Properties["Options"] as Options; }
        }

        public ApiLogger Logger
        {
            get { return this.Configuration.Properties["ApiLogger"] as ApiLogger; }
        }
    }
}
