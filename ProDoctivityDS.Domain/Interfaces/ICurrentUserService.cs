namespace ProDoctivityDS.Domain.Interfaces
{
    /// <summary>
    /// Servicio para rastrear qué usuario está asociado a cada sesión.
    /// Permite aislar la configuración por usuario.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Obtiene el username del usuario actual (desde HTTP o override de background task).
        /// </summary>
        string? GetCurrentUsername();

        /// <summary>
        /// Asocia un sessionId a un username (se llama al hacer login).
        /// </summary>
        void SetUsername(string sessionId, string username);

        /// <summary>
        /// Elimina la asociación de un sessionId (se llama al hacer logout).
        /// </summary>
        void RemoveSession(string sessionId);

        /// <summary>
        /// Establece un username override para tareas en background (sin HttpContext).
        /// Retorna un IDisposable que limpia el override al hacer Dispose.
        /// </summary>
        IDisposable OverrideUsername(string? username);
    }
}
