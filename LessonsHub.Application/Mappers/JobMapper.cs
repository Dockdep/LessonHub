using LessonsHub.Application.Models.Jobs;
using LessonsHub.Domain.Entities;

namespace LessonsHub.Application.Mappers;

public static class JobMapper
{
    public static JobDto ToDto(Job j) => new(
        j.Id,
        j.Type,
        j.Status,
        j.ResultJson,
        j.Error,
        j.RelatedEntityType,
        j.RelatedEntityId,
        j.CreatedAt,
        j.StartedAt,
        j.CompletedAt);

    public static JobEvent ToEvent(Job j) => new(
        j.Id,
        j.Type,
        j.Status,
        j.ResultJson,
        j.Error,
        j.RelatedEntityType,
        j.RelatedEntityId,
        DateTime.UtcNow);
}
