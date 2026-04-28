using LessonsHub.Application.Models.Requests;
using LessonsHub.Application.Models.Responses;

namespace LessonsHub.Application.Abstractions.Services;

public interface IDocumentService
{
    Task<ServiceResult<List<DocumentDto>>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<DocumentDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ServiceResult<DocumentDto>> UploadAsync(UploadDocumentInput input, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken ct = default);
}
