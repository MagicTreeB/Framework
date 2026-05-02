# MagicTree.Framework.Hangfire

Comprehensive background job processing package for MMO microservices using Hangfire.

## Overview

`MagicTree.Framework.Hangfire` provides a standardized wrapper around Hangfire for reliable background job execution across all microservices. Supports fire-and-forget, delayed, recurring, and continuation jobs with SQL Server or in-memory storage.

## Features

- ✅ **Multiple Job Types**: Fire-and-forget, delayed, scheduled, recurring, continuations
- ✅ **Persistent Storage**: SQL Server (production) or InMemory (development)
- ✅ **Dashboard UI**: Web-based monitoring at `/hangfire` with role-based authorization
- ✅ **Automatic Retries**: Configurable retry policies with exponential backoff
- ✅ **Worker Pool**: Configurable concurrent worker threads and queue processing
- ✅ **Job Service**: Clean abstraction over Hangfire API with dependency injection
- ✅ **Feature Flag**: Enable/disable Hangfire via configuration

## Installation

### 1. Add Package Reference

```xml
<ItemGroup>
    <ProjectReference Include="..\..\Core\MagicTree.Framework.Hangfire\MagicTree.Framework.Hangfire.csproj" />
</ItemGroup>
```

### 2. Configure appsettings.json

```json
{
  "Hangfire": {
    "Enabled": true,
    "StorageType": "SqlServer",
    "ConnectionString": "Server=localhost,1444;Database=HangfireDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True",
    "Dashboard": {
      "Enabled": true,
      "Route": "/hangfire",
      "RequireAuthentication": true,
      "RequiredRole": "Admin",
      "Title": "Auth API - Hangfire Dashboard"
    },
    "Worker": {
      "WorkerCount": 20,
      "Queues": ["default", "critical", "emails", "reports"],
      "PollingIntervalSeconds": 15
    },
    "Retry": {
      "MaxRetryAttempts": 10,
      "UseExponentialBackoff": true,
      "InitialDelaySeconds": 60
    }
  }
}
```

### 3. Register in Program.cs

```csharp
using MagicTree.Framework.Hangfire.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Hangfire services
builder.Services.AddHangfireServices(builder.Configuration);

var app = builder.Build();

// Add Hangfire dashboard (after UseAuthentication/UseAuthorization)
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboardWithAuth(app.Configuration);

app.Run();
```

## Usage Examples

### 1. Fire-and-Forget Jobs (Immediate Execution)

**Scenario**: Send welcome email after user registration

```csharp
public class UserService
{
    private readonly IJobService _jobService;
    private readonly IEmailService _emailService;

    public UserService(IJobService jobService, IEmailService emailService)
    {
        _jobService = jobService;
        _emailService = emailService;
    }

    public async Task<UserDto> RegisterUserAsync(RegisterRequest request)
    {
        // Create user account
        var user = await CreateUserAsync(request);

        // Enqueue welcome email (fire-and-forget)
        _jobService.Enqueue<IEmailService>(x => x.SendWelcomeEmailAsync(user.Email, user.FirstName));

        return user;
    }
}
```

### 2. Delayed Jobs (Execute After Delay)

**Scenario**: Expire password reset token after 1 hour

```csharp
public class PasswordResetService
{
    private readonly IJobService _jobService;

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var token = Guid.NewGuid().ToString();
        await SaveTokenAsync(email, token);

        // Schedule token expiration after 1 hour
        _jobService.Schedule<IPasswordResetService>(
            x => x.ExpireTokenAsync(token),
            TimeSpan.FromHours(1)
        );

        return token;
    }

    public async Task ExpireTokenAsync(string token)
    {
        await DeleteTokenAsync(token);
        Console.WriteLine($"Token {token} expired");
    }
}
```

### 3. Scheduled Jobs (Execute at Specific Time)

**Scenario**: Generate monthly report on last day of month

```csharp
public class ReportService
{
    private readonly IJobService _jobService;

    public void ScheduleMonthlyReport(int year, int month)
    {
        var lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 0);

        _jobService.Schedule<IReportService>(
            x => x.GenerateMonthlyReportAsync(year, month),
            new DateTimeOffset(lastDayOfMonth)
        );
    }

    public async Task GenerateMonthlyReportAsync(int year, int month)
    {
        // Generate and save report
        Console.WriteLine($"Generating report for {month}/{year}");
    }
}
```

### 4. Recurring Jobs (Scheduled with Cron)

**Scenario**: Daily coupon expiration check at midnight

```csharp
public class CouponService
{
    private readonly IJobService _jobService;

    public void SetupRecurringJobs()
    {
        // Check expired coupons daily at 00:00
        _jobService.AddOrUpdateRecurringJob<ICouponService>(
            "expire-coupons-daily",
            x => x.ExpireOldCouponsAsync(),
            Cron.Daily(0, 0) // "0 0 * * *"
        );

        // Cleanup temp files every Sunday at 02:00
        _jobService.AddOrUpdateRecurringJob<IStorageService>(
            "cleanup-temp-files-weekly",
            x => x.CleanupTempFilesAsync(),
            Cron.Weekly(DayOfWeek.Sunday, 2, 0)
        );

        // Aggregate analytics every hour
        _jobService.AddOrUpdateRecurringJob<IAnalyticsService>(
            "aggregate-analytics-hourly",
            x => x.AggregateHourlyDataAsync(),
            Cron.Hourly()
        );
    }

    public async Task ExpireOldCouponsAsync()
    {
        var expiredCount = await MarkExpiredCouponsAsync();
        Console.WriteLine($"Expired {expiredCount} coupons");
    }
}
```

**Common Cron Expressions:**
```csharp
Cron.Minutely()                          // Every minute: "* * * * *"
Cron.Hourly()                            // Every hour: "0 * * * *"
Cron.Daily(hour, minute)                 // Daily at time: "0 0 * * *"
Cron.Weekly(DayOfWeek.Monday, 9, 0)     // Every Monday 09:00
Cron.Monthly(1, 0, 0)                    // 1st of month at 00:00
Cron.Yearly(1, 1, 0, 0)                  // January 1st at 00:00
```

### 5. Continuation Jobs (Chained Execution)

**Scenario**: Process order → Send confirmation email → Update analytics

```csharp
public class OrderService
{
    private readonly IJobService _jobService;

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = await SaveOrderAsync(request);

        // Process payment (fire-and-forget)
        var paymentJobId = _jobService.Enqueue<IPaymentService>(
            x => x.ProcessPaymentAsync(order.Id)
        );

        // Send confirmation email after payment succeeds
        var emailJobId = _jobService.ContinueWith<IEmailService>(
            paymentJobId,
            x => x.SendOrderConfirmationAsync(order.Id)
        );

        // Update analytics after email sent
        _jobService.ContinueWith<IAnalyticsService>(
            emailJobId,
            x => x.TrackOrderCompletedAsync(order.Id)
        );

        return order;
    }
}
```

### 6. Job Management

**Delete, Requeue, Remove Recurring Jobs**

```csharp
public class JobManagementService
{
    private readonly IJobService _jobService;

    public void DeleteScheduledJob(string jobId)
    {
        var deleted = _jobService.DeleteJob(jobId);
        if (deleted)
        {
            Console.WriteLine($"Job {jobId} deleted");
        }
    }

    public void RetryFailedJob(string jobId)
    {
        var requeued = _jobService.RequeueJob(jobId);
        if (requeued)
        {
            Console.WriteLine($"Job {jobId} requeued");
        }
    }

    public void StopRecurringJob(string jobId)
    {
        _jobService.RemoveRecurringJob(jobId);
        Console.WriteLine($"Recurring job {jobId} removed");
    }
}
```

## Configuration Options

### Hangfire Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable Hangfire |
| `StorageType` | string | `"SqlServer"` | Storage type: `SqlServer`, `InMemory` |
| `ConnectionString` | string | `null` | SQL Server connection string (required for SqlServer) |

### Dashboard Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable dashboard UI |
| `Route` | string | `"/hangfire"` | Dashboard URL path |
| `RequireAuthentication` | bool | `true` | Require authentication to access |
| `RequiredRole` | string | `"Admin"` | Role required for access |
| `Title` | string | `"Hangfire Dashboard"` | Dashboard page title |

### Worker Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `WorkerCount` | int | `20` | Number of concurrent worker threads |
| `Queues` | string[] | `["default"]` | Queue names to process |
| `PollingIntervalSeconds` | int | `15` | Job polling interval |

### Retry Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxRetryAttempts` | int | `10` | Maximum automatic retries |
| `UseExponentialBackoff` | bool | `true` | Enable exponential backoff |
| `InitialDelaySeconds` | int | `60` | Initial retry delay |

## Storage Options

### SQL Server (Production - Recommended)

**Pros:**
- ✅ Persistent (jobs survive app restarts)
- ✅ Distributed (multiple instances share same queue)
- ✅ Transaction support
- ✅ Historical job data retained

**Setup:**
```json
{
  "Hangfire": {
    "StorageType": "SqlServer",
    "ConnectionString": "Server=localhost,1444;Database=HangfireDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

**Database Schema:**
Hangfire creates tables automatically in `Hangfire` schema:
- `Hangfire.Job` - Job definitions
- `Hangfire.State` - Job state history
- `Hangfire.JobQueue` - Enqueued jobs
- `Hangfire.Server` - Active servers
- `Hangfire.Set`, `Hangfire.Hash`, `Hangfire.List`, `Hangfire.Counter` - Internal storage

### InMemory (Development Only)

**Pros:**
- ✅ No database setup required
- ✅ Fast for testing

**Cons:**
- ❌ Jobs lost on app restart
- ❌ Not suitable for production

**Setup:**
```json
{
  "Hangfire": {
    "StorageType": "InMemory"
  }
}
```

## Dashboard Access

Access the dashboard at: `https://your-api.com/hangfire`

**Features:**
- View all jobs (enqueued, scheduled, processing, succeeded, failed)
- Retry failed jobs manually
- Delete jobs
- View job details and execution history
- Monitor server health and worker status
- View recurring job schedules

**Authorization:**
- Requires authentication by default (`RequireAuthentication: true`)
- Requires specific role (`RequiredRole: "Admin"`)
- Customize via `appsettings.json`

## Best Practices

### 1. Use Appropriate Job Types

- **Fire-and-forget**: Quick tasks (emails, notifications)
- **Delayed**: Time-sensitive tasks (token expiration, reminders)
- **Recurring**: Scheduled maintenance (cleanup, reports, aggregation)
- **Continuations**: Multi-step workflows (order processing, data pipelines)

### 2. Handle Failures Gracefully

```csharp
public async Task SendEmailAsync(string email, string subject, string body)
{
    try
    {
        await _emailClient.SendAsync(email, subject, body);
    }
    catch (SmtpException ex)
    {
        // Log error - Hangfire will retry automatically
        _logger.LogError(ex, "Failed to send email to {Email}", email);
        throw; // Rethrow to trigger Hangfire retry
    }
}
```

### 3. Use Queues for Prioritization

```csharp
// Critical jobs (high priority)
BackgroundJob.Enqueue<ICriticalService>(x => x.ProcessCriticalTask(), "critical");

// Normal jobs
BackgroundJob.Enqueue<IEmailService>(x => x.SendEmail(), "default");

// Low priority jobs
BackgroundJob.Enqueue<IReportService>(x => x.GenerateReport(), "reports");
```

**Configure queues in order of priority:**
```json
{
  "Worker": {
    "Queues": ["critical", "default", "emails", "reports"]
  }
}
```

### 4. Avoid Long-Running Jobs

Break long tasks into smaller jobs:

```csharp
// ❌ Bad: Long-running job
public async Task ProcessAllOrdersAsync()
{
    var orders = await GetAllOrdersAsync(); // Could be millions
    foreach (var order in orders)
    {
        await ProcessOrderAsync(order); // Hours of processing
    }
}

// ✅ Good: Batch processing
public async Task ProcessOrderBatchAsync(int page, int pageSize)
{
    var orders = await GetOrderPageAsync(page, pageSize);
    foreach (var order in orders)
    {
        await ProcessOrderAsync(order);
    }
    
    // Enqueue next batch
    if (orders.Count == pageSize)
    {
        _jobService.Enqueue<IOrderService>(x => x.ProcessOrderBatchAsync(page + 1, pageSize));
    }
}
```

### 5. Use Idempotent Jobs

Jobs may execute multiple times due to retries:

```csharp
// ✅ Idempotent: Safe to run multiple times
public async Task SendWelcomeEmailAsync(Guid userId)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null || user.WelcomeEmailSent)
    {
        return; // Already sent or user not found
    }
    
    await _emailService.SendAsync(user.Email, "Welcome!");
    
    user.WelcomeEmailSent = true;
    await _userRepository.UpdateAsync(user);
}
```

## Common Use Cases by Microservice

### Auth API
- Email verification after registration
- Password reset token expiration
- Session cleanup (expired tokens)
- User account deactivation reminders

### MMO API
- Coupon expiration checks (daily)
- Partner product sync (hourly/daily)
- Affiliate link click tracking aggregation
- Cache warming for popular products

### Storage API
- Temporary file cleanup (daily)
- Thumbnail generation for uploaded images
- Orphaned file detection and deletion
- Storage usage reporting (monthly)

### Email API
- Email queue processing (fire-and-forget)
- Newsletter sending (batched)
- Bounce/complaint handling
- Email template rendering

### Analytics API
- Hourly/daily/monthly data aggregation
- Traffic report generation
- Data warehouse ETL jobs
- Metric calculation and caching

### MasterData API
- Reference data synchronization
- Data import/export jobs
- Data validation and cleanup
- Master data snapshot creation

## Troubleshooting

### Jobs Not Executing

**Check:**
1. Hangfire server is running (`builder.Services.AddHangfireServer()`)
2. Database connection is valid (SQL Server storage)
3. Worker count > 0 (`WorkerCount: 20`)
4. Job is enqueued (check dashboard)

**Debug:**
```csharp
// Enable verbose logging
services.AddHangfire(config => {
    config.UseColouredConsoleLogProvider();
});
```

### Dashboard Not Loading

**Check:**
1. Dashboard enabled (`Dashboard.Enabled: true`)
2. User authenticated (if `RequireAuthentication: true`)
3. User has required role (`RequiredRole: "Admin"`)
4. Correct route (`/hangfire`)

**Test:**
```csharp
// Temporarily disable auth for testing
"Dashboard": {
  "RequireAuthentication": false
}
```

### High Memory Usage

**Solutions:**
1. Reduce worker count (`WorkerCount: 10`)
2. Increase polling interval (`PollingIntervalSeconds: 30`)
3. Use SQL Server storage instead of InMemory
4. Delete old job history from database

### Failed Jobs

**Check Dashboard:**
- Failed Jobs tab shows exceptions
- Retry manually or delete
- Check logs for error details

**Automatic Retries:**
Jobs retry automatically with exponential backoff (default: 10 attempts)

## Dependencies

- **Hangfire.Core** (1.8.18)
- **Hangfire.SqlServer** (1.8.18)
- **Hangfire.AspNetCore** (1.8.18)
- **Microsoft.Extensions.Configuration.Binder** (10.0.0)
- **Microsoft.Extensions.Options.ConfigurationExtensions** (10.0.0)

## Migration Guide

### From Manual Hangfire Setup

**Before:**
```csharp
builder.Services.AddHangfire(config => {
    config.UseSqlServerStorage(connectionString);
});
builder.Services.AddHangfireServer();
app.UseHangfireDashboard("/hangfire");
```

**After:**
```csharp
builder.Services.AddHangfireServices(builder.Configuration);
app.UseHangfireDashboardWithAuth(app.Configuration);
```

### From Other Background Job Libraries

Replace direct Hangfire calls with `IJobService`:

**Before:**
```csharp
BackgroundJob.Enqueue(() => SendEmail());
RecurringJob.AddOrUpdate("daily-job", () => DailyTask(), Cron.Daily());
```

**After:**
```csharp
_jobService.Enqueue<IEmailService>(x => x.SendEmail());
_jobService.AddOrUpdateRecurringJob<ITaskService>("daily-job", x => x.DailyTask(), Cron.Daily());
```

## License

MIT License - Same as MMO project

## Support

- Issues: Report in main MMO project repository
- Documentation: See `Core/MagicTree.Framework.Hangfire/README.md`
- Examples: Check Auth API, MMO API implementations
