namespace DataOnion.Auth
{
    internal interface IAuthServiceStrategy<T>
    {
        Task<T?> LoginAsync(T userSession);
        Task<T?> GetSessionAsync(string id);
        Task LogoutAsync(string id);
    }
}