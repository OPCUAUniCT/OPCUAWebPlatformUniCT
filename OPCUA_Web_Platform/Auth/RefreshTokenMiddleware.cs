using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Auth
{
    public class RefreshTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public RefreshTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context, ITokenManager tokenManager)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                string authorization = context.Request.Headers["Authorization"];
                string token = "empty";

                if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring("Bearer ".Length).Trim();
                    try
                    {
                        if (tokenManager.isRefreshable(token))
                        {
                            token = tokenManager.RefreshToken(token);
                            //todo: remove this print line
                            Console.WriteLine("I am refreshing the token!");
                        }
                    }
                    catch (Exception)
                    {
                        //todo: remove this print line
                        Console.WriteLine("Unauthorized by exception");
                        context.Response.StatusCode = 401;
                    }
                }

                context.Response.Headers.Add("x-token", token); 
            }

            return this._next(context);
            
        }
    }

    public static class RefreshTokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseRefreshToken(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RefreshTokenMiddleware>();
        }
    }
}
