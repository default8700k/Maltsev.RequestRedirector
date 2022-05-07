namespace Maltsev.RequestRedirector.Tests;

public class RequestModel
{
    public string RequestUrl { get; set; } = null!;
    public IEnumerable<KeyValuePair<string, string>> RequestHeaders { get; set; } = null!;
    public string RequestContentBody { get; set; } = null!;
    public IEnumerable<KeyValuePair<string, string>> ResponseHeaders { get; set; } = null!;
    public string ResponseContentBody { get; set; } = null!;
}
