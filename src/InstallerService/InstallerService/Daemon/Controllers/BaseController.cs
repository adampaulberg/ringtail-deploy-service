using InstallerService.ControllerHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.SelfHost;

namespace InstallerService.Daemon.Controllers
{
    [AuthorizeLocalAdmin]
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

        public override Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {            
            return base.ExecuteAsync(controllerContext, cancellationToken);
        }
    }
}
