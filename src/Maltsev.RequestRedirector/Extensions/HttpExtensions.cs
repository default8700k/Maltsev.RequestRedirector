using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Headers;

namespace Maltsev.RequestRedirector.Extensions;

internal static class HttpExtensions
{
    internal static HttpRequestMessage GetRequestMessage(this HttpContext httpContext)
    {
        var requestMessage = new HttpRequestMessage
        {
            Content = new StreamContent(httpContext.Request.Body)
        };

        requestMessage.SetMethod(httpContext.Request.Method);
        requestMessage.Headers.SetHeaders(httpContext.Request.Headers);
        return requestMessage;
    }

    internal static async Task WriteResponseAsync(this HttpContext httpContext, HttpResponseMessage response)
    {
        httpContext.Response.Headers.SetHeaders(response.Headers);
        httpContext.Response.SetContentType(response.Content.Headers.ContentType);
        httpContext.Response.SetStatusCode(response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        await httpContext.Response.WriteAsync(content);
    }

    private static HttpRequestMessage SetMethod(this HttpRequestMessage requestMessage, string method)
    {
        requestMessage.Method = new HttpMethod(method);
        return requestMessage;
    }

    private static HttpRequestHeaders SetHeaders(this HttpRequestHeaders requestHeaders, IHeaderDictionary headerDictionary)
    {
        foreach (var header in headerDictionary)
        {
            var values = header.Value.ToArray();
            requestHeaders.Add(header.Key, values);
        }

        return requestHeaders;
    }

    private static IHeaderDictionary SetHeaders(this IHeaderDictionary headerDictionary, HttpResponseHeaders responseHeaders)
    {
        foreach (var responseHeader in responseHeaders)
        {
            var values = responseHeader.Value.ToArray();
            headerDictionary.Add(responseHeader.Key, values);
        }

        return headerDictionary;
    }

    private static HttpResponse SetContentType(this HttpResponse response, MediaTypeHeaderValue? mediaType)
    {
        if (mediaType?.MediaType != null)
        {
            response.ContentType = mediaType.MediaType;
        }

        return response;
    }

    private static HttpResponse SetStatusCode(this HttpResponse response, HttpStatusCode statusCode)
    {
        response.StatusCode = (int)statusCode;
        return response;
    }
}
