namespace DataOnion.Auth
{
    internal interface IAuthServiceStrategy<T>
    {
        Task<bool> LoginAsync(T userSession);
        Task<T?> CheckSessionAsync(string id);
        Task LogoutAsync(string id);
        Task<bool> SetAbsoluteExpirationAsync(string id, TimeSpan expiration);
    }
}