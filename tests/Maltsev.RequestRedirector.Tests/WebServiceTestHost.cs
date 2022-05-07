using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RichardSzalay.MockHttp;

namespace Maltsev.RequestRedirector.Tests;

public class WebServiceTestHost : IDisposable
{
    private readonly IHost _host;

    public WebServiceTestHost(MockHttpMessageHandler redirectorHandler)
    {
        _host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => builder
                .UseTestServer()
                .ConfigureTestServices(services =>
                {
                    services.AddHttpClient("Redirector", httpClient =>
                    {
                        httpClient.BaseAddress = new Uri("https://redirector:5001/");
                        httpClient.Timeout = TimeSpan.FromSeconds(45);
                    }).ConfigurePrimaryHttpMessageHandler(x => redirectorHandler);
                })
                .Configure(app =>
                {
                    app.Map("/api/redirector", x => x.UseRequestRedirector("Redirector"));
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("Hello World!");
                    });
                })
            )
            .Build();
    }

    public async Task<WebServiceTestHost> StartAsync()
    {
        await _host.StartAsync();
        return this;
    }

    public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage requestMessage) =>
        _host.GetTestClient().SendAsync(requestMessage);

    public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage requestMessage, IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
        {
            requestMessage.Headers.Add(header.Key, header.Value);
        }

        return await _host.GetTestClient().SendAsync(requestMessage);
    }

    public void Dispose()
    {
        _host.Dispose();
    }
}
