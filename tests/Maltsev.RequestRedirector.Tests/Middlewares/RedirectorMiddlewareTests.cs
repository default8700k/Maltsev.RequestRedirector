using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Maltsev.RequestRedirector.Tests.Middlewares;

public class RedirectorMiddlewareTests : IDisposable
{
    private readonly WireMockServer _server;

    public RedirectorMiddlewareTests()
    {
        _server = WireMockServer.Start(25565);
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_ShouldBeRedirected(string url, string responseContent)
    {
        // setup
        _server.Given(
            Request.Create()
                .UsingGet()
                .WithPath($"/{url}")
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.Found)
                .WithHeader("Content-Type", "text/example")
                .WithBody(responseContent)
        );

        using var host = await new WebServiceTestHost().StartAsync();

        // act
        var response = await host.SendRequestAsync(
            request: new HttpRequestMessage(HttpMethod.Get, $"/api/redirector/{url}")
        );

        // assert
        response.Should().Be302Found();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/example");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be(responseContent);
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithHeaders_ShouldBeCorrect(string url, Generator<KeyValuePair<string, string>> headers)
    {
        // setup
        var requestHeaders = headers.Take(3).ToList();
        var responseHeaders = headers.Take(3).ToList();

        _server.Given(
            Request.Create()
                .UsingGet()
                .WithHeader(requestHeaders[0].Key, requestHeaders[0].Value)
                .WithHeader(requestHeaders[1].Key, requestHeaders[1].Value)
                .WithHeader(requestHeaders[2].Key, requestHeaders[2].Value)
                .WithPath($"/{url}")
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.Found)
                .WithHeader(responseHeaders[0].Key, responseHeaders[0].Value)
                .WithHeader(responseHeaders[1].Key, responseHeaders[1].Value)
                .WithHeader(responseHeaders[2].Key, responseHeaders[2].Value)
        );

        using var host = await new WebServiceTestHost().StartAsync();

        // act
        var response = await host.SendRequestAsync(
            request: new HttpRequestMessage(HttpMethod.Get, $"/api/redirector/{url}"),
            headers: requestHeaders
        );

        // assert
        response.Should().Be302Found();

        foreach (var header in responseHeaders)
        {
            response.Should().HaveHeader(header.Key).And.BeValues(new[] { header.Value });
        }
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithQueryParameters_ShouldBeCorrect(string url, KeyValuePair<string, StringValues>[] parameters)
    {
        // setup
        var requestUrl = QueryHelpers.AddQueryString($"/api/redirector/{url}", parameters);

        _server.Given(
            Request.Create()
                .UsingGet()
                .WithParam(parameters[0].Key, parameters[0].Value)
                .WithParam(parameters[1].Key, parameters[1].Value)
                .WithParam(parameters[2].Key, parameters[2].Value)
                .WithPath($"/{url}")
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.Found)
        );

        using var host = await new WebServiceTestHost().StartAsync();

        // act
        var response = await host.SendRequestAsync(
            request: new HttpRequestMessage(HttpMethod.Get, requestUrl)
        );

        // assert
        response.Should().Be302Found();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnRedirector_WithRequestContent_ShouldBeCorrect(string url, string requestContent)
    {
        // setup
        _server.Given(
            Request.Create()
                .UsingPost()
                .WithPath($"/{url}")
                .WithBody(requestContent)
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.Found)
        );

        using var host = await new WebServiceTestHost().StartAsync();

        // act
        var response = await host.SendRequestAsync(
            request: new HttpRequestMessage(HttpMethod.Post, $"/api/redirector/{url}")
            {
                Content = new StringContent(requestContent, Encoding.UTF8, "text/plain")
            }
        );

        // assert
        response.Should().Be302Found();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnOther_ShouldNotBeRedirected(string url, HttpMethod method)
    {
        // setup
        _server.Given(
            Request.Create()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(HttpStatusCode.Found)
        );

        using var host = await new WebServiceTestHost().StartAsync();

        // act
        var response = await host.SendRequestAsync(
            request: new HttpRequestMessage(method, $"/something/{url}")
        );

        // assert
        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Hello World!");
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
