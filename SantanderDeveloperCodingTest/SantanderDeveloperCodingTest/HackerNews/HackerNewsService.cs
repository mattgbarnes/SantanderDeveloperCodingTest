using System.Collections.Concurrent;
using System.Text.Json;

namespace SantanderDeveloperCodingTest.HackerNews
{
    public interface IHackerNewsService
    {
        Task<List<StoryDetails>> GetBestStories(int n);
    }

    public class HackerNewsService : IHackerNewsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _keyLocks = new ConcurrentDictionary<int, SemaphoreSlim>();
        private readonly ConcurrentDictionary<int, StoryDetails> _cache = new ConcurrentDictionary<int, StoryDetails>();
        private readonly int _bestStoriesTimeout;
        private readonly bool _doBestStoriesExpire;
        private readonly int _itemDetailsTimeout;
        private readonly bool _doItemDetailsExpire;
        private List<int> _bestStoriesIds = new List<int>();
        private DateTime _bestStoriesExpiry;

        public HackerNewsService(IConfiguration configuration, ILogger<HackerNewsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _bestStoriesTimeout = int.Parse(_configuration["CacheTimeoutsMs:BestStories"]);
            _itemDetailsTimeout = int.Parse(_configuration["CacheTimeoutsMs:ItemDetails"]);

            _doBestStoriesExpire = _bestStoriesTimeout >= 0;
            _doItemDetailsExpire = _itemDetailsTimeout >= 0;

            var bestStoriesExpireNarrative = _doBestStoriesExpire ? "" : " (never expire)";
            var itemDetailsExpireNarrative = _doItemDetailsExpire ? "" : " (never expire)";

            _logger.LogInformation($"BestStoriesTimeout: {_bestStoriesTimeout}{bestStoriesExpireNarrative}, ItemDetailsTimeout: {_itemDetailsTimeout}{itemDetailsExpireNarrative}");
        }

        public async Task<List<StoryDetails>> GetBestStories(int n)
        {
            if (n < 0)
                throw new Exception("Can't return a negative number of stories");

            if (n == 0)
                return new List<StoryDetails>();

            var bestStoryIds = await GetBestStoriesIds();

            if (n > bestStoryIds.Count)
                n = bestStoryIds.Count;

            var nBestStoryIds = bestStoryIds.Take(n);
            var storyDetails = await GetStoryDetails(nBestStoryIds);

            return storyDetails;
        }

        private async Task<List<int>> GetBestStoriesIds()
        {
            if (!_doBestStoriesExpire && _bestStoriesIds.Any())
                return _bestStoriesIds;

            if (DateTime.Now < _bestStoriesExpiry)
                return _bestStoriesIds;

            var query = _configuration["HackerNewsUrls:BestStories"];

            using (var httpClient = new HttpClient())
            {
                var data = await httpClient.GetStringAsync(query);
                try
                {
                    var results = JsonSerializer.Deserialize<int[]>(data);
                    if (results != null)
                        RemoveUnneededDetails(results);
                    _bestStoriesIds = results.ToList();
                    _bestStoriesExpiry = DateTime.Now.AddMilliseconds(_bestStoriesTimeout);
                    return _bestStoriesIds;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error trying to parse StockTimeSeries json ({data}).\n{ex}");
                }
            }

            return new List<int>();
        }

        private void RemoveUnneededDetails(int[] results)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                var idsToRemove = new List<int>();
                foreach (var kvp in _cache)
                {
                    if (!results.Contains(kvp.Key))
                    {
                        idsToRemove.Add(kvp.Key);
                    }
                }

                foreach (var idToRemove in idsToRemove)
                {
                    _cache.Remove(idToRemove, out _);
                    _keyLocks.Remove(idToRemove, out _);
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        private Task<List<StoryDetails>> GetStoryDetails(IEnumerable<int> ids)
        {
            var storyDetails = new ConcurrentBag<StoryDetails>();
            
            _cacheLock.EnterReadLock();
            try
            {
                Parallel.ForEach(ids, id =>
                {
                    storyDetails.Add(GetStoryDetails(id).Result);
                });
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
            var orderedStoryDetails = storyDetails.OrderByDescending(sd => sd.Score).ToList();
            _logger.LogInformation($"OrderedStoryDetails (count: {orderedStoryDetails.Count})");
            return Task.FromResult(orderedStoryDetails);
        }

        private async Task<StoryDetails> GetStoryDetails(int id)
        {
            StoryDetails value;

            // Lock on individual items
            var keyLock = _keyLocks.GetOrAdd(id, x => new SemaphoreSlim(1));
            await keyLock.WaitAsync().ConfigureAwait(false);
            try
            {
                // try to get the item from the cache - if it's not there or it has expired then fetch it
                if (!_cache.TryGetValue(id, out value) || (value.Expiry < DateTime.Now && _doItemDetailsExpire))
                {
                    var query = _configuration["HackerNewsUrls:ItemDetails"];
                    query = query.Replace("[id]", id.ToString());

                    using (var httpClient = new HttpClient())
                    {
                        var data = await httpClient.GetStringAsync(query).ConfigureAwait(false);
                        try
                        {
                            var itemDetails = JsonSerializer.Deserialize<ItemDetails>(data);
                            value = itemDetails.ToStoryDetails();
                            value.Expiry = DateTime.Now.AddMilliseconds(_itemDetailsTimeout);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error trying to parse StockTimeSeries json ({data}).\n{ex}");
                        }
                    }

                    // cache value
                    _cache.AddOrUpdate(id, value, (i, v) => v);
                }
            }
            finally
            {
                keyLock.Release();
            }

            return value;
        }
    }
}
