using DataOnion.Auth;
using StackExchange.Redis;

namespace SampleApp.Models
{

    public class LoginData : IAuthStorable<LoginData>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Guid SessionId { get; set; }

        public LoginData(
            string username,
            string password,
            Guid sessionId
        )
        {
            Username = username;
            Password = password;
            SessionId = sessionId;
        }

        public LoginData(
            HashEntry[] hash
        )
        {
            var dict = hash.ToStringDictionary();
            Username = dict["username"];
            Password = dict["password"];
            SessionId = Guid.Parse(dict["session-id"]);
        }

        public string GetId() => SessionId.ToString();

        public IEnumerable<HashEntry> ToRedisHash()
        {
            return new[]
            {
                new HashEntry("username", Username),
                new HashEntry("password", Password),
                new HashEntry("session-id", SessionId.ToString())
            };
        }

        public bool Equals(LoginData? other) => (SessionId == other?.SessionId);

        // These are not strictly required, but recommended by the documentation for IEquatable
        public override bool Equals(object? obj)
        {
            if (obj != null && obj is LoginData)
            {
                return SessionId == ((LoginData)obj).SessionId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return SessionId.GetHashCode();
        }

        public static bool operator==(LoginData? lhs, LoginData? rhs)
        {
            return lhs?.Equals(rhs) ?? false;
        }

        public static bool operator!=(LoginData? lhs, LoginData? rhs)
        {
            return !(lhs == rhs);
        }
    }
}