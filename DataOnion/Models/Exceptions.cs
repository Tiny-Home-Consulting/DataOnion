using System.Net;

namespace DataOnion.Models
{
    public abstract class ApiException : Exception
    {
        public abstract HttpStatusCode StatusCodeEnum { get; }
        public virtual int StatusCode => (int) StatusCodeEnum;
        public ApiException(string message) : base(message) {}
    }

    // Not Found 404
    public class EntityNotFoundException<T> : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.NotFound;

        public EntityNotFoundException(string? message = null) : base(message ?? $"The requested {typeof(T).Name} was not found.") {}
    }

    // Too Many Requests 429
    public class TooManyRequestsException : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.TooManyRequests;
        public int TimeoutRemaining { get; private set;}

        public TooManyRequestsException(int timeoutRemaining, string? message = null ) : base(message ?? $"Too many requests. Please wait {timeoutRemaining} seconds.") 
        {
            TimeoutRemaining = timeoutRemaining;
        }
    }
}