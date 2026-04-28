namespace LessonsHub.Application.Interfaces;

/// <summary>
/// Persists uploaded document files. Two strategies (Local + GCS) plug into
/// this interface; the controller stays unaware of which one is in use.
///
/// The returned URI is stored on the Document row and later passed to the
/// Python RAG service so it can read the source content for chunking.
/// </summary>
public interface IDocumentStorage
{
    /// <summary>
    /// Save a file's contents and return a URI that uniquely identifies it
    /// (and that <see cref="Delete"/> understands).
    /// </summary>
    Task<string> SaveAsync(
        int userId,
        int documentId,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Remove the file. No-op (returns false) if the URI doesn't resolve.</summary>
    Task<bool> DeleteAsync(string storageUri, CancellationToken cancellationToken = default);
}
