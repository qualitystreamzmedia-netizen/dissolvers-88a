using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Dissolvers88A.Maui.R;

/// <summary>
/// Serves the bundled WebR runtime (<c>Resources/Raw/webr/**</c>) over loopback
/// with the COOP/COEP headers WebR needs for <c>SharedArrayBuffer</c>. A WebView
/// can't set those headers on <c>file://</c> assets, so we front them with this
/// tiny HTTP/1.1 responder instead.
/// </summary>
public sealed class WebrServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public int Port { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}/";

    public WebrServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = AcceptLoopAsync(_cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(ct); }
            catch { break; }
            _ = HandleAsync(client, ct);
        }
    }

    private static async Task HandleAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using (client)
            {
                await using var net = client.GetStream();
                using var reader = new StreamReader(net, Encoding.ASCII, false, 2048, leaveOpen: true);

                var requestLine = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(requestLine)) return;
                string? header;
                while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync(ct))) { /* drain */ }

                var target = requestLine.Split(' ') is { Length: >= 2 } p ? p[1] : "/";
                var rel = Uri.UnescapeDataString(target.Split('?', '#')[0]).Trim('/');
                if (rel.Length == 0) rel = "r-console.html";
                var asset = "webr/" + rel;

                byte[] body;
                string status;
                try
                {
                    await using var s = await FileSystem.OpenAppPackageFileAsync(asset);
                    using var ms = new MemoryStream();
                    await s.CopyToAsync(ms, ct);
                    body = ms.ToArray();
                    status = "200 OK";
                }
                catch
                {
                    body = Encoding.UTF8.GetBytes("Not found: " + asset);
                    status = "404 Not Found";
                }

                var head =
                    $"HTTP/1.1 {status}\r\n" +
                    $"Content-Type: {Mime(rel)}\r\n" +
                    $"Content-Length: {body.Length}\r\n" +
                    "Cross-Origin-Opener-Policy: same-origin\r\n" +
                    "Cross-Origin-Embedder-Policy: require-corp\r\n" +
                    "Cross-Origin-Resource-Policy: cross-origin\r\n" +
                    "Cache-Control: no-store\r\n" +
                    "Connection: close\r\n\r\n";

                await net.WriteAsync(Encoding.ASCII.GetBytes(head), ct);
                await net.WriteAsync(body, ct);
                await net.FlushAsync(ct);
            }
        }
        catch { /* client went away */ }
    }

    private static string Mime(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".mjs" or ".js" => "text/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".wasm" => "application/wasm",
        ".json" => "application/json",
        _ => "application/octet-stream",
    };

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
    }
}
