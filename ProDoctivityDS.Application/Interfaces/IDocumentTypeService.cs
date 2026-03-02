using ProDoctivityDS.Application.Dtos.ProDoctivity;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IDocumentTypeService
    {
        Task<List<DocumentTypeDto>> GetAllDocumentTypes(CancellationToken cancellationToken = default);
    }
}
