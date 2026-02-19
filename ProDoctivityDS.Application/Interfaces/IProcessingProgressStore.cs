using ProDoctivityDS.Application.Dtos.Response;

namespace ProDoctivityDS.Application.Interfaces
{
    public interface IProcessingProgressStore
    {
        void UpdateProgress(string sessionId, ProcessProgressDto progress);

        ProcessProgressDto? GetProgress(string sessionId);

        void RemoveProgress(string sessionId);
    }
}
