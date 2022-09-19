namespace DataOnion.Auth
{
    public interface IAuthService<T>
    {
        Task<bool> LoginAsync(T details);
        Task<T?> CheckSessionAsync(string id);
        Task LogoutAsync(string id);
        Task<bool> SetAbsoluteExpirationAsync(string id, TimeSpan expiration);
    }

    public class AuthService<T> : IAuthService<T>
    {
        private readonly IAuthServiceStrategy<T> _strategy;

        internal AuthService(
            IAuthServiceStrategy<T> strategy
        )
        {
            _strategy = strategy;
        }

        public async Task<bool> LoginAsync(T details)
        {
            return await _strategy.LoginAsync(details);
        }

        public async Task<T?> CheckSessionAsync(string id)
        {
            return await _strategy.CheckSessionAsync(id);
        }

        public async Task LogoutAsync(string id)
        {
            await _strategy.LogoutAsync(id);
        }

        public async Task<bool> SetAbsoluteExpirationAsync(string id, TimeSpan expiration)
        {
            return await _strategy.SetAbsoluteExpirationAsync(id, expiration);
        }
    }
}