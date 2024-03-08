using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api1.HealthChecking
{
    public class MainHealthCheck : IHealthCheck
    {
        private readonly IEnumerable<IHealthCheck> _checks;

        public MainHealthCheck(IEnumerable<IHealthCheck> checks)
        {
            _checks = checks;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var results = await Task.WhenAll(_checks.Select(c => c.CheckHealthAsync(context, cancellationToken)));

            var unhealthyResults = results.Where(r => r.Status == HealthStatus.Unhealthy).ToList();
            if (unhealthyResults.Count != 0)
            {
                var unhealthyReasons = unhealthyResults.Select(r => r.Description).ToList();
                var exceptions = unhealthyResults.Select(r => r.Exception).Where(e => e != null).ToList();
                return new HealthCheckResult(status: context.Registration.FailureStatus,
                                             description: string.Join(", ", unhealthyReasons),
                                             exception: new AggregateException(exceptions));
            }

            return HealthCheckResult.Healthy();
        }
    }
}
