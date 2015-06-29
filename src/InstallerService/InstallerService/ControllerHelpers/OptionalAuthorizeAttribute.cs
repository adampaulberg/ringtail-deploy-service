using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace InstallerService.ControllerHelpers
{
    public class OptionalAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var config = actionContext.ControllerContext.Configuration as HttpSelfHostConfiguration;
            if(config != null && config.ClientCredentialType != HttpClientCredentialType.None) {                
                base.OnAuthorization(actionContext);   
            }                        
        }
    }
}
