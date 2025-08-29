using Coravel;
using FlyingKit.Core.Abstractions;
using FlyingKit.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FlyingKit.Core.Extensions;

public static class CoreExtensions
{
    public static void AddFlyingKitCore(this IServiceCollection services)
    {
        
        // Register our Hangfire-like services
        services.AddSingleton<RedisJobStorage>();
        services.AddSingleton<JobScheduler>();

        services.AddSingleton<JobHandler>();
        services.AddSingleton<JobScanner>();
        services.AddSingleton<JobHandlersStorage>();
        services.AddSingleton<JobProcessor>();

        var runningAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        services.Scan(s =>
            s.FromAssemblies(runningAssemblies)
                .AddClasses(c => c.AssignableTo(typeof(ITickJob)))
                .AsSelf()
                .WithTransientLifetime());

        services.AddScheduler();
    }

    public static void UseFlyingKit(this WebApplication app,int pollingIntervalInSeconds = 30)
    {
        //want to scan all implementations of ITickJob and register them to the jobsHandlerStorage service
        var jobHandlersStorage = app.Services.GetRequiredService<JobHandlersStorage>();

        var runningAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var runningAssembly in runningAssemblies)
        {
            foreach (var type in runningAssembly.GetTypes().Where(t => t.IsAssignableTo(typeof(ITickJob)) && !t.IsAbstract))
            {
                jobHandlersStorage.Add(type.ToString(), app.Services.GetRequiredService(type) as ITickJob);
            }
        }
        
        app.Services.UseScheduler(s =>
        {
            s.Schedule<JobScanner>()
                .EverySeconds(pollingIntervalInSeconds)
                .RunOnceAtStart()
                .PreventOverlapping("FLYING_KIT_JOB_SCANNER");
        });
    }
}