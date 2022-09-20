using StackExchange.Redis;

namespace DataOnion.Auth
{
    public interface IAuthStorable<T> : IEquatable<T>
    {
        string GetId();
        IEnumerable<HashEntry> ToRedisHash();
    }
}