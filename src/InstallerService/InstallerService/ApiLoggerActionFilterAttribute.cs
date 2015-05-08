using InstallerService.Daemon.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace InstallerService
{
    public class ApiLoggerActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            var controller = actionContext.ControllerContext.Controller as BaseController;
            if (controller != null)
            {
                var request = actionContext.Request;
                controller.Logger.Log(request);
            }
            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var controller = actionExecutedContext.ActionContext.ControllerContext.Controller as BaseController;
            if (controller != null)
            {
                var request = actionExecutedContext.Request;
                var response = actionExecutedContext.Response;
                controller.Logger.Log(request, response);
            }
            base.OnActionExecuted(actionExecutedContext);
        }
    }
}
