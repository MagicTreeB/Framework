# MagicTree.Framework.Metrics

Standardized Prometheus metrics exposure for Grafana monitoring across all microservices.

## Features

- ✅ **ASP.NET Core Metrics**: HTTP request rates, response times, error rates
- ✅ **Custom Business Metrics**: Counters, gauges, histograms for domain events
- ✅ **Database Metrics**: EF Core query performance (via middleware)
- ✅ **Automatic Collection**: HTTP metrics middleware
- ✅ **Prometheus Integration**: `/metrics` endpoint for scraping

## Configuration

Add to `appsettings.json`:

```json
{
  "Metrics": {
    "Enabled": true,
    "EndpointPath": "/metrics",
    "ApplicationName": "YourApi",
    "Environment": "Production"
  }
}
```

## Usage

### Program.cs

```csharp
using MagicTree.Framework.Metrics.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register metrics services
builder.Services.AddMetricsService(builder.Configuration);

var app = builder.Build();

// Expose /metrics endpoint
app.UseMetrics(builder.Configuration);

app.Run();
```

## Standard Metrics Exposed

### HTTP Metrics (Automatic)

- `http_requests_in_progress` - Current active HTTP requests
- `http_request_duration_seconds` - HTTP request duration histogram
- `http_requests_received_total` - Total HTTP requests by method, controller, action, code

### Custom Metrics Example

```csharp
using Prometheus;

// Define custom metrics
private static readonly Counter UserRegistrations = Metrics.CreateCounter(
    "user_registrations_total",
    "Total user registrations",
    new CounterConfiguration { LabelNames = new[] { "method", "status" } });

private static readonly Histogram QueryDuration = Metrics.CreateHistogram(
    "db_query_duration_seconds",
    "Database query execution time");

// Usage in handlers
UserRegistrations.WithLabels("email", "success").Inc();

using (QueryDuration.NewTimer())
{
    await _repository.GetUserAsync(userId);
}
```

## Prometheus Configuration

Add to `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'mmo-apis'
    scrape_interval: 30s
    static_configs:
      - targets:
          - 'auth-api:5000'
          - 'mmo-api:5051'
          - 'email-api:5170'
          # Add all 16 APIs
```

## Grafana Dashboards

Import dashboard ID **14282** for ASP.NET Core metrics visualization.

### Key Metrics to Monitor

- **Request Rate**: Requests per second
- **Error Rate**: 4xx/5xx responses
- **Latency**: p50, p95, p99 response times
- **Active Requests**: Concurrent requests in-flight

## Kubernetes ServiceMonitor

For Prometheus Operator:

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: mmo-apis
  namespace: monitoring
spec:
  selector:
    matchLabels:
      app: mmo-api
  endpoints:
    - port: http
      path: /metrics
      interval: 30s
```

## Best Practices

1. ✅ **Use labels wisely** - Avoid high-cardinality labels (IDs, timestamps)
2. ✅ **Consistent naming** - Use `_total` suffix for counters, `_seconds` for durations
3. ✅ **Histograms over summaries** - Better for aggregation in Prometheus
4. ✅ **Monitor business metrics** - Not just infrastructure metrics
5. ✅ **Set reasonable scrape intervals** - 30s for most cases

## Dependencies

- `Prometheus.AspNetCore` - Metrics library and HTTP middleware
- `Microsoft.Extensions.Configuration.Binder` - Configuration binding

## Access Metrics

Navigate to `http://localhost:{port}/metrics` to view raw Prometheus metrics.

## Integration with Existing Packages

Works alongside:
- `MagicTree.Framework.RateLimit` - Exposes rate limiting statistics
- `MagicTree.Framework.Idempotency` - Tracks duplicate request rates
- `MagicTree.Framework.HybridCache` - Reports cache hit/miss ratios

## Troubleshooting

**Metrics not appearing?**
- Check `Metrics.Enabled = true` in appsettings.json
- Verify endpoint path accessibility (`/metrics`)
- Ensure middleware order: `UseMetrics()` before `UseEndpoints()`

**High cardinality warning?**
- Review custom metric labels
- Avoid user IDs, request IDs, or timestamps as labels
- Use histogram buckets for time-based metrics
