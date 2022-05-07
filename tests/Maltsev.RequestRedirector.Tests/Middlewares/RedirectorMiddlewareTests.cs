using AutoFixture.Xunit2;
using FluentAssertions;
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
    public async Task RequestOnRedirector_ShouldBeRedirected(RequestModel model)
    {
        // setup
        var requestRedirector = _httpClientRedirector
            .When(HttpMethod.Post, $"https://redirector:5001/*")
            .WithHeaders(model.RequestHeaders)
            .WithContent(model.RequestContentBody)
            .Respond(
                statusCode: HttpStatusCode.Accepted,
                headers: model.ResponseHeaders,
                mediaType: "text/html",
                content: "abc"
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(HttpMethod.Post, $"/api/redirector/{model.RequestUrl}")
            {
                Content = new StringContent(model.RequestContentBody, Encoding.UTF8, "application/json")
            },
            headers: model.RequestHeaders
        );

        // assert
        response.Should().Be202Accepted();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");

        foreach (var header in model.ResponseHeaders)
        {
            response.Should().HaveHeader(header.Key).And.BeValues(new[] { header.Value });
        }

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("abc");

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [AutoData]
    public async Task RequestOnOther_ShouldNotBeRedirected(HttpMethod method, string url)
    {
        // setup
        var requestRedirector = _httpClientRedirector
            .When("https://redirector:5001/*")
            .Respond(
                statusCode: HttpStatusCode.Accepted,
                mediaType: "text/html",
                content: "abc"
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(method, $"/api/{url}"),
            headers: Enumerable.Empty<KeyValuePair<string, string>>()
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
