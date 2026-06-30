using System.Net;
using System.Text;
using System.Text.Json;

using Markdig;

namespace Ferret.Manual;

/// <summary>Self-hosted HTTP server that serves the Ferret Manual over <see cref="HttpListener"/>.</summary>
public sealed class ManualServer : IDisposable
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private readonly HttpListener _listener;
    private readonly int _port;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ManualServer"/> class bound to the given port.</summary>
    /// <param name="port">TCP port to listen on (default 7070).</param>
    public ManualServer(int port = 7070)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    /// <summary>Gets the base URL of the manual (e.g. "http://localhost:7070/manual").</summary>
    public Uri BaseUrl => new Uri($"http://localhost:{_port}/manual");

    /// <summary>Starts the listener loop; runs until <paramref name="ct"/> is cancelled.</summary>
    /// <param name="ct">Cancellation token that stops the server.</param>
    /// <returns>A task that completes when the server shuts down.</returns>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener.Start();
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(ctx), ct);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch (ObjectDisposedException)
        {
            // already stopped
        }
    }

    private static async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var query = ctx.Request.Url?.Query ?? string.Empty;

            if (path == "/" || path == "/manual" || path == "/manual/")
            {
                Redirect(ctx, "/manual/getting-started/index");
                return;
            }

            if (path.StartsWith("/manual/search", StringComparison.OrdinalIgnoreCase))
            {
                await ServeSearchAsync(ctx, query).ConfigureAwait(false);
                return;
            }

            if (path.StartsWith("/manual/", StringComparison.OrdinalIgnoreCase))
            {
                var slug = path["/manual/".Length..].TrimEnd('/');
                await ServePageAsync(ctx, slug).ConfigureAwait(false);
                return;
            }

            Respond(ctx, 404, "text/plain", "Not found");
        }
        catch (InvalidOperationException ex)
        {
            TryRespondError(ctx, ex.Message);
        }
        catch (IOException ex)
        {
            TryRespondError(ctx, ex.Message);
        }
#pragma warning disable CA1031 // catch broad exception as a last-resort safety net for the HTTP handler
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[ManualServer] Unhandled error serving {ctx.Request.Url?.AbsolutePath}: {ex}").ConfigureAwait(false);
            TryRespondError(ctx, "Internal server error");
        }
#pragma warning restore CA1031
    }

    private static Task ServePageAsync(HttpListenerContext ctx, string slug)
    {
        var page = DocRegistry.GetPage(slug);
        if (page is null)
        {
            Respond(
                ctx,
                404,
                "text/html",
                "<html><body><h1>404 — Page not found</h1><p><a href=\"/manual\">Back to manual</a></p></body></html>");
            return Task.CompletedTask;
        }

        var markdown = DocRegistry.GetMarkdown(page);
        var contentHtml = Markdown.ToHtml(markdown, Pipeline);
        var prev = DocRegistry.GetPreviousPage(page);
        var next = DocRegistry.GetNextPage(page);
        var html = HtmlTemplate.Render(page, contentHtml, DocRegistry.AllPages, prev, next);
        Respond(ctx, 200, "text/html; charset=utf-8", html);
        return Task.CompletedTask;
    }

    private static Task ServeSearchAsync(HttpListenerContext ctx, string rawQuery)
    {
        var q = System.Web.HttpUtility.ParseQueryString(rawQuery)["q"] ?? string.Empty;
        var results = DocRegistry.AllPages
            .Where(p => p.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || p.Section.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(p => new { slug = p.Slug, title = p.Title, section = p.Section });
        var json = JsonSerializer.Serialize(results);
        Respond(ctx, 200, "application/json", json);
        return Task.CompletedTask;
    }

    private static void Redirect(HttpListenerContext ctx, string location)
    {
        ctx.Response.StatusCode = 302;
        ctx.Response.Headers["Location"] = location;
        ctx.Response.Close();
    }

    private static void Respond(HttpListenerContext ctx, int statusCode, string contentType, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes);
        ctx.Response.Close();
    }

    private static void TryRespondError(HttpListenerContext ctx, string message)
    {
        try
        {
            Respond(ctx, 500, "text/plain", $"Error: {message}");
        }
        catch (ObjectDisposedException)
        {
            // listener closed
        }
        catch (InvalidOperationException)
        {
            // listener closed
        }
    }
}
