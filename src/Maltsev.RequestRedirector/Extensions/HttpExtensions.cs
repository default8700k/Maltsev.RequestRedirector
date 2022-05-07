﻿using Microsoft.AspNetCore.Http;
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

        requestMessage.Content!.Headers.ContentType = MediaTypeHeaderValue.Parse(httpContext.Request.ContentType);
        requestMessage.Content!.Headers.ContentLength = httpContext.Request.ContentLength;

        requestMessage.SetMethod(httpContext.Request.Method);
        requestMessage.SetHeaders(httpContext.Request.Headers);
        return requestMessage;
    }

    internal static async Task WriteResponseAsync(this HttpContext httpContext, HttpResponseMessage response)
    {
        httpContext.Response.SetHeaders(response.Headers);
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

    private static HttpRequestMessage SetHeaders(this HttpRequestMessage requestMessage, IHeaderDictionary headerDictionary)
    {
        headerDictionary.RemoveIfKeyExists("Content-Type");
        headerDictionary.RemoveIfKeyExists("Content-Length");

        foreach (var header in headerDictionary)
        {
            var values = header.Value.ToArray();
            requestMessage.Headers.Add(header.Key, values);
        }

        return requestMessage;
    }

    private static HttpResponse SetHeaders(this HttpResponse response, HttpResponseHeaders responseHeaders)
    {
        foreach (var responseHeader in responseHeaders)
        {
            var values = responseHeader.Value.ToArray();
            response.Headers.Add(responseHeader.Key, values);
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

    private static IHeaderDictionary RemoveIfKeyExists(this IHeaderDictionary headerDictionary, string key)
    {
        if (headerDictionary.ContainsKey(key))
        {
            headerDictionary.Remove(key);
        }

        return headerDictionary;
    }
}