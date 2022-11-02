using DataOnion.db;
using DataOnion.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Net;

namespace DataOnion.Auth
{
    public interface ITwoFactorAuthService<TDid, TUser>
        where TDid : IDid
        where TUser : IUser
    {
        Task<Guid> RegisterDidAsync(
            RegisterDidParams parameters
        );

        Task<bool> VerifyDidAsync(
            VerifyDidParams parameters
        );
    }

    public class TwoFactorAuthService<TDid, TUser> : ITwoFactorAuthService<TDid, TUser>
        where TDid : IDid, new()
        where TUser : IUser, new()
    {
        private readonly ITwoFactorRedisContext _redisContext;
        private readonly IDapperService<DbConnection> _dapper;
        private readonly IEFCoreService<DbContext> _efCore;
        private readonly ILogger? _logger;

        private readonly int _twoFactorThrottleTimeoutSeconds;
        private Func<int> _generate2FACode;

        public TwoFactorAuthService(
            ITwoFactorRedisContext redisContext,
            IDapperService<DbConnection> dapper,
            IEFCoreService<DbContext> efCore,
            ILogger<TwoFactorAuthService<TDid, TUser>>? logger,
            int twoFactorThrottleTimeoutSeconds,
            Func<int> generate2FACode
        )
        {
            _redisContext = redisContext;
            _efCore = efCore;
            _dapper = dapper;
            _logger = logger;
            _twoFactorThrottleTimeoutSeconds = twoFactorThrottleTimeoutSeconds;
            _generate2FACode = generate2FACode;
        }


        public async Task<Guid> RegisterDidAsync(
            RegisterDidParams parameters
        )
        {
            var did = parameters.Did;
            var method = parameters.Method;
            var userId = parameters.UserId;

            switch (method)
            {
                case VerificationMethod.Call:
                    var recentCallRequests = await _redisContext.FetchTwoFactorRequestsAsync(
                        userId,
                        VerificationMethod.Call
                    );

                    var recentCallRequest = recentCallRequests.FirstOrDefault();
                    var callRequestValidity = CheckTwoFactorTimeout(recentCallRequest);

                    if (recentCallRequest != null && !callRequestValidity.Item1)
                    {
                        throw new TooManyRequestsException(
                            callRequestValidity.Item2
                        );
                    }
                    break;
                case VerificationMethod.Text:
                    var recentTextRequests = await _redisContext.FetchTwoFactorRequestsAsync(
                        userId,
                        VerificationMethod.Text
                    );

                    var recentTextRequest = recentTextRequests.FirstOrDefault();
                    var textRequestValidity = CheckTwoFactorTimeout(recentTextRequest);

                    if (recentTextRequest != null && !textRequestValidity.Item1)
                    {
                        throw new TooManyRequestsException(
                            textRequestValidity.Item2
                        );
                    }
                    break;
                // In the case that we ever add another form of 2FA I don't
                // want this implementation to prevent it from working if
                // we forget to update this.
                default:
                    break;
            }

            var code = _generate2FACode.Invoke();
            var token = Guid.NewGuid();

            await _redisContext.CreateTwoFactorRequestAsync(
                userId, 
                new TwoFactorRequest
                {
                    Did = did,
                    VerificationCode = code,
                    CreatedAt = DateTime.UtcNow,
                    Token = token,
                    Method = method
                }
            );

            return token;
        }

        public async Task<bool> VerifyDidAsync(
            VerifyDidParams parameters
        )
        {
            var userId = parameters.UserId;
            var code = parameters.Code;
            var method = parameters.Method;
            var token = parameters.Token;

            TwoFactorRequest? recentRequest = null;

            if (method != VerificationMethod.Call && recentRequest == null)
            {
                // Text or Unknown
                var recentTextRequest = (await _redisContext.FetchTwoFactorRequestsAsync(
                    userId,
                    VerificationMethod.Text
                )).FirstOrDefault();

                if (recentTextRequest != null && recentTextRequest.VerificationCode == code)
                {
                    recentRequest = recentTextRequest;
                }
            }
            if (method != VerificationMethod.Text && recentRequest == null)
            {
                // Call or Unknown
                var recentCallRequest = (await _redisContext.FetchTwoFactorRequestsAsync(
                    userId,
                    VerificationMethod.Call
                )).FirstOrDefault();

                if (recentCallRequest != null && recentCallRequest.VerificationCode == code)
                {
                    recentRequest = recentCallRequest;
                }
            }

            if (recentRequest != null)
            {
                if (token != recentRequest.Token)
                {
                    throw new EntityNotfoundException<TwoFactorRequest>();
                }

                var existingUser = await _efCore.FetchAsync<TUser, int>(userId);

                if (existingUser == null)
                {
                    var newUser = new TUser()
                    {
                        Id = userId
                    };

                    existingUser = await _efCore.CreateAsync<TUser>(newUser);
                }

                var existingDid = await _efCore.FetchAsync<TDid>(d
                    => d.Number == recentRequest.Did);

                if (existingDid == null)
                {
                    await _efCore.CreateAsync(new TDid
                    {
                        Number = recentRequest.Did,
                        User = existingUser
                    });
                }
                else
                {
                    existingDid.User = existingUser;
                    existingDid.UpdatedAt = DateTime.UtcNow;

                    await _efCore.UpdateAsync(
                        existingDid.Id,
                        existingDid
                    );
                }

                await _redisContext.DeleteTwoFactorRequestsAsync(
                    userId
                );

                _logger?.LogInformation($"Verified did {recentRequest.Did} for CPC user {parameters.UserId}");
            }

            // If the code didn't match, recentRequest == null
            // If the token didn't match, it'd throw an error before reaching here
            // Therefore, verified = (recentRequest != null)
            return recentRequest != null;
        }

                private string ReplaceDigitTags(int code, string body)
        {
            var codeString = code.ToString();
            for (int i = 0; i < 6; i++)
            {
                var digitTag = $"[d{i + 1}]";
                var replacement = codeString[i].ToString();

                body = body.Replace(digitTag, replacement);
            }

            return body;
        }

        // Checks the TwoFactorRequest's timestamp to see if it has passed the throttle timeout yet.
        private (bool, int) CheckTwoFactorTimeout(TwoFactorRequest? existingRequest)
        {
            if (existingRequest == null)
            {
                return (false, 0);
            }
            
            var throttleTimeout = _twoFactorThrottleTimeoutSeconds;
            var timeoutSpan = TimeSpan.FromSeconds(throttleTimeout);
            var createdAt = existingRequest.CreatedAt;
            var now = DateTime.UtcNow;

            // Get the difference between now and when the request was created.
            var diff = now - createdAt;

            // If the difference between now and when the request was made is greater than
            // the timeout period then the request is not valid. Calculate 
            // the seconds remaining by subtracting the timeout from the difference in
            // seconds between now and when the request was created.

            var ret = (
                diff > timeoutSpan,
                throttleTimeout - (int) diff.TotalSeconds
            );

            return ret;
        }
    }

    public class RegisterDidParams
    {
        public int UserId { get; private set; }
        public string Did { get; private set; }
        public string Language { get; private set; }
        public VerificationMethod Method { get; private set; }

        public RegisterDidParams(
            int userId,
            string did,
            string language,
            VerificationMethod method
        )
        {
            UserId = userId;
            Did = did;
            Language = language;
            Method = method;
        }
    }

    public class VerifyDidParams
    {
        public Guid Token { get; private set; }
        public int Code { get; private set; }
        public int UserId { get; private set; }
        public VerificationMethod Method { get; private set; }

        public VerifyDidParams(
            Guid token,
            int code,
            int userId,
            VerificationMethod method
        )
        {
            Token = token;
            Code = code;
            UserId = userId;
            Method = method;
        }
    }

    public enum VerificationMethod
    {
        Unknown,
        Call,
        Text
    }

    public class ExceptionWithCode : Exception {
        public HttpStatusCode StatusCode;
        public ExceptionWithCode(
            HttpStatusCode statusCode,
            string message
        ) : base(message) {
            StatusCode = statusCode;
        }
    }
}