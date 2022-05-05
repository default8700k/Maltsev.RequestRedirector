using AutoFixture.Xunit2;
using FluentAssertions;
using RichardSzalay.MockHttp;
using System.Net;
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
                statusCode: HttpStatusCode.OK,
                headers: model.ResponseHeaders,
                mediaType: "application/json",
                content: "abc"
            );

        using var host = await new WebServiceTestHost(_httpClientRedirector).StartAsync();

        // act
        var response = await host.SendRequestAsync(
            requestMessage: new HttpRequestMessage(HttpMethod.Post, $"/api/redirector/{model.RequestUrl}")
            {
                Content = new StringContent(model.RequestContentBody)
            },
            headers: model.RequestHeaders
        );

        // assert
        response.Should().Be200Ok();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        foreach (var header in model.ResponseHeaders)
        {
            response.Should().HaveHeader(header.Key).And.BeValues(new[] { header.Value });
        }

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("abc");

        _httpClientRedirector.GetMatchCount(requestRedirector).Should().Be(1);
        _httpClientRedirector.VerifyNoOutstandingExpectation();
    }
}
