using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Services;
using LessonsHub.Infrastructure.Auth;
using LessonsHub.Infrastructure.Data;
using LessonsHub.Infrastructure.Realtime;
using LessonsHub.Infrastructure.Repositories;

namespace LessonsHub.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILessonPlanRepository, LessonPlanRepository>();
        services.AddScoped<ILessonPlanShareRepository, LessonPlanShareRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILessonDayRepository, LessonDayRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IExerciseAnswerRepository, ExerciseAnswerRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<ILessonPlanShareService, LessonPlanShareService>();
        services.AddScoped<ILessonDayService, LessonDayService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ILessonPlanService, LessonPlanService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IJobService, JobService>();
        return services;
    }

    /// <summary>
    /// SignalR + in-memory job queue + background worker. Single-instance only:
    /// the queue is in-process, so Cloud Run must run with --max-instances=1
    /// until we add a Redis backplane.
    /// </summary>
    public static IServiceCollection AddJobInfrastructure(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IJobQueue, ChannelJobQueue>();
        // Registry is Scoped because executors are Scoped (they pull DbContext +
        // repositories + per-user services). The background service creates a
        // fresh DI scope per job and resolves the registry from there.
        services.AddScoped<IJobExecutorRegistry, JobExecutorRegistry>();
        services.AddHostedService<JobBackgroundService>();

        // TEMP — Phase-0 sanity test executor. Remove once all real executors are landed.
        services.AddScoped<IJobExecutor, LessonsHub.Application.Services.Executors.EchoTestExecutor>();

        // Real executors land here as Phase 3 progresses.
        services.AddScoped<IJobExecutor, LessonsHub.Application.Services.Executors.LessonPlanGenerateExecutor>();

        return services;
    }
}
