using Maltsev.RequestRedirector.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Maltsev.RequestRedirector;

public static class RedirectorExtensions
{
    public static IApplicationBuilder UseRequestRedirector(this IApplicationBuilder app, string httpClient)
    {
        app.UseMiddleware<RedirectorMiddleware>(httpClient);
        return app;
    }
}
