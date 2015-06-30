using InstallerService.Daemon.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.SelfHost;

namespace InstallerService.ControllerHelpers
{
    /// <summary>
    /// This Attribute will only authorize requests if Authentication has 
    /// been enabled. If it's enabled it will only Authorize users that
    /// have Local Administrator permissions.
    /// </summary>
    public class AuthorizeLocalAdminAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (IsAuthenticationEnabled(actionContext))
            {
                if (IsLocalAdmin(actionContext))
                    base.OnAuthorization(actionContext);
                else
                    base.HandleUnauthorizedRequest(actionContext);
            }                
        }

        public bool IsAuthenticationEnabled(HttpActionContext actionContext)
        {
            var controller = actionContext.ControllerContext.Controller as BaseController;
            return controller.IsSecurityEnabled;
        }

        public bool IsLocalAdmin(HttpActionContext actionContext)
        {            
            var controller = actionContext.ControllerContext.Controller as ApiController;
            var windowsUser = controller.User as WindowsPrincipal;
            var result = windowsUser.IsInRole(WindowsBuiltInRole.Administrator);
            return result;            
        }
    }
}
