using StackExchange.Redis;
using System.Net;

namespace DataOnion.Models
{
    public abstract class ApiException : Exception
    {
        public abstract HttpStatusCode StatusCodeEnum { get; }
        public virtual int StatusCode => (int) StatusCodeEnum;
        public ApiException(string message) : base(message) {}
    }

    // Bad Request 400
    public class EntityBadRequestException<T> : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.BadRequest;

        public EntityBadRequestException(string? message = null) : base(message ?? $"The request for {typeof(T).Name} was invalid.") {}
    }

    // Unauthorized 401
    public class UnauthorizedException : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.Unauthorized;

        public UnauthorizedException(string? message = null) : base(message ?? "Attempted to perform an unauthorized operation.") {}
    }

    // Forbidden 403
    public class ForbiddenException : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.Forbidden;

        public ForbiddenException(string? message = null) : base(message ?? "The request was refused") {}
    }

    // Not Found 404
    public class EntityNotFoundException<T> : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.NotFound;

        public EntityNotFoundException(string? message = null) : base(message ?? $"The requested {typeof(T).Name} was not found.") {}
    }

    // Conflict 409
    public class EntityConflictException<T> : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => HttpStatusCode.Conflict;

        public EntityConflictException(string? message = null) : base(message ?? $"A {typeof(T).Name} entity with this ID already exists")  {}
    }

    // ImAlittleTeapot 418
    public class ImALittleTeapotException : ApiException
    {
        public override HttpStatusCode StatusCodeEnum => throw new NotImplementedException();
        
        public override int StatusCode => 418;
        
        public ImALittleTeapotException(string? message = null) : base(message ?? $"I'm a little tea pot!") {}
    }

    //RedisConnectionFailed
    public class RedisConnectionFailedException : RedisException
    {
        public RedisConnectionFailedException(string? message = null) : base(message ?? $"Failed to establish Redis connection.") {}
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