using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Net.Http.Headers;

namespace Maltsev.RequestRedirector.Extensions;

internal static class HttpExtensions
{
    internal static async Task<HttpRequestMessage> GetRequestMessageAsync(this HttpContext httpContext)
    {
        var method = new HttpMethod(httpContext.Request.Method);
        var content = await httpContext.Request.Body.ReadToStringAsync();

        var requestUri = QueryHelpers.AddQueryString(httpContext.Request.Path, httpContext.Request.Query);
        var requestMessage = new HttpRequestMessage(method, requestUri)
        {
            Content = new StringContent(content)
        };

        requestMessage.Content!.Headers.ContentType = GetMediaTypeHeaderValue(httpContext.Request.ContentType);
        requestMessage.Content!.Headers.ContentLength = httpContext.Request.ContentLength;

        requestMessage.SetHeaders(httpContext.Request.Headers);
        return requestMessage;

        static MediaTypeHeaderValue? GetMediaTypeHeaderValue(string? contentType) =>
            string.IsNullOrWhiteSpace(contentType) ? null : MediaTypeHeaderValue.Parse(contentType);
    }

    internal static HttpResponse Preparation(this HttpResponse instance, HttpResponseMessage response)
    {
        instance.SetHeaders(response.Headers);
        instance.SetContentType(response.Content.Headers.ContentType);
        instance.SetStatusCode(response.StatusCode);

        return instance;
    }

    private static Task<string> ReadToStringAsync(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEndAsync();
    }

    private static HttpRequestMessage SetHeaders(this HttpRequestMessage request, IHeaderDictionary headers)
    {
        headers.RemoveIfKeyExists("Content-Type");
        headers.RemoveIfKeyExists("Content-Length");

        foreach (var header in headers)
        {
            var values = header.Value as IEnumerable<string>;
            request.Headers.Add(header.Key, values);
        }

        return request;
    }

    private static HttpResponse SetHeaders(this HttpResponse response, HttpResponseHeaders headers)
    {
        headers.RemoveIfKeyExists("Transfer-Encoding");

        foreach (var header in headers)
        {
            var values = header.Value.ToArray();
            response.Headers.Add(header.Key, values);
        }

        return response;
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

    private static IHeaderDictionary RemoveIfKeyExists(this IHeaderDictionary headers, string key)
    {
        if (headers.ContainsKey(key))
        {
            headers.Remove(key);
        }

        return headers;
    }

    private static HttpResponseHeaders RemoveIfKeyExists(this HttpResponseHeaders headers, string key)
    {
        if (headers.Contains(key))
        {
            headers.Remove(key);
        }

        return headers;
    }
}
