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
        public bool IsSameSessionAs(LoginData other) => (SessionId == other.SessionId);
        public IEnumerable<HashEntry> ToRedisHash()
        {
            return new[]
            {
                new HashEntry("username", Username),
                new HashEntry("password", Password),
                new HashEntry("session-id", SessionId.ToString())
            };
        }
    }
}