using System.Net.Http;
using System.Net.Sockets;

namespace Ferret.Manual.Tests;

public sealed class ManualServerTests : IAsyncDisposable
{
    private readonly ManualServer _server;
    private readonly HttpClient _client;
    private readonly CancellationTokenSource _cts;

    public ManualServerTests()
    {
        _server = new ManualServer(17070); // non-default port avoids conflicts
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:17070") };
        _cts = new CancellationTokenSource();
        _ = _server.StartAsync(_cts.Token);

        // wait up to 3 seconds for the listener to accept connections
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var probe = new TcpClient();
                probe.Connect("127.0.0.1", 17070);
                break;
            }
            catch (SocketException)
            {
                Thread.Sleep(50);
            }
        }
    }

    [Fact]
    public async Task Get_Root_Redirects_To_Manual()
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CheckCertificateRevocationList = true,
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:17070") };
        var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(302, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_Manual_Returns_Html()
    {
        var response = await _client.GetAsync(new Uri("/manual", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html", body, StringComparison.Ordinal);
        Assert.Contains("Ferret Manual", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_KnownPage_Returns_Html()
    {
        var response = await _client.GetAsync(new Uri("/manual/getting-started/installation", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Installation", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_UnknownPage_Returns_404()
    {
        var response = await _client.GetAsync(new Uri("/manual/nonexistent-page", UriKind.Relative));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SearchApi_Returns_Json()
    {
        var response = await _client.GetAsync(new Uri("/manual/search?q=install", UriKind.Relative));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("[", body.Trim(), StringComparison.Ordinal);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _client.Dispose();
        _server.Dispose();
    }
}
