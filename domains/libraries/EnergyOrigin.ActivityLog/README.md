# Activity Log

Activity log is built around logging activities in the different sub modules of EneryOrigin. This will enable us to have a similar approach and simple aligned API across the api's. The nuget package consists of

* API Endpoint /api/{ServiceName}/activity-log
* Extension methods for adding and using activity log (Adding dependencies and wiring up middleware)
* Database entity and repository for getting / creating activity logs.
* HostedServices / Background service for cleaning up Activity Logs again.

## Quick Start

1. Register DbContext as part of normal EF Core setup in Program.cs

```csharp
builder.Services.AddDbContext<DbContext, ApplicationDbContext>
{
    // Normal setup
}
```

2. Add ActivityLog in Program.cs
```csharp
builder.Services.AddActivityLog();
// Or
services.AddActivityLog(options =>
{
    options.CleanupIntervalInMinutes = 15; // Default
    options.CleanupActivityLogsOlderThanInDays = 60; // Default
    options.ServiceName = "YourServiceName" // No defaults
});
// Or simply inject ActivityLogOptions
```

3. Use Activity log in Program.cs

```csharp
app.MapControllers();
// After map controllers
builder.Services.AddActivityLog();
```

4. Add DbSet and modelBuilder in ApplicationDbContext.cs
```csharp
modelBuilder.AddActivityLogEntry();
....

public DbSet<ActivityLogEntry> ActivityLogs { get; set; }
```

5. Update and run Db migrations to get new tables in the database. (Read DB migrations docs if in dought)

6. Now start storing Activity logs in your sub module.

```csharp
IActivityLogEntryRepository activityLogEntryRepository

var activityLog = ActivityLogEntry.Create(...);
activityLogEntryRepository.AddActivityLogEntryAsync(activityLog);
```
