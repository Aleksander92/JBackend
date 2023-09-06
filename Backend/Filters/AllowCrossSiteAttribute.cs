using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Filters {
    public class AllowCrossSiteAttribute : ResultFilterAttribute {
        public override void OnResultExecuting(ResultExecutingContext context) {
            context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            base.OnResultExecuting(context);
        }
    }
}
