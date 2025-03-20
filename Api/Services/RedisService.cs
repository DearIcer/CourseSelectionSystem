using StackExchange.Redis;

namespace Api.Services;

public class RedisService : IRedisService
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key, string value)
    {
        var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

        await _db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { value });
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry);
    }

    public async Task<string> GetStringAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task<long> DecrementAsync(string key)
    {
        return await _db.StringDecrementAsync(key);
    }

    public async Task<long> IncrementAsync(string key)
    {
        return await _db.StringIncrementAsync(key);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task RemoveKeyAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> SetNxAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    public async Task<bool> SetCourseStockAsync(int courseId, int stock)
    {
        string key = $"course:{courseId}:stock";
        return await SetStringAsync(key, stock.ToString());
    }

    public async Task<long> DecrementCourseStockAsync(int courseId)
    {
        string key = $"course:{courseId}:stock";
        return await DecrementAsync(key);
    }

    public async Task<long> GetCourseStockAsync(int courseId)
    {
        string key = $"course:{courseId}:stock";
        var value = await GetStringAsync(key);
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return long.Parse(value);
    }
}