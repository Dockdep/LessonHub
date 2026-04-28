namespace LessonsHub.Application.Models.Responses;

public class LessonDayDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public List<AssignedLessonDto> Lessons { get; set; } = new();
}

public class AssignedLessonDto
{
    public int Id { get; set; }
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int LessonPlanId { get; set; }
    public string LessonPlanName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class LessonPlanSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int LessonsCount { get; set; }
    public bool IsOwner { get; set; }
    public string? OwnerName { get; set; }
}

public class AvailableLessonDto
{
    public int Id { get; set; }
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public int LessonPlanId { get; set; }
    public string LessonPlanName { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
}

public class LessonPlanDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NativeLanguage { get; set; }

    /// <summary>Language lessons only — target language being studied.</summary>
    public string? LanguageToLearn { get; set; }

    /// <summary>Language lessons only — when true, render in native; when false, immerse in target.</summary>
    public bool UseNativeLanguage { get; set; } = true;

    public DateTime CreatedDate { get; set; }
    public List<PlanLessonDto> Lessons { get; set; } = new();
    public bool IsOwner { get; set; }
    public string? OwnerName { get; set; }
}

public class LessonPlanShareDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
}

public class AddShareRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class PlanLessonDto
{
    public int Id { get; set; }
    public int LessonNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LessonTopic { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
