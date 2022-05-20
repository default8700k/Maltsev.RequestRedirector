using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maltsev.RequestRedirector.Tests;

public class WebServiceTestHost : IDisposable
{
    private readonly IHost _host = new HostBuilder()
        .ConfigureWebHostDefaults(builder => builder
            .UseTestServer()
            .ConfigureTestServices(services =>
            {
                services.AddHttpClient("Redirector", httpClient =>
                {
                    httpClient.BaseAddress = new Uri("http://localhost:25565/");
                    httpClient.Timeout = TimeSpan.FromSeconds(45);
                });
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

    public async Task<WebServiceTestHost> StartAsync()
    {
        await _host.StartAsync();
        return this;
    }

    public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request) =>
        _host.GetTestClient().SendAsync(request);

    public Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        return _host.GetTestClient().SendAsync(request);
    }

    public void Dispose()
    {
        _host.Dispose();
    }
}
