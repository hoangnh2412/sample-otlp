using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Infrastructure.Caching.Redis
{
    public class RedisCacheService : ICacheService
    {
        private IConnectionMultiplexer _connection;
        private IDatabase _database;
        private readonly RedisCacheOptions _options;

        public RedisCacheService(
            IOptions<RedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
                throw new ArgumentNullException(nameof(optionsAccessor));

            _options = optionsAccessor.Value;
        }

        public async Task<List<string>> GetKeysAsync(string pattern, CancellationToken token = default)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            token.ThrowIfCancellationRequested();
            await ConnectAsync();

            var keys = new List<string>();
            foreach (var item in _options.ConfigurationOptions.EndPoints)
            {
                var server = _connection.GetServer(item);
                var data = server.Keys(pattern: $"{_options.InstanceName}{pattern}").Select(x => x.ToString());

                foreach (var element in data)
                {
                    var key = element;
                    if (element.StartsWith(_options.InstanceName))
                        key = element.Substring(_options.InstanceName.Length, element.Length - _options.InstanceName.Length);

                    keys.Add(key);
                }
            }
            return keys;
        }

        public async Task KeyExistAsync(string pattern, CancellationToken token = default)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            token.ThrowIfCancellationRequested();

            // if (!key.StartsWith(_options.InstanceName))
            //     key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();
        }

        public async Task<Dictionary<string, string>> HashGetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAllAsync(key);
            return data.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        }

        public async Task<string> HashGetAsync(string key, string hashField, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAsync(key, hashField);
            if (data.ToString() == null)
                return default;

            return data.ToString();
        }

        public async Task<List<T>> HashGetAsync<T>(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAllAsync(key);
            return data.Select(x => JsonConvert.DeserializeObject<T>(x.Value.ToString())).ToList();
        }

        public async Task<List<T>> HashGetAsync<T>(string key, List<string> hashFields, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAsync(key, hashFields.Select(x => RedisValue.Unbox(x)).ToArray());
            var items = new List<T>();
            foreach (var item in data)
            {
                var element = item.ToString();
                if (element != null)
                    items.Add(JsonConvert.DeserializeObject<T>(element));
            }
            return items;
        }

        public async Task<List<string>> HashGetAsync(string key, List<string> hashFields, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAsync(key, hashFields.Select(x => RedisValue.Unbox(x)).ToArray());
            var items = new List<string>();
            foreach (var item in data)
            {
                var element = item.ToString();
                if (element != null)
                    items.Add(element);
            }
            return items;
        }

        public async Task<T> HashGetAsync<T>(string key, string hashField, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            var data = await _database.HashGetAsync(key, hashField);
            if (data.ToString() == null)
                return default;

            return JsonConvert.DeserializeObject<T>(data.ToString());
        }

        public async Task HashSetAsync<T>(string key, Dictionary<string, T> data, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.HashSetAsync(key, data.Select(x => new HashEntry(x.Key, JsonConvert.SerializeObject(x.Value))).ToArray());

            if (options != null && options.AbsoluteExpirationRelativeToNow.HasValue)
                await _database.KeyExpireAsync(key, options.AbsoluteExpirationRelativeToNow.Value);
        }

        public async Task HashSetAsync(string key, Dictionary<string, string> data, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.HashSetAsync(key, data.Select(x => new HashEntry(x.Key, x.Value)).ToArray());

            if (options != null && options.AbsoluteExpirationRelativeToNow.HasValue)
                await _database.KeyExpireAsync(key, options.AbsoluteExpirationRelativeToNow.Value);
        }

        public async Task HashSetAsync(string key, string hashField, string data, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.HashSetAsync(key, hashField, data);

            if (options != null && options.AbsoluteExpirationRelativeToNow.HasValue)
                await _database.KeyExpireAsync(key, options.AbsoluteExpirationRelativeToNow.Value);
        }

        public async Task HashDeleteAsync(string key, string hashField, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.HashDeleteAsync(key, hashField);
        }

        public async Task HashDeleteAsync(string key, string[] hashFields, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.HashDeleteAsync(key, hashFields.Select(x => RedisValue.Unbox(x)).ToArray());
        }

        public async Task<long> PushAsync(string key, string value, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            return await _database.ListRightPushAsync(key, value);
        }

        public async Task<long> PushAsync(string key, string[] values, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            return await _database.ListRightPushAsync(key, values.Select(x => RedisValue.Unbox(x)).ToArray());
        }

        public async Task<string> PopAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            token.ThrowIfCancellationRequested();

            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            return await _database.ListLeftPopAsync(key);
        }

        public async Task ExecuteCommandAsync(string key)
        {
            await ConnectAsync();
            // var result = _cache.HashScan(
            //     key: "Test",
            //     pattern: "*001*",
            //     pageSize: 1,
            //     pageOffset: 0,
            //     cursor: 5).ToList();

            await _database.HashSetAsync("Test", "20211013002", "bbbb");
            // var result = _cache.HashScan("Test", "*001*", 1).ToList();
            // foreach (var item in _options.ConfigurationOptions.EndPoints)
            // {
            //     var server = _connection.GetServer(item);
            //     var data = server.Keys(pattern: $"*001").ToList();
            // }
        }

        private async Task ConnectAsync(CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();
            _connection = await RedisConnector.ConnectAsync(_options, token);
            _database = RedisConnector.GetDatabase();
        }

        public async Task<T> QueryCacheKeyAsync<T>(string cache, Func<Task<T>> query, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            var bytes = await GetAsync(cache);
            if (bytes != null)
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));

            var data = await query.Invoke();
            await SetAsync(cache, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)), options);
            return data;
        }

        public async Task<T> QueryHashKeyAsync<T>(string cache, string key, Func<Task<T>> query, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            var data = await HashGetAsync<T>(cache, key);
            if (data != null)
                return data;

            var item = await query.Invoke();
            await HashSetAsync(cache, key, JsonConvert.SerializeObject(item));
            return item;
        }

        public async Task<List<T>> QueryHashKeysAsync<T>(string cache, List<string> keys, Func<Task<List<T>>> query, Func<List<T>, Dictionary<string, T>> parser, DistributedCacheEntryOptions options = null, CancellationToken token = default)
        {
            var data = await HashGetAsync<T>(cache, keys);
            if (data != null && data.Count == keys.Count)
                return data;

            var item = await query.Invoke();
            await HashSetAsync(cache, parser.Invoke(item));
            return item;
        }

        public byte[] Get(string key)
        {
            return this.GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            string value = await _database.StringGetAsync(key);
            if (value == null)
                return null;

            return Encoding.UTF8.GetBytes(value);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, null).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.StringSetAsync(key, Encoding.UTF8.GetString(value));
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            RemoveAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (!key.StartsWith(_options.InstanceName))
                key = $"{_options.InstanceName}:{key}";

            await ConnectAsync();

            await _database.KeyDeleteAsync(key);
        }
    }
}
