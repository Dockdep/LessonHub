using LessonsHub.Application.Abstractions;
using LessonsHub.Application.Abstractions.Repositories;
using LessonsHub.Application.Abstractions.Services;
using LessonsHub.Application.Services;
using LessonsHub.Infrastructure.Auth;
using LessonsHub.Infrastructure.Data;
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
        return services;
    }
}
