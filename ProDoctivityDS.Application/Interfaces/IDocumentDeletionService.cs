namespace ProDoctivityDS.Application.Interfaces
{
    public interface IDocumentDeletionService
    {
        Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    }
}
