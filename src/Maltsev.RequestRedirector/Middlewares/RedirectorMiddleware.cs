using Maltsev.RequestRedirector.Extensions;
using Microsoft.AspNetCore.Http;

namespace Maltsev.RequestRedirector.Middlewares;

public class RedirectorMiddleware
{
    private readonly string _httpClient;

    public RedirectorMiddleware(RequestDelegate next, string httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task InvokeAsync(HttpContext httpContext, IHttpClientFactory httpClientFactory)
    {
        var request = await httpContext.GetRequestMessageAsync();

        var response = await httpClientFactory.CreateClient(_httpClient).SendAsync(request);
        httpContext.Response.Preparation(response);

        var content = await response.Content.ReadAsStringAsync();
        await httpContext.Response.WriteAsync(content);
    }
}
