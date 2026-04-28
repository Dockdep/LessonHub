namespace LessonsHub.Application.Models.Jobs;

/// <summary>
/// String constants used as the discriminator on the Job table. Strings (not
/// an enum) so the column survives executor renames and stays readable in DB.
/// </summary>
public static class JobType
{
    public const string LessonPlanGenerate = "LessonPlanGenerate";
    public const string LessonContentGenerate = "LessonContentGenerate";
    public const string LessonContentRegenerate = "LessonContentRegenerate";
    public const string ExerciseGenerate = "ExerciseGenerate";
    public const string ExerciseRetry = "ExerciseRetry";
    public const string ExerciseReview = "ExerciseReview";
    public const string DocumentIngest = "DocumentIngest";
    public const string LessonResources = "LessonResources";
}
