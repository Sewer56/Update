using System;
using System.Net;
using System.Net.Http;
using CacheCow.Client;
using Octokit.Internal;

using Octokit;

namespace Sewer56.Update.Resolvers.GitHub;

internal static class GitHubClientInstance
{
    private static GitHubClient? _client;

    /// <summary>
    /// Tries to get an instance of the GitHub client, if possible.
    /// </summary>
    /// <param name="ex">Any exception, if caught.</param>
    /// <returns>A valid instance if successful, else null and exception.</returns>
    public static GitHubClient? TryGet(out Exception? ex)
    {
        ex = null;
        if (_client != null)
            return _client;

        try
        {
            var productHeader = new ProductHeaderValue("Sewer56.Update.Resolvers.GitHub");
            _client = new GitHubClient(new Connection(productHeader, new HttpClientAdapter(GetHandler)));
            return _client;
        }
        catch (Exception e)
        {
            ex = e;
            return null;
        }
    }

    private static HttpMessageHandler GetHandler()
    {
        var cachingHandler = new CachingHandler(new AkavacheContentStore());
        cachingHandler.UseConditionalPutPatchDelete = true;
        cachingHandler.InnerHandler = new HttpClientHandler();
        return cachingHandler;
    }
}