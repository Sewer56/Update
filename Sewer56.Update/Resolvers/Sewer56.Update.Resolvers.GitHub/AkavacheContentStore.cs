using System;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;
// ReSharper disable once RedundantUsingDirective
using System.Reactive.Linq; // IMPORTANT - this makes await work!
using Akavache;
using Akavache.Sqlite3;
using CacheCow.Client;
using CacheCow.Common;
using CacheCow.Common.Helpers;

namespace Sewer56.Update.Resolvers.GitHub;

internal class AkavacheContentStore : ICacheStore
{
    private static readonly TimeSpan _expiration = TimeSpan.FromDays(30);

    static AkavacheContentStore()
    {
        const string DatabaseName = "Sewer56.Update.Resolvers.GitHub";

        Akavache.Registrations.Start(DatabaseName);
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseName, "Cache.db");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        Cache = new SQLitePersistentBlobCache(filePath);
    }

    private static readonly IBlobCache Cache;

    private MessageContentHttpMessageSerializer _messageSerializer = new MessageContentHttpMessageSerializer(true);

    public void Dispose() => Cache.Vacuum();

    public async Task<HttpResponseMessage> GetValueAsync(CacheKey key)
    {
        var observable = Cache.Get(key.ToString());
        var buffer = await observable.Catch(Observable.Return<byte[]>(null!));
        if (buffer != null)
        {
            await using var memoryStream = new MemoryStream(buffer);
            return await _messageSerializer.DeserializeToResponseAsync(memoryStream).ConfigureAwait(false);
        }

        return null;
    }

    public async Task AddOrUpdateAsync(CacheKey key, HttpResponseMessage response)
    {
        var req = response.RequestMessage;
        response.RequestMessage = null!;
        await using var memoryStream = new MemoryStream();
        await _messageSerializer.SerializeAsync(response, memoryStream).ConfigureAwait(false);
        response.RequestMessage = req;
        await Cache.Insert(key.ToString(), memoryStream.ToArray(), _expiration);
    }

    public async Task<bool> TryRemoveAsync(CacheKey key)
    {
        await Cache.Invalidate(key.ToString());
        return true;
    }

    public async Task ClearAsync() => await Cache.InvalidateAll();
}