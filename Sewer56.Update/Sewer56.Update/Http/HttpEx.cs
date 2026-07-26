using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Sewer56.Update.Http;

/// <summary>
/// Shared helpers for HTTP requests issued by the built-in package resolvers.
/// </summary>
public static class HttpEx
{
    /// <summary>
    /// User-Agent string used by the library when no application-wide User-Agent is configured.
    ///
    /// Derived from this assembly's version (set by <c>&lt;Version&gt;</c> in
    /// <c>Sewer56.Update.csproj</c>), so it stays in sync automatically across releases.
    /// </summary>
    public static readonly string DefaultUserAgent =
        $"Sewer56.Update/{typeof(HttpEx).Assembly.GetName().Version}";

    private static string _applicationUserAgent = DefaultUserAgent;

    /// <summary>
    /// User-Agent sent on every HTTP request the library issues (file-size probes, downloads,
    /// metadata fetches). Defaults to <see cref="DefaultUserAgent"/>.
    ///
    /// Host applications should set this once at startup (e.g.
    /// <c>HttpEx.ApplicationUserAgent = "Reloaded-II/2.2.2";</c>) so their traffic can be identified
    /// server-side. Setting <c>null</c> or whitespace is ignored, preserving the previous value.
    /// </summary>
    public static string ApplicationUserAgent
    {
        get => _applicationUserAgent;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                _applicationUserAgent = value!;
        }
    }

    /// <summary>
    /// Timeout, in milliseconds, applied to file-size probe requests. Probes must not hang for the
    /// full 100s <see cref="HttpWebRequest"/> default.
    /// </summary>
    public static int ProbeTimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// Obtains the download size of the resource at <paramref name="url"/> without downloading its body.
    /// </summary>
    /// <remarks>
    /// Issues an HTTP <c>HEAD</c> request; if the server does not support <c>HEAD</c> or omits
    /// <c>Content-Length</c>, falls back to a 1-byte <c>Range</c> GET and parses the total from
    /// <c>Content-Range</c>. The response is disposed on every code path. Cancellation is delivered
    /// via <see cref="HttpWebRequest.Abort"/> and rethrown as <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="url">URL of the resource whose size will be probed.</param>
    /// <param name="token">Cancellation token. Aborts the in-flight request when cancelled.</param>
    /// <returns>The size of the resource in bytes, or <c>-1</c> if it could not be determined.</returns>
    public static async Task<long> GetContentLengthAsync(Uri url, CancellationToken token = default)
    {
        // HttpWebRequest.Timeout only bounds synchronous GetResponse; GetResponseAsync (built on
        // BeginGetResponse) ignores it and would otherwise hang for the 100s default. Link the caller
        // token with a probe-timeout token and wire Abort to the linked token so the in-flight
        // request is torn down whichever fires first.
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        probeCts.CancelAfter(ProbeTimeoutMilliseconds);
        var probeToken = probeCts.Token;

        // Strategy 1: HEAD request.
        try
        {
            var headReq = CreateProbeRequest(url);
            headReq.Method = "HEAD";
            using var reg = probeToken.Register(() => { try { headReq.Abort(); } catch { /* ignored */ } });
            var headResp = (HttpWebResponse)await headReq.GetResponseAsync().ConfigureAwait(false);
            using (headResp)
            {
                if (headResp.ContentLength >= 0)
                    return headResp.ContentLength;
            }
        }
        catch (WebException ex)
        {
            // Server may reject HEAD (405/501); dispose the error response and fall through.
            (ex.Response as HttpWebResponse)?.Dispose();
        }
        catch (Exception) { /* transient failure; fall through to Range GET */ }

        token.ThrowIfCancellationRequested();

        // Strategy 2: 1-byte Range GET.
        try
        {
            var rangeReq = CreateProbeRequest(url);
            rangeReq.AddRange(0, 0);
            using var reg2 = probeToken.Register(() => { try { rangeReq.Abort(); } catch { /* ignored */ } });
            var rangeResp = (HttpWebResponse)await rangeReq.GetResponseAsync().ConfigureAwait(false);
            using (rangeResp)
            {
                // Prefer Content-Range total: "bytes 0-0/<total>" (also handles "bytes */<total>").
                var contentRange = rangeResp.Headers["Content-Range"];
                if (!string.IsNullOrEmpty(contentRange))
                {
                    var slash = contentRange.LastIndexOf('/');
                    if (slash >= 0 && slash + 1 < contentRange.Length &&
                        long.TryParse(contentRange.Substring(slash + 1), out var total) && total >= 0)
                        return total;
                }

                // Server ignored Range (200 OK, no Content-Range). Full Content-Length is available
                // on the header. Return it without draining the body; disposing the response drops
                // the connection. (On Range-ignoring servers this abandons a full body; prefer HEAD.)
                return rangeResp.ContentLength;
            }
        }
        catch (WebException ex)
        {
            // e.g. 416 Range Not Satisfiable, 404, etc. Dispose the error body so it isn't abandoned.
            (ex.Response as HttpWebResponse)?.Dispose();
        }
        catch (Exception) { /* transient failure */ }

        // If cancellation triggered the Abort, surface it as OCE rather than silently returning -1.
        token.ThrowIfCancellationRequested();
        return -1;
    }

    /// <summary>
    /// Creates an <see cref="HttpWebRequest"/> preconfigured for probing (UA + bounded timeout).
    /// </summary>
    private static HttpWebRequest CreateProbeRequest(Uri url)
    {
        var req = WebRequest.CreateHttp(url);
        req.Timeout = ProbeTimeoutMilliseconds;
        req.ReadWriteTimeout = ProbeTimeoutMilliseconds;
        req.UserAgent = ApplicationUserAgent;
        return req;
    }

    /// <summary>
    /// Applies the library's User-Agent (see <see cref="ApplicationUserAgent"/>) to a download
    /// <see cref="HttpWebRequest"/> created by a resolver.
    /// </summary>
    /// <param name="request">Request to configure.</param>
    public static void ApplyUserAgent(HttpWebRequest request)
    {
        if (request == null)
            return;

        request.UserAgent = ApplicationUserAgent;
    }
}
