using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.MVCBugFix
{
    public static class RequestValidatorExtensions
    {
        // Extensions method to simplify RequestValidatorMiddleware usage 
        public static IApplicationBuilder UseRequestValidator(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestValidatorMiddleware>();
        }
    }
}
