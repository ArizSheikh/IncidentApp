using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace IncidentApp.AI.Resilience
{
    public class PollyResilienceService
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncTimeoutPolicy _timeoutPolicy;

        public PollyResilienceService()
        {
            // Retry Policy: Retry up to 3 times with exponential backoff
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception?.Message}");
                    });

            // Circuit Breaker Policy: Break after 5 failures, stay open for 30 seconds
            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, breakDelay) =>
                    {
                        Console.WriteLine($"Circuit broken due to: {exception?.Message}. Will remain open for {breakDelay.TotalSeconds}s");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit reset - allowing requests again");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Circuit is half-open - testing if service is recovered");
                    });

            // Timeout Policy: Timeout after 30 seconds
            _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
        }

        public async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> action, T defaultValue = default)
        {
            try
            {
                return await _timeoutPolicy
                    .WrapAsync(_circuitBreakerPolicy)
                    .WrapAsync(_retryPolicy)
                    .ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"All resilience strategies failed: {ex.Message}");
                return defaultValue;
            }
        }

        public async Task ExecuteWithResilienceAsync(Func<Task> action)
        {
            try
            {
                await _timeoutPolicy
                    .WrapAsync(_circuitBreakerPolicy)
                    .WrapAsync(_retryPolicy)
                    .ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"All resilience strategies failed: {ex.Message}");
            }
        }

        public CircuitState GetCircuitState()
        {
            return _circuitBreakerPolicy.CircuitState;
        }
    }
}
