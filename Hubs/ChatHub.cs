using Microsoft.AspNetCore.SignalR;

namespace CatrinasAPI.Hubs;

public class ChatHub : Hub
{
    // Método para enviar mensaje a todos los conectados
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    // Notificar cuando alguien se conecta
    public override async Task OnConnectedAsync()
    {
        // Mensaje de bienvenida personal al usuario que se conectó
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"¡Bienvenido al chat! Tu ID de conexión es: {Context.ConnectionId}");
        
        // Mensaje de bienvenida con información del servidor
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"Conexión establecida exitosamente. Servidor: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        // Notificar a todos los demás usuarios que alguien se conectó
        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            "Un nuevo usuario se ha unido al chat");
        
        // Evento específico para manejo en el frontend (opcional)
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    // Notificar cuando alguien se desconecta
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Mensaje a todos los usuarios sobre la desconexión
        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            "Un usuario ha abandonado el chat");
        
        // Evento específico para manejo en el frontend
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }
}