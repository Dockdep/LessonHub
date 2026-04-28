using LessonsHub.Application.Interfaces;
using LessonsHub.Application.Services;
using LessonsHub.Controllers;
using LessonsHub.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace LessonsHub.Tests.TestSupport;

/// <summary>
/// Wires the full controller -> service -> repository -> DbContext stack on top of a TestDb,
/// so test cases keep exercising real EF behaviour without each test rebuilding the graph.
/// </summary>
public static class TestStack
{
    public static LessonPlanShareController ShareController(TestDb db, int actingAs) =>
        new(BuildShareService(db, actingAs));

    public static UserProfileController UserProfileController(TestDb db, int actingAs) =>
        new(BuildUserProfileService(db, actingAs));

    public static LessonDayController LessonDayController(TestDb db, int actingAs) =>
        new(BuildLessonDayService(db, actingAs));

    public static LessonPlanController LessonPlanController(TestDb db, int actingAs, ILessonsAiApiClient ai) =>
        new(BuildLessonPlanService(db, actingAs, ai), new TestJobService());

    public static LessonController LessonController(TestDb db, int actingAs, ILessonsAiApiClient ai) =>
        new(
            BuildLessonService(db, actingAs, ai),
            BuildExerciseService(db, actingAs, ai));

    private static LessonPlanShareService BuildShareService(TestDb db, int actingAs) =>
        new(
            new LessonPlanShareRepository(db.Context),
            new LessonPlanRepository(db.Context),
            new UserRepository(db.Context),
            new TestCurrentUser(actingAs),
            NullLogger<LessonPlanShareService>.Instance);

    private static UserProfileService BuildUserProfileService(TestDb db, int actingAs) =>
        new(
            new UserRepository(db.Context),
            new TestCurrentUser(actingAs),
            NullLogger<UserProfileService>.Instance);

    private static LessonDayService BuildLessonDayService(TestDb db, int actingAs) =>
        new(
            new LessonPlanRepository(db.Context),
            new LessonRepository(db.Context),
            new LessonDayRepository(db.Context),
            new TestCurrentUser(actingAs),
            NullLogger<LessonDayService>.Instance);

    private static LessonPlanService BuildLessonPlanService(TestDb db, int actingAs, ILessonsAiApiClient ai) =>
        new(
            new LessonPlanRepository(db.Context),
            new LessonRepository(db.Context),
            new LessonDayRepository(db.Context),
            ai,
            new TestCurrentUser(actingAs),
            NullLogger<LessonPlanService>.Instance);

    private static LessonService BuildLessonService(TestDb db, int actingAs, ILessonsAiApiClient ai) =>
        new(
            new LessonRepository(db.Context),
            new LessonPlanRepository(db.Context),
            ai,
            new TestCurrentUser(actingAs),
            NullLogger<LessonService>.Instance);

    private static ExerciseService BuildExerciseService(TestDb db, int actingAs, ILessonsAiApiClient ai) =>
        new(
            new LessonRepository(db.Context),
            new LessonPlanRepository(db.Context),
            new ExerciseRepository(db.Context),
            new ExerciseAnswerRepository(db.Context),
            ai,
            new TestCurrentUser(actingAs),
            NullLogger<ExerciseService>.Instance);
}
