namespace LessonsHub.Domain.Entities;

public class LessonPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NativeLanguage { get; set; }

    /// <summary>
    /// Language lessons only — the target language the user is studying.
    /// Null for Default/Technical plans.
    /// </summary>
    public string? LanguageToLearn { get; set; }

    /// <summary>
    /// Language lessons only — when true, the lesson output is rendered in
    /// <see cref="NativeLanguage"/>; when false, in <see cref="LanguageToLearn"/>
    /// (immersive mode). Ignored for Default/Technical plans.
    /// </summary>
    public bool UseNativeLanguage { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// When set, this plan was generated from a user-uploaded document.
    /// All downstream content/exercise generation passes this through to the
    /// Python service so it can RAG-ground each lesson against the same source.
    /// </summary>
    public int? DocumentId { get; set; }
    public Document? Document { get; set; }

    public List<Lesson> Lessons { get; set; } = new();
    public List<LessonPlanShare> Shares { get; set; } = new();
}
