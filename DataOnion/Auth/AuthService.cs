namespace DataOnion.Auth
{
    public interface IAuthService<T>
    {
        Task<T?> LoginAsync(T userSession);
        Task<T?> GetSessionAsync(string id);
        Task LogoutAsync(string id);
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

        public async Task<T?> LoginAsync(T userSession)
        {
            return await _strategy.LoginAsync(userSession);
        }

        public async Task<T?> GetSessionAsync(string id)
        {
            return await _strategy.GetSessionAsync(id);
        }

        public async Task LogoutAsync(string id)
        {
            await _strategy.LogoutAsync(id);
        }
    }
}