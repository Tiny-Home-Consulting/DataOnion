using Microsoft.Extensions.Logging;

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
        private readonly ILogger? _logger;

        public AuthService(
            IAuthServiceStrategy<T> strategy,
            ILogger<AuthService<T>>? logger = null
        )
        {
            _strategy = strategy;
            _logger = logger;
        }

        public async Task<T?> LoginAsync(T userSession)
        {
            _logger?.LogDebug(
                "Logging in with strategy {0}",
                _strategy.GetType()
            );
            var session = await _strategy.LoginAsync(userSession);
            _logger?.LogDebug(
                "Finished logging in."
            );
            return session;
        }

        public async Task<T?> GetSessionAsync(string id)
        {
            _logger?.LogDebug(
                "Getting session with strategy {0}",
                _strategy.GetType()
            );
            var session = await _strategy.GetSessionAsync(id);
            _logger?.LogDebug(
                "Finished getting session."
            );
            return session;
        }

        public async Task LogoutAsync(string id)
        {
            _logger?.LogDebug(
                "Logging out with strategy {0}",
                _strategy.GetType()
            );
            await _strategy.LogoutAsync(id);
            _logger?.LogDebug(
                "Finished logging out."
            );
        }
    }
}