namespace DataOnion.Auth
{
    public interface IAuthService<T>
    {
        Task<bool> LoginAsync(T details);
        Task<T?> CheckSessionAsync(string id);
        Task LogoutAsync(string id);
        Task<bool> SetAbsoluteExpirationAsync(string id, TimeSpan expiration);
    }
}