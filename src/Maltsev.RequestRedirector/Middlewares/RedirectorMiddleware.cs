using Maltsev.RequestRedirector.Extensions;
using Microsoft.AspNetCore.Http;

namespace Maltsev.RequestRedirector.Middlewares;

public class RedirectorMiddleware
{
    private readonly string _httpClient;

    public RedirectorMiddleware(RequestDelegate next, string httpClient)
    {
        this._httpClient = httpClient;
    }

    public async Task InvokeAsync(HttpContext httpContext, IHttpClientFactory httpClientFactory)
    {
        var request = httpContext.GetRequestMessage();
        var response = await httpClientFactory.CreateClient(_httpClient).SendAsync(request);
        await httpContext.WriteResponseAsync(response);
    }
}
