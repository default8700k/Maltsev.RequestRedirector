using Microsoft.AspNetCore.Http;

namespace Maltsev.RequestRedirector.Middlewares;

public class RedirectorMiddleware
{
    private readonly string _httpClient;

    public RedirectorMiddleware(RequestDelegate next, string httpClient)
    {
        this._httpClient = httpClient;
    }

    public Task InvokeAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }
}
