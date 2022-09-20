using StackExchange.Redis;

namespace DataOnion.Auth
{
    public interface IAuthStorable<T>
    {
        string GetId();
        bool IsSameSessionAs(T other);
        IEnumerable<HashEntry> ToRedisHash();
    }
}