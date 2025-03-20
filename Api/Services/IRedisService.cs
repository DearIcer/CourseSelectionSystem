namespace Api.Services;

public interface IRedisService
{
    Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiry);
    Task ReleaseLockAsync(string key, string value);
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<string> GetStringAsync(string key);
    Task<long> DecrementAsync(string key);
    Task<long> IncrementAsync(string key);
    Task<bool> KeyExistsAsync(string key);
    Task RemoveKeyAsync(string key);
    Task<bool> SetNxAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> SetCourseStockAsync(int courseId, int stock);
    Task<long> DecrementCourseStockAsync(int courseId);
    Task<long> GetCourseStockAsync(int courseId);
}