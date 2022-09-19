using StackExchange.Redis;
using System.Reflection;

namespace DataOnion.Auth
{
    public static class Utils
    {
        public static IEnumerable<HashEntry> MakeRedisHashEntries<T>(T obj)
        {
            var properties = typeof(T).GetProperties(
                BindingFlags.Instance | BindingFlags.Public
            );

            var nameValuePairs = properties
                .Select(property => (property.Name, Value: property.GetValue(obj)))
                .Where(pair => pair.Value != null);

            return nameValuePairs
                .Select(pair => new HashEntry(pair.Name, pair.Value.ToString()));
        }

        public static T ConstructFromRedisHash<T>(HashEntry[] values)
            where T : new()
        {
            var retval = new T();

            foreach (var hashEntry in values)
            {
                var property = typeof(T).GetProperty(
                    hashEntry.Name.ToString(),
                    BindingFlags.Instance | BindingFlags.Public
                );

                if (property == null)
                {
                    continue;
                }

                var propertyType = property.GetType();
                var stringCtor = propertyType.GetConstructor(
                    BindingFlags.Public,
                    new[]
                    {
                        typeof(string)
                    }
                );

                if (stringCtor == null)
                {
                    // TODO: figure out this case
                    throw new Exception();
                }

                property.SetValue(
                    retval,
                    stringCtor.Invoke(new object?[]
                    {
                        hashEntry.Value.ToString()
                    })
                );
            }

            return retval;
        }
    }
}