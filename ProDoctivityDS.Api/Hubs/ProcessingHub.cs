using Microsoft.AspNetCore.SignalR;

namespace ProDoctivityDS.Api.Hubs
{
    public class ProcessingHub : Hub
    {
        /// <summary>
        /// El cliente llama a este método para unirse a un grupo identificado por sessionId.
        /// Así recibirá mensajes solo de su proceso.
        /// </summary>
        public async Task JoinGroup(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        /// <summary>
        /// Opcional: salir del grupo.
        /// </summary>
        public async Task LeaveGroup(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }

        // Podemos agregar métodos que el cliente pueda invocar si es necesario
        // Por ahora no necesitamos nada más.
    }
}