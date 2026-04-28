namespace LessonsHub.Application.Models.Requests;

/// <summary>
/// Marks an AI request that may opt out of the documentation cache.
/// Only honoured for Technical lessons; ignored otherwise.
/// </summary>
public interface IAiTechnicalRequest
{
    bool BypassDocCache { get; set; }
}
