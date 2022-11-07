using Microsoft.Extensions.Logging;

namespace DataOnion.Auth
{
    public interface ITwoFactorAuthService
    {
        Task<RegisterDidResult> RegisterDidAsync(
            RegisterDidParams parameters
        );

        Task<TwoFactorRequest?> FetchRequestAsync(
            FetchRequestParams parameters
        );

        Task DeleteRequestsAsync(
            int userId
        );
    }

    public class TwoFactorAuthService : ITwoFactorAuthService
    {
        private readonly ITwoFactorRedisContext _redisContext;
        private readonly ILogger? _logger;

        public TwoFactorAuthService(
            ITwoFactorRedisContext redisContext,
            ILogger<TwoFactorAuthService>? logger
        )
        {
            _redisContext = redisContext;
            _logger = logger;
        }

        public async Task<RegisterDidResult> RegisterDidAsync(
            RegisterDidParams parameters
        )
        {
            var did = parameters.Did;
            var method = parameters.Method;
            var userId = parameters.UserId;
            var code = parameters.VerificationCode;
            var throttleTimeout = parameters.ThrottleTimeout;

            bool tooManyRequests = false;
            TwoFactorRequest? newRequest = null;
            int timeoutRemaining = 0;

            switch (method)
            {
                case VerificationMethod.Call:
                    var recentCallRequests = await _redisContext.FetchTwoFactorRequestsAsync(
                        userId,
                        VerificationMethod.Call
                    );

                    var recentCallRequest = recentCallRequests.FirstOrDefault();
                    var callRequestValidity = CheckTwoFactorTimeout(recentCallRequest, throttleTimeout);

                    if (recentCallRequest != null && !callRequestValidity.Item1)
                    {
                        tooManyRequests = true;
                        timeoutRemaining = callRequestValidity.Item2;
                    }
                    break;
                case VerificationMethod.Text:
                    var recentTextRequests = await _redisContext.FetchTwoFactorRequestsAsync(
                        userId,
                        VerificationMethod.Text
                    );

                    var recentTextRequest = recentTextRequests.FirstOrDefault();
                    var textRequestValidity = CheckTwoFactorTimeout(recentTextRequest, throttleTimeout);

                    if (recentTextRequest != null && !textRequestValidity.Item1)
                    {
                        tooManyRequests = true;
                        timeoutRemaining = textRequestValidity.Item2;
                    }
                    break;
                // In the case that we ever add another form of 2FA I don't
                // want this implementation to prevent it from working if
                // we forget to update this.
                default:
                    break;
            }

            if (!tooManyRequests)
            {
                var token = Guid.NewGuid();

                newRequest = new TwoFactorRequest
                {
                    Did = did,
                    VerificationCode = code,
                    CreatedAt = DateTime.UtcNow,
                    Token = token,
                    Method = method
                };

                await _redisContext.CreateTwoFactorRequestAsync(
                    userId, 
                    newRequest
                );
            }

            return new RegisterDidResult(
                tooManyRequests,
                newRequest,
                timeoutRemaining
            );
        }

        public async Task<TwoFactorRequest?> FetchRequestAsync(
            FetchRequestParams parameters
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

            return recentRequest;
        }

        public async Task DeleteRequestsAsync(
            int userId
        )
        {
            await _redisContext.DeleteTwoFactorRequestsAsync(
                userId
            );
        }

        // Checks the TwoFactorRequest's timestamp to see if it has passed the throttle timeout yet.
        private (bool, int) CheckTwoFactorTimeout(TwoFactorRequest? existingRequest, int throttleTimeout)
        {
            if (existingRequest == null)
            {
                return (false, 0);
            }
            
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
        public int VerificationCode { get; private set; }
        public int ThrottleTimeout { get; private set; }

        public RegisterDidParams(
            int userId,
            string did,
            string language,
            VerificationMethod method,
            int verificationCode,
            int throttleTimeout
        )
        {
            UserId = userId;
            Did = did;
            Language = language;
            Method = method;
            VerificationCode = verificationCode;
            ThrottleTimeout = throttleTimeout;
        }
    }

    public class FetchRequestParams
    {
        public Guid Token { get; private set; }
        public int Code { get; private set; }
        public int UserId { get; private set; }
        public VerificationMethod Method { get; private set; }

        public FetchRequestParams(
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

    public class RegisterDidResult
    {
        public bool AreThereTooManyRequests;
        public TwoFactorRequest? Request;
        public int TimeoutRemaining;

        public RegisterDidResult(
            bool areThereTooManyRequests,
            TwoFactorRequest? request,
            int timeoutRemaining
        )
        {
            AreThereTooManyRequests = areThereTooManyRequests;
            Request = request;
            TimeoutRemaining = timeoutRemaining;
        }
    }

    public enum VerificationMethod
    {
        Unknown,
        Call,
        Text
    }
}