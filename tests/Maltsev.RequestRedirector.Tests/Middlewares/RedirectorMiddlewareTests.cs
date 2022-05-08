using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using Xunit;

namespace Maltsev.RequestRedirector.Tests.Middlewares;

public class RedirectorMiddlewareTests
{
    private readonly MockHttpMessageHandler _httpClientRedirector = new();

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_ShouldBeRedirected(string url, HttpMethod method, string responseContent)
    {
        // setup
        var requestRedirector = _httpClientRedirector
            .When(method, $"https://redirector:5001/{url}")
            .Respond(
                statusCode: HttpStatusCode.Found, // 302
                mediaType: "text/example",
                content: responseContent
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/redirector/{url}")
        );

        // assert
        response.Should().Be302Found();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/example");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(responseContent);

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithHeaders_ShouldBeCorrect(string url, HttpMethod method, Generator<KeyValuePair<string, string>> headers)
    {
        // setup
        var requestHeaders = headers.Take(3).ToList();
        var responseHeaders = headers.Take(3).ToList();

        var requestRedirector = _httpClientRedirector
            .When(method, $"https://redirector:5001/{url}")
            .WithHeaders(requestHeaders)
            .Respond(
                statusCode: HttpStatusCode.Found, // 302
                headers: responseHeaders,
                mediaType: "text/example",
                content: ""
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/redirector/{url}"),
            headers: requestHeaders
        );

        // assert
        response.Should().Be302Found();

        response.Headers.Should().HaveSameCount(responseHeaders);
        foreach (var header in responseHeaders)
        {
            response.Should().HaveHeader(header.Key).And.BeValues(new[] { header.Value });
        }

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithQueryParameters_ShouldBeCorrect(string url, HttpMethod method, IEnumerable<KeyValuePair<string, StringValues>> parameters)
    {
        // setup
        var queryParameters = QueryHelpers.AddQueryString("", parameters).Remove(0, 1);

        var requestRedirector = _httpClientRedirector
            .When(method, $"https://redirector:5001/{url}")
            .WithQueryString(queryParameters)
            .Respond(
                statusCode: HttpStatusCode.Found, // 302
                mediaType: "text/example",
                content: ""
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/redirector/{url}?{queryParameters}")
        );

        // assert
        response.Should().Be302Found();

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithRequestContent_ShouldBeCorrect(string url, HttpMethod method, string requestContent)
    {
        // setup
        var requestRedirector = _httpClientRedirector
            .When(method, $"https://redirector:5001/{url}")
            .WithContent(requestContent)
            .Respond(
                statusCode: HttpStatusCode.Found, // 302
                mediaType: "text/example",
                content: ""
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/redirector/{url}")
            {
                Content = new StringContent(requestContent, Encoding.UTF8, "text/plain")
            }
        );

        // assert
        response.Should().Be302Found();

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnOther_ShouldNotBeRedirected(string url, HttpMethod method)
    {
        // setup
        var requestRedirector = _httpClientRedirector
            .When("https://redirector:5001/*")
            .Respond(
                statusCode: HttpStatusCode.BadGateway // 502
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/{url}")
        );

        // assert
        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Hello World!");

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(0);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }
}
