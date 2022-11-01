using DataOnion.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace DataOnion.Auth
{
    public interface ITwoFactorAuthService
    {
        Task<Guid> RegisterDidAsync(
            RegisterDidParams parameters
        );

        Task<bool> VerifyDidAsync(
            VerifyDidParams parameters
        );
    }

    public class TwoFactorAuthService : ITwoFactorAuthService
    {
        private readonly ApplicationConfig _appConfig; // this should come from the client
        private readonly IRedisContext _redisContext;
        private readonly INumberGenerator _numberGenerator; // this can come from DataOnion
        private readonly IUserRepository<T> _userRepository; // this has to come from the client
        private readonly IDapperService<DbConnection> _dapper;
        private readonly IEFCoreService<DbContext> _efCore;
        private readonly ILogger _logger;

        public TwoFactorAuthService(
            ApplicationConfig appConfig,
            IRedisContext redisContext,
            INumberGenerator numberGenerator,
            ICpcUserRepository cpcUserRepository,
            IDapperService<DbConnection> dapper,
            IEFCoreService<DbContext> efCore,
            ILogger<TwoFactorAuthService> logger
        )
        {
            _appConfig = appConfig;
            _redisContext = redisContext;
            _numberGenerator = numberGenerator;
            _cpcUserRepository = cpcUserRepository;
            _efCore = efCore;
            _dapper = dapper;
            _logger = logger;
        }


        public async Task<Guid> RegisterDidAsync(
            RegisterDidParams parameters
        )
        {
            var did = parameters.Did;
            var method = parameters.Method;
            var language = parameters.Language;
            var userId = parameters.UserId;
            var throttleTimeout = _appConfig.TwoFactorAuthentication.TwoFactorThrottleTimeoutSeconds;

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

            var code = _numberGenerator.Generate2FACode();
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

                var existingUser = await _cpcUserRepository.FetchAsync(userId);

                if (existingUser == null)
                {
                    var newCpcUser = new CpcUser()
                    {
                        Id = userId
                    };

                    existingUser = await _cpcUserRepository.CreateAsync(newCpcUser);
                }

                var existingDid = await _efCore.FetchAsync(d
                    => d.Number == recentRequest.Did);

                if (existingDid == null)
                {
                    await _didRepository.CreateAsync(new Did
                    {
                        Number = recentRequest.Did,
                        CpcUser = existingUser
                    });
                }
                else
                {
                    existingDid.CpcUser = existingUser;
                    existingDid.UpdatedAt = DateTime.UtcNow;

                    await _didRepository.UpdateAsync(
                        existingDid.Id,
                        existingDid
                    );
                }

                await _redisContext.DeleteTwoFactorRequestsAsync(
                    userId
                );

                _logger.LogInformation($"Verified did {recentRequest.Did} for CPC user {parameters.UserId}");
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
            
            var throttleTimeout = _appConfig.TwoFactorAuthentication.TwoFactorThrottleTimeoutSeconds;
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

    public class TwoFactorRequest
    {
        public string Did { get; set; }
        public int VerificationCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Token { get; set; }
        public VerificationMethod Method { get; set; }
    }

    public interface IUserRepository<T>
    {
        Task<T?> FetchAsync(int id);
        Task<T> CreateAsync(T entity);
        Task<T?> GetUserAndAppInstancesByIdAsync(int userId);
        Task<T?> GetUserDidsAndAppInstancesByIdAsync(int userId);
    }
}