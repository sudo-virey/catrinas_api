using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace CatrinasAPI.Hubs;

[Authorize] // Requiere autenticación JWT
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
        // Extraer información del JWT
        var userName = Context.User?.Identity?.Name ?? "Usuario Anónimo";
        var accessType = Context.User?.FindFirst("AccessType")?.Value ?? "No especificado";
        var idUsuario = Context.User?.FindFirst("idUsuario")?.Value ?? "No especificado";
        var idAcceso = Context.User?.FindFirst("IdAcceso")?.Value ?? "No especificado";
        var fullName = Context.User?.FindFirst("FullName")?.Value ?? userName;
        
        // Personalizar mensaje según AccessType
        string mensajeBienvenida;
        string iconoAcceso;
        
        if (accessType == "Admin")
        {
            iconoAcceso = "👑";
            mensajeBienvenida = $"¡Bienvenido al chat, {fullName}! Tienes acceso de ADMINISTRADOR.";
        }
        else if (accessType == "Votacion")
        {
            iconoAcceso = "🗳️";
            mensajeBienvenida = $"¡Bienvenido al chat, {fullName}! Tienes acceso para VOTACIÓN.";
        }
        else
        {
            iconoAcceso = "👤";
            mensajeBienvenida = $"¡Bienvenido al chat, {fullName}! Acceso: {accessType}.";
        }
        
        // Mensaje de bienvenida personalizado
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"{iconoAcceso} {mensajeBienvenida}");
        
        // Información detallada del token
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"📋 Información de sesión - Tipo: {accessType} | ID Usuario: {idUsuario} | ID Acceso: {idAcceso}");
        
        // Información técnica de conexión
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"🔗 ID de conexión: {Context.ConnectionId.Substring(0, 8)}... | 🕐 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        // Notificar a todos los demás usuarios que alguien se conectó (con tipo de acceso)
        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            $"{iconoAcceso} {fullName} ({accessType}) se ha unido al chat");
        
        // Evento específico para manejo en el frontend (opcional)
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    // Notificar cuando alguien se desconecta
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "Usuario Anónimo";
        var accessType = Context.User?.FindFirst("AccessType")?.Value ?? "No especificado";
        var fullName = Context.User?.FindFirst("FullName")?.Value ?? userName;
        
        // Icono según tipo de acceso
        string iconoAcceso = accessType switch
        {
            "Admin" => "👑",
            "Votacion" => "🗳️",
            _ => "👤"
        };
        
        // Mensaje a todos los usuarios sobre la desconexión
        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            $"👋 {iconoAcceso} {fullName} ({accessType}) ha abandonado el chat");
        
        // Evento específico para manejo en el frontend
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }
}