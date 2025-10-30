using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Security.Claims;
using CatrinasAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using CatrinasAPI.Models;

namespace CatrinasAPI.Hubs;

// Clase para manejar votaciones en curso
public class VotacionEnCurso
{
    public int IdParticipante { get; set; }
    public string Participante { get; set; } = "";
    public long TiempoInicio { get; set; }
    public int TiempoDuracion { get; set; }
}

[Authorize] // Requiere autenticación JWT
public class BasicHub : Hub
{

    private readonly CatrinasDbContext _context;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<BasicHub> _logger;

    private static readonly Dictionary<int, VotacionEnCurso> _votacionesEnCurso = new();
    private static readonly Dictionary<string, List<string>> _userConnections = new();

    // Configuración JSON para preservar acentos
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };


    public BasicHub(CatrinasDbContext context, IServiceScopeFactory serviceScopeFactory, IHubContext<ChatHub> hubContext, ILogger<BasicHub> logger)
    {
        _context = context;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Método genérico para recibir cualquier mensaje JSON
    /// </summary>
    public async Task SendMessage(string jsonMessage)
    {
        try
        {
            // Validar que el usuario esté autenticado
            if (!Context.User?.Identity?.IsAuthenticated ?? true)
            {
                var authError = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    error = "unauthorized",
                    message = "User not authenticated"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(authError, JsonOptions));
                return;
            }

            // Parsear el JSON para validar que sea válido
            var messageObject = JsonSerializer.Deserialize<JsonElement>(jsonMessage);

            // Obtener información del usuario
            var userName = Context.User?.Identity?.Name ?? "Usuario";
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogInformation($"[BASIC HUB] Mensaje recibido de {userName}: {jsonMessage}");

            // Verificar si el mensaje tiene un campo "command" o "comando" para procesar comandos específicos
            if (messageObject.TryGetProperty("command", out var commandElement) ||
                messageObject.TryGetProperty("comando", out commandElement))
            {
                var command = commandElement.GetString();
                await ProcessCommand(command, messageObject, userName, userRole);
                return;
            }

            // Si no es un comando, procesar como mensaje genérico
            var response = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                message = "Message not recognized"
            };

            // Enviar respuesta solo al cliente que envió el mensaje
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (JsonException ex)
        {
            // Error si el JSON no es válido
            _logger.LogWarning($"[BASIC HUB] JSON inválido de {Context.User?.Identity?.Name}: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                message = ex.Message
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
        catch (Exception ex)
        {
            // Error general
            _logger.LogError($"[BASIC HUB] Error general en SendMessage: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                message = ex.Message
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    /// <summary>
    /// Procesa comandos específicos enviados a través de JSON
    /// </summary>
    private async Task ProcessCommand(string? command, JsonElement messageObject, string userName, string? userRole)
    {
        try
        {
            switch (command?.ToLower())
            {
                case "get_dashboard":
                case "get_dashboard_data":
                    await SendRoleBasedResponse();
                    break;

                case "get_participants":
                case "get_participantes":
                    await ProcessGetParticipants();
                    break;

                case "get_ranking":
                    await ProcessGetRanking();
                    break;

                case "get_voting_status":
                case "get_estado_votacion":
                    await ProcessGetVotingStatus();
                    break;

                case "vote":
                case "votar":
                    await ProcessVote(messageObject);
                    break;

                case "help":
                case "ayuda":
                    await SendHelpMessage();
                    break;

                // Nuevos comandos con estructura específica
                case "ajustes":
                    await ProcessAjustes(messageObject);
                    break;

                case "iniciar_votacion":
                    await ProcessParticipanteCommand(messageObject);
                    break;

                case "desempatar_votacion":
                    await DesempatarVotacionCommand(messageObject);
                    break;

                default:
                    var unknownResponse = new
                    {
                        type = "unknown_command",
                        message = $"Comando desconocido: '{command}'. Envía {{'command': 'help'}} para ver comandos disponibles.",
                        receivedCommand = command
                    };
                    await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(unknownResponse, JsonOptions));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error procesando comando '{command}': {ex.Message}");
            var errorResponse = new
            {
                type = "command_error",
                timestamp = DateTime.UtcNow,
                error = "command_processing_error",
                command = command,
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    /// <summary>
    /// Método para obtener información del usuario conectado (diferenciada por rol)
    /// </summary>
    public async Task GetUserInfo()
    {
        try
        {
            // Validar que el usuario esté autenticado
            if (!Context.User?.Identity?.IsAuthenticated ?? true)
            {
                var authError = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    error = "unauthorized",
                    message = "User not authenticated"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(authError, JsonOptions));
                return;
            }

            var claims = Context.User?.Claims?.ToDictionary(c => c.Type, c => c.Value) ?? new Dictionary<string, string>();

            _logger.LogInformation($"[BASIC HUB] Info de usuario solicitada: {Context.User?.Identity?.Name}");

            // Enviar respuesta diferenciada por rol con información del usuario
            await SendRoleBasedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener info de usuario: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "user_info_error",
                message = ex.Message
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    /// <summary>
    /// Método para obtener datos del dashboard (respuesta según rol)
    /// </summary>
    public async Task GetDashboardData()
    {
        try
        {
            // Validar que el usuario esté autenticado
            if (!Context.User?.Identity?.IsAuthenticated ?? true)
            {
                var authError = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    error = "unauthorized",
                    message = "User not authenticated"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(authError, JsonOptions));
                return;
            }

            _logger.LogInformation($"[BASIC HUB] Datos de dashboard solicitados por: {Context.User?.Identity?.Name}");

            // Enviar respuesta diferenciada por rol
            await SendRoleBasedResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener datos de dashboard: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "dashboard_error",
                message = ex.Message
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userName = Context.User?.Identity?.Name ?? "Usuario";
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false;

            // Desconectar conexiones anteriores del mismo usuario
            if (_userConnections.ContainsKey(userName))
            {
                var oldConnections = _userConnections[userName].ToList();
                foreach (var oldConnectionId in oldConnections)
                {
                    if (oldConnectionId != Context.ConnectionId)
                    {
                        try
                        {
                            await Clients.Client(oldConnectionId).SendAsync("ServerResponse", JsonSerializer.Serialize(new
                            {
                                type = "connection_replaced",
                                timestamp = DateTime.UtcNow,
                                message = "Nueva conexión establecida desde otro lugar"
                            }, JsonOptions));

                            _logger.LogInformation($"[BASIC HUB] Desconectando conexión anterior: {oldConnectionId} para usuario {userName}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[BASIC HUB] Error al notificar conexión anterior: {ex.Message}");
                        }
                    }
                }
                _userConnections[userName].Clear();
            }
            else
            {
                _userConnections[userName] = new List<string>();
            }

            // Agregar la nueva conexión
            _userConnections[userName].Add(Context.ConnectionId);

            // Agregar a grupos según el rol
            if (userRole == "Administrador")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Administradores");
            }
            else if (userRole == "votante" || userRole == "Publico")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Votantes");
            }

            _logger.LogInformation($"[BASIC HUB] Usuario conectado: {userName} (ID: {Context.ConnectionId}) - Rol: {userRole} - Autenticado: {isAuthenticated}");

            // Enviar respuesta diferenciada por rol
            await SendRoleBasedResponse();
            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error en conexión: {ex.Message}");
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userName = Context.User?.Identity?.Name ?? "Usuario";

            // Remover la conexión del diccionario
            if (_userConnections.ContainsKey(userName))
            {
                _userConnections[userName].Remove(Context.ConnectionId);
                if (_userConnections[userName].Count == 0)
                {
                    _userConnections.Remove(userName);
                }
            }

            _logger.LogInformation($"[BASIC HUB] Usuario desconectado: {userName} (ID: {Context.ConnectionId})");

            if (exception != null)
            {
                _logger.LogError($"[BASIC HUB] Error en desconexión: {exception.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al procesar desconexión: {ex.Message}");
            await base.OnDisconnectedAsync(exception);
        }
    }


    // ===== MÉTODOS PRIVADOS PARA OBTENER DATOS =====

    private async Task<object> GetAdminDashboardData()
    {
        try
        {
            // Obtener votación en curso desde el diccionario en memoria
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Espera (votación activa)
                .FirstOrDefaultAsync();

            object? votacionEnCurso = null;
            if (participanteEnVotacion != null && _votacionesEnCurso.ContainsKey(participanteEnVotacion.Id_Participante))
            {
                var datosVotacion = _votacionesEnCurso[participanteEnVotacion.Id_Participante];
                votacionEnCurso = new
                {
                    idParticipante = datosVotacion.IdParticipante,
                    participante = datosVotacion.Participante,
                    tiempoInicio = datosVotacion.TiempoInicio,
                    tiempoDuracion = datosVotacion.TiempoDuracion
                };
            }

            // Obtener todos los participantes
            var participantes = await _context.Participantes
                .Include(p => p.Estado)
                .OrderBy(p => p.Orden)
                .Select(p => new
                {
                    idParticipante = p.Id_Participante,
                    participante = p.Nombre,
                    estado = p.Id_Estado,
                    orden = p.Orden
                })
                .ToListAsync();

            // Obtener ranking completo
            var ranking = await GetAdminRanking();

            // Obtener ajustes
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            return new
            {
                votacionEnCurso = votacionEnCurso,
                participantes = participantes,
                ranking = ranking,
                ajustes = new
                {
                    tiempoVotacion = ultimoAjuste?.Tiempo_de_Votacion ?? 300,
                    terminado = ultimoAjuste?.Publicacion_Resultados ?? false
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener datos de dashboard admin: {ex.Message}");
            return new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "dashboard_data_error",
                message = ex.Message
            };
        }
    }

    private async Task<object> GetAdminRanking()
    {
        try
        {
            var ranking = await _context.Rankings.Select(p => new
            {
                IdParticipante = p.Id_Participante,
                Participante = p.Participante.Nombre,
                Puntaje = p.Puntos
            }).OrderByDescending(p => p.Puntaje)
                .ToListAsync();

            // Agregar orden de ranking después de obtener los datos
            return ranking.Select((item, index) => new
            {
                OrdenRanking = index + 1,
                IdParticipante = item.IdParticipante,
                Participante = item.Participante,
                Puntaje = item.Puntaje,
                detallePuntaje = new
                {
                    atuendo = 0,
                    maquillaje = 0,
                    tradiciones = 0,
                    pasarela = 0,
                    interaccion = 0
                }
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener ranking: {ex.Message}");
            return new List<object>();
        }
    }

    private async Task<object> GetVotanteData()
    {
        try
        {

            // Verificar si el evento está marcado como terminado
            var ultimoAjuste = await _context.Ajustes
                .Where(a => a.Activo == true)
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            if (ultimoAjuste?.Publicacion_Resultados ?? false)
            {
                return new
                {
                    type = "error",
                    concursoTerminado = true,
                    votacionEnCurso = false,
                    mensaje = "El concurso ha finalizado. Ya no es posible votar."
                };
            }

            // Obtener participante en votación actual
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Espera (votación activa)
                .FirstOrDefaultAsync();

            if (participanteEnVotacion == null)
            {
                return new
                {
                    type = "success",
                    timestamp = DateTime.UtcNow,
                    concursoTerminado = false,
                    votacionEnCurso = false,
                    mensaje = "No hay ninguna votación activa en este momento."
                };
            }

            return new
            {
                type = "votante_data",
                timestamp = DateTime.UtcNow,
                concursoTerminado = false,
                votacionEnCurso = true,
                detallesVotacionEnCurso = new
                {
                    idParticipante = participanteEnVotacion.Id_Participante,
                    participante = participanteEnVotacion.Nombre,
                    mensaje = $"Puedes votar por {participanteEnVotacion.Nombre}",
                    tiempoInicio = _votacionesEnCurso[participanteEnVotacion.Id_Participante].TiempoInicio,
                    tiempoDuracion = _votacionesEnCurso[participanteEnVotacion.Id_Participante].TiempoDuracion,
                    tiempoTranscurrido = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _votacionesEnCurso[participanteEnVotacion.Id_Participante].TiempoInicio,
                    tiempoRestante = Math.Max(0, _votacionesEnCurso[participanteEnVotacion.Id_Participante].TiempoDuracion - (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _votacionesEnCurso[participanteEnVotacion.Id_Participante].TiempoInicio))
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener datos de votante: {ex.Message}");
            return new
            {
                type = "error",
                message = ex.Message
            };
        }
    }

    /// <summary>
    /// Envía respuesta diferenciada según el rol del usuario
    /// </summary>
    private async Task SendRoleBasedResponse()
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userName = Context.User?.Identity?.Name ?? "Usuario";

            object responseData;

            switch (userRole?.ToLower())
            {
                case "administrador":
                    var adminData = await GetAdminDashboardData();
                    await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(adminData, JsonOptions));
                    break;
                case "votante":
                case "publico":
                    var votanteData = await GetVotanteData();
                    await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(votanteData, JsonOptions));
                    break;
                default:
                    responseData = new
                    {
                        timestamp = DateTime.UtcNow,
                        message = "Acceso denegado o rol no reconocido:" + userRole?.ToLower(),
                    };
                    await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(responseData, JsonOptions));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error en SendRoleBasedResponse: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "role_response_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    // ===== MÉTODOS PARA PROCESAR COMANDOS JSON =====

    private async Task ProcessGetParticipants()
    {
        try
        {
            var participantes = await _context.Participantes
                .Include(p => p.Estado)
                .OrderBy(p => p.Orden)
                .Select(p => new
                {
                    idParticipante = p.Id_Participante,
                    participante = p.Nombre,
                    estado = p.Id_Estado,
                    orden = p.Orden
                })
                .ToListAsync();

            var response = new
            {
                type = "participants_list",
                timestamp = DateTime.UtcNow,
                data = participantes
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener participantes: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "participants_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    private async Task ProcessGetRanking()
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole?.ToLower() == "administrador")
            {
                var ranking = await GetAdminRanking();
                var response = new
                {
                    type = "admin_ranking",
                    timestamp = DateTime.UtcNow,
                    data = ranking
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
            }
            else
            {
                var response = new
                {
                    type = "access_denied",
                    timestamp = DateTime.UtcNow,
                    message = "Solo los administradores pueden ver el ranking completo."
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener ranking: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "ranking_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    private async Task ProcessGetVotingStatus()
    {
        try
        {
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Espera (votación activa)
                .FirstOrDefaultAsync();

            var response = new
            {
                type = "voting_status",
                timestamp = DateTime.UtcNow,
                data = new
                {
                    hayVotacionActiva = participanteEnVotacion != null,
                    participanteActual = participanteEnVotacion != null ? new
                    {
                        idParticipante = participanteEnVotacion.Id_Participante,
                        nombre = participanteEnVotacion.Nombre,
                        orden = participanteEnVotacion.Orden
                    } : null
                }
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al obtener estado de votación: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "voting_status_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    private async Task ProcessVote(JsonElement messageObject)
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var idAccesoStr = Context.User?.FindFirst("IdAcceso")?.Value;
            var accessCode = Context.User?.FindFirst("AccessCode")?.Value;

            // Validar rol
            if (userRole?.ToLower() != "publico")
            {
                var response = new
                {
                    type = "access_denied",
                    timestamp = DateTime.UtcNow,
                    message = "No cuenta con permisos para realizar esta acción."
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
                return;
            }

            // Validar que tenga ID de acceso válido
            if (string.IsNullOrEmpty(idAccesoStr) || !int.TryParse(idAccesoStr, out var idAcceso))
            {
                var response = new
                {
                    type = "access_denied",
                    timestamp = DateTime.UtcNow,
                    message = "Acceso no válido en el token."
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
                return;
            }

            // Extraer datos del JSON
            var idParticipante = messageObject.TryGetProperty("idParticipante", out var idElement) ? idElement.GetInt32() : 0;
            var atuendo = messageObject.TryGetProperty("atuendo", out var atuendoElement) ? atuendoElement.GetInt32() : 0;
            var maquillaje = messageObject.TryGetProperty("maquillaje", out var maquillajeElement) ? maquillajeElement.GetInt32() : 0;
            var tradiciones = messageObject.TryGetProperty("tradiciones", out var tradicionesElement) ? tradicionesElement.GetInt32() : 0;
            var pasarela = messageObject.TryGetProperty("pasarela", out var pasarelaElement) ? pasarelaElement.GetInt32() : 0;
            var interaccion = messageObject.TryGetProperty("interaccion", out var interaccionElement) ? interaccionElement.GetInt32() : 0;

            // Validaciones básicas
            if (idParticipante <= 0)
            {
                var errorResponse = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    message = "ID de participante inválido"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
                return;
            }

            // Validar que todas las puntuaciones estén entre 1 y 5
            if (atuendo < 1 || atuendo > 5 || maquillaje < 1 || maquillaje > 5 ||
                tradiciones < 1 || tradiciones > 5 || pasarela < 1 || pasarela > 5 ||
                interaccion < 1 || interaccion > 5)
            {
                var errorResponse = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    message = "Todas las puntuaciones deben estar entre 1 y 5"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
                return;
            }

            // Buscar participante en votación activa (estado 2)
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2 && p.Id_Participante == idParticipante)
                .FirstOrDefaultAsync();

            if (participanteEnVotacion == null)
            {
                var errorResponse = new
                {
                    type = "error",
                    timestamp = DateTime.UtcNow,
                    message = "No hay votación activa para este participante o el participante no existe"
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
                return;
            }

            // Calcular total usando la lógica del ChatHub
            var totalCriterios = atuendo + maquillaje + tradiciones + pasarela + interaccion;
            var totalPuntos = (decimal)totalCriterios * 10 / 25; // Conversión: 25 criterios = 10 puntos

            // Verificar si ya existe un registro para este Id_Participante y Id_Acceso
            var evaluacionExistente = await _context.Evaluaciones
                .FirstOrDefaultAsync(e => e.Id_Participante == idParticipante && e.Id_Acceso == idAcceso);

            if (evaluacionExistente != null)
            {
                // Si existe y está activo, no permitir votar de nuevo
                if (evaluacionExistente.Activo)
                {
                    var errorResponse = new
                    {
                        type = "error",
                        timestamp = DateTime.UtcNow,
                        message = "Ya has votado por este participante"
                    };
                    await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
                    return;
                }

                // Si existe pero está inactivo (Activo = 0), actualizar el registro
                evaluacionExistente.Atuendo = atuendo;
                evaluacionExistente.Maquillaje = maquillaje;
                evaluacionExistente.Tradiciones = tradiciones;
                evaluacionExistente.Pasarela = pasarela;
                evaluacionExistente.Interaccion = interaccion;
                evaluacionExistente.Total = Math.Round(totalPuntos, 2);
                evaluacionExistente.Activo = true;
                evaluacionExistente.FechaEvaluacion = DateTime.Now;

                // Actualizar el registro existente
                _context.Evaluaciones.Update(evaluacionExistente);
            }
            else
            {
                // Si no existe ningún registro, crear uno nuevo
                evaluacionExistente = new CatrinasAPI.Models.Evaluacion
                {
                    Id_Participante = idParticipante,
                    Id_Acceso = idAcceso,
                    Atuendo = atuendo,
                    Maquillaje = maquillaje,
                    Tradiciones = tradiciones,
                    Pasarela = pasarela,
                    Interaccion = interaccion,
                    Total = Math.Round(totalPuntos, 2),
                    Activo = true,
                    FechaEvaluacion = DateTime.Now
                };

                // Agregar el nuevo registro
                _context.Evaluaciones.Add(evaluacionExistente);
            }

            // Guardar cambios en base de datos
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[BASIC HUB] Voto guardado: Usuario {accessCode} votó por participante {participanteEnVotacion.Nombre} con total {totalPuntos:F2}");

            // Respuesta exitosa al votante
            var successResponse = new
            {
                type = "success",
                timestamp = DateTime.UtcNow,
                message = "Tu voto ha sido registrado exitosamente",
                data = new
                {
                    participante = participanteEnVotacion.Nombre,
                    idParticipante = idParticipante,
                    criterios = new
                    {
                        atuendo = atuendo,
                        maquillaje = maquillaje,
                        tradiciones = tradiciones,
                        pasarela = pasarela,
                        interaccion = interaccion,
                        totalCriterios = totalCriterios
                    },
                    puntosFinal = Math.Round(totalPuntos, 2),
                    fechaVoto = evaluacionExistente.FechaEvaluacion
                }
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(successResponse, JsonOptions));

        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al procesar voto: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "vote_processing_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    private async Task SendHelpMessage()
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var commands = new List<object>();

            // Comandos básicos para todos
            // commands.Add(new { command = "get_user_info", description = "Obtener información del usuario actual" });
            // commands.Add(new { command = "get_participants", description = "Obtener lista de participantes" });
            // commands.Add(new { command = "get_voting_status", description = "Obtener estado actual de votación" });
            commands.Add(new { command = "help", description = "Mostrar esta ayuda" });

            // Comandos específicos por rol
            if (userRole?.ToLower() == "administrador")
            {
                commands.Add(new { Titulo = "Cambiar tiempo de votación", command = "ajustes", description = "Cambia el valor de tiempoVotacion, el formato debe estar en segundo como se muestra en el ejemplo {\"comando\":\"ajustes\",\"tiempoVotacion\":300, \"terminado\":false }" });
                commands.Add(new { Titulo = "Finalizar el evento", command = "ajustes", description = "Cambia el valor de terminado a true, como se muestra en el ejemplo {\"comando\":\"ajustes\",\"tiempoVotacion\":300, \"terminado\":true }" });
                commands.Add(new { Titulo = "Iniciar Votacion", command = "iniciar_votacion", description = "Para iniciar una votacion es necesario el id del participante y iniciar votacion true, como se muestra en el ejemplo: {\"comando\":\"iniciar_votacion\",\"iniciarVotacion\":true,\"idParticipante\":1}" });
                commands.Add(new { Titulo = "Cancelar Votacion", command = "iniciar_votacion", description = "Para cancelar una votacion es necesario el id del participante y iniciar votacion false, como se muestra en el ejemplo: {\"comando\":\"iniciar_votacion\",\"iniciarVotacion\":false,\"idParticipante\":1}" });
                commands.Add(new { Titulo = "Desempate", command = "desempatar_votacion", description = "Para desempatar una votacion es necesario el id del participante, como se muestra en el ejemplo: {\"comando\":\"desempatar_votacion\",\"idParticipante\":1}" });

                // commands.Add(new { command = "get_dashboard", description = "Obtener datos completos del dashboard (solo admin)" });
                // commands.Add(new { command = "get_ranking", description = "Obtener ranking completo (solo admin)" });
                // commands.Add(new { command = "start_voting", description = "Iniciar votación (solo admin)" });
                // commands.Add(new { comando = "ajustes", description = "Modificar ajustes del evento (solo admin)" });
                // commands.Add(new { comando = "participante", description = "Iniciar/cancelar votación de participante (solo admin)" });
            }
            else if (userRole?.ToLower() == "votante" || userRole?.ToLower() == "publico")
            {
                commands.Add(new { Titulo = "Enviar una votación", command = "vote", description = "Enviar los datos como se muestra en el ejemplo {\"command\": \"vote\",\"idParticipante\": 1,\"atuendo\": 5,\"maquillaje\": 5,\"tradiciones\": 5,\"pasarela\": 5,\"interaccion\": 5}" });
                // commands.Add(new { command = "vote", description = "Enviar voto (solo votantes/jueces)" });
            }

            var response = new
            {
                type = "help_message",
                timestamp = DateTime.UtcNow,
                user = new { role = userRole },
                message = "Comandos disponibles para tu rol:",
                commands = commands,
                example = new
                {
                    format = "Envía mensajes JSON con el campo 'command' o 'comando'",
                    sample = new { command = "get_user_info" },
                    ajustesExample = new { comando = "ajustes", tiempoVotacion = 300, terminado = false },
                    participanteExample = new { comando = "participante", iniciarVotacion = true, idParticipante = 1 }
                }
            };

            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al enviar mensaje de ayuda: {ex.Message}");
            var errorResponse = new
            {
                type = "error",
                timestamp = DateTime.UtcNow,
                error = "help_error",
                message = ex.Message
            };
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(errorResponse, JsonOptions));
        }
    }

    /// Procesa el comando de ajustes enviado por un administrador
    private async Task ProcessAjustes(JsonElement messageObject)
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // Si no es administrador, denegar acceso
            if (userRole?.ToLower() != "administrador")
            {
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
                {
                    type = "access_denied",
                    message = "No cuenta con permisos para realizar esta acción."
                }, JsonOptions));
                return;
            }

            // Extraer datos de ajustes
            var tiempoVotacion = messageObject.TryGetProperty("tiempoVotacion", out var tiempoElement) ? tiempoElement.GetInt32() : 300;
            var terminado = messageObject.TryGetProperty("terminado", out var terminadoElement) ? terminadoElement.GetBoolean() : false;

            // Marcar todos los ajustes existentes como inactivos (activo = 0)
            var ajustesExistentes = await _context.Ajustes.ToListAsync();
            foreach (var ajuste in ajustesExistentes)
            {
                ajuste.Activo = false;
            }

            // Crear un nuevo registro de ajuste que será el único activo
            var nuevoAjuste = new CatrinasAPI.Models.Ajuste
            {
                Tiempo_de_Votacion = tiempoVotacion,
                Publicacion_Resultados = terminado,
                Fecha = DateTime.UtcNow,
                Activo = true // Marcar como activo
            };

            _context.Ajustes.Add(nuevoAjuste);
            await _context.SaveChangesAsync();


            var adminData = await GetAdminDashboardData();

            // Notificar a otros administradores del cambio
            await Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(adminData, JsonOptions));
            await Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetVotanteData(), JsonOptions));


        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al procesar ajustes: {ex.Message}");
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
            {
                type = "error",
                message = ex.Message
            }, JsonOptions));
        }
    }

    private async Task ProcessParticipanteCommand(JsonElement messageObject)
    {
        try
        {
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole?.ToLower() != "administrador")
            {
                var accessDeniedResponse = new
                {
                    type = "access_denied",
                    timestamp = DateTime.UtcNow,
                    message = "No cuenta con permisos para realizar esta acción."
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(accessDeniedResponse, JsonOptions));
                return;
            }

            // Extraer datos del participante
            var iniciarVotacion = messageObject.TryGetProperty("iniciarVotacion", out var iniciarElement)
                ? iniciarElement.GetBoolean() : false;
            var idParticipante = messageObject.TryGetProperty("idParticipante", out var idElement)
                ? idElement.GetInt32() : 0;

            if (idParticipante <= 0)
            {
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = "ID de participante inválido"
                }, JsonOptions));
                return;
            }

            // Buscar el participante
            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.Id_Participante == idParticipante);

            if (participante == null)
            {
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = "Participante no encontrado"
                }, JsonOptions));
                return;
            }

            if (iniciarVotacion)
            {
                // Iniciar votación: cambiar estado a "En Espera" (2)
                participante.Id_Estado = 2;

                // Obtener tiempo de votación de ajustes
                var ultimoAjuste = await _context.Ajustes
                    .Where(a => a.Activo == true)
                    .OrderByDescending(a => a.Id_Ajuste)
                    .FirstOrDefaultAsync();

                var tiempoDuracion = ultimoAjuste?.Tiempo_de_Votacion ?? 300;

                // Agregar al diccionario de votaciones en curso
                _votacionesEnCurso[idParticipante] = new VotacionEnCurso
                {
                    IdParticipante = idParticipante,
                    Participante = participante.Nombre,
                    TiempoInicio = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    TiempoDuracion = tiempoDuracion
                };
                await _context.SaveChangesAsync();

                // Notificar a otros administradores del cambio
                await Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetAdminDashboardData(), JsonOptions));

                // Notificar a todos los votantes del cambio
                await Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetVotanteData(), JsonOptions));

                // Iniciar cuenta regresiva en segundo plano
                _ = Task.Run(async () =>
                {
                    Console.WriteLine($"[TIMER] Iniciando cuenta regresiva para participante {idParticipante} por {tiempoDuracion} segundos");
                    var tiempoRestante = tiempoDuracion;
                    while (tiempoRestante > 0)
                    {
                        await Task.Delay(1000); // Esperar 1 segundo
                        tiempoRestante--;

                        Console.WriteLine($"[TIMER] Tiempo restante: {tiempoRestante} segundos para participante {idParticipante}");
                        if (!_votacionesEnCurso.ContainsKey(idParticipante))
                        {
                            Console.WriteLine($"[TIMER] Votación ya finalizada manualmente para participante {idParticipante}");
                            break; // Salir si la votación fue finalizada manualmente
                        }
                    }
                    if (tiempoRestante <= 0 && _votacionesEnCurso.ContainsKey(idParticipante))
                    {
                        Console.WriteLine($"[TIMER] ¡TIEMPO TERMINADO! Finalizando votación automáticamente para participante {idParticipante}");
                        await FinalizarVotacionAutomatica(idParticipante);
                    }
                });
            }
            else
            {
                // Cancelar votación: cambiar estado a "Registrado" (1)
                participante.Id_Estado = 1;

                // Desactivar todas las evaluaciones de este participante
                var evaluacionesParticipante = await _context.Evaluaciones
                    .Where(e => e.Id_Participante == idParticipante)
                    .ToListAsync();

                foreach (var evaluacion in evaluacionesParticipante)
                {
                    evaluacion.Activo = false;
                }

                // Remover del diccionario de votaciones en curso
                _votacionesEnCurso.Remove(idParticipante);

                await _context.SaveChangesAsync();



                // Notificar a otros administradores del cambio
                await Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetAdminDashboardData(), JsonOptions));

                // Notificar a todos los votantes del cambio
                await Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetVotanteData(), JsonOptions));

            }

        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al procesar comando de participante: {ex.Message}");
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
            {
                type = "error",
                message = ex.Message
            }, JsonOptions));
        }
    }

    private async Task DesempatarVotacionCommand(JsonElement messageObject)
    {
        try
        {

            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole?.ToLower() != "administrador")
            {
                var accessDeniedResponse = new
                {
                    type = "access_denied",
                    timestamp = DateTime.UtcNow,
                    message = "No cuenta con permisos para realizar esta acción."
                };
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(accessDeniedResponse, JsonOptions));
                return;
            }

            // Extraer datos del participante

            var idParticipante = messageObject.TryGetProperty("idParticipante", out var idElement)
                ? idElement.GetInt32() : 0;

            if (idParticipante <= 0)
            {
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = "ID de participante inválido"
                }, JsonOptions));
                return;
            }

            // Buscar el participante
            var participanteDesempate = await _context.Rankings
                .FirstOrDefaultAsync(p => p.Id_Participante == idParticipante);

            if (participanteDesempate == null)
            {
                await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = "Participante no encontrado"
                }, JsonOptions));
                return;
            }

            var calculoDesempate = participanteDesempate.PuntosDesempate + 0.01m;
            var PuntosTotales = participanteDesempate.Puntos + calculoDesempate;

            participanteDesempate.PuntosDesempate = calculoDesempate;
            participanteDesempate.Puntos = PuntosTotales;

            // Actualizar el registro existente
            _context.Rankings.Update(participanteDesempate);
            await _context.SaveChangesAsync();

            await Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(await GetAdminDashboardData(), JsonOptions));

        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error al procesar comando de desempate: {ex.Message}");
            await Clients.Caller.SendAsync("ServerResponse", JsonSerializer.Serialize(new
            {
                type = "error",
                message = ex.Message
            }, JsonOptions));
        }
    }

    /// <summary>
    /// Maneja la cuenta regresiva de la votación, enviando actualizaciones cada segundo
    /// </summary>
    private async Task IniciarCuentaRegresiva(int idParticipante, string nombreParticipante, int tiempoDuracion)
    {
        try
        {
            _logger.LogInformation($"[BASIC HUB] Iniciando cuenta regresiva para participante {idParticipante} ({nombreParticipante}) por {tiempoDuracion} segundos");
            var tiempoRestante = tiempoDuracion;


            try
            {
                // Crear un nuevo scope para acceder a los servicios de SignalR de forma segura
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BasicHub>>();
                    // Enviar a administradores usando el contexto independiente
                    await hubContext.Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(new
                    {
                        type = "countdown_started",
                        idParticipante = idParticipante,
                        participante = nombreParticipante,
                        tiempoRestante = tiempoRestante,
                        tiempoTotal = tiempoDuracion,
                        mensaje = $"Votación iniciada para {nombreParticipante} - Duración: {tiempoDuracion} segundos"
                    }, JsonOptions));

                    // Enviar a votantes usando el contexto independiente
                    await hubContext.Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(new
                    {
                        type = "countdown_started",
                        idParticipante = idParticipante,
                        participante = nombreParticipante,
                        tiempoRestante = tiempoRestante,
                        mensaje = $"¡Votación iniciada! Puedes votar por {nombreParticipante} durante {tiempoDuracion} segundos"
                    }, JsonOptions));

                }

                _logger.LogInformation($"[BASIC HUB] Mensaje inicial de votación enviado: {tiempoDuracion}s de duración");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[BASIC HUB] Error enviando mensaje inicial: {ex.Message}");
            }

            // Cuenta regresiva silenciosa - solo logs, sin enviar mensajes cada segundo
            while (tiempoRestante > 0)
            {
                await Task.Delay(1000); // Esperar 1 segundo
                tiempoRestante--;

                // Verificar si la votación aún está activa (puede haber sido finalizada o cancelada manualmente)
                if (!_votacionesEnCurso.ContainsKey(idParticipante))
                {
                    _logger.LogInformation($"[BASIC HUB] Votación ya finalizada/cancelada manualmente para participante {idParticipante}");
                    break; // Salir si la votación fue finalizada/cancelada manualmente
                }

                _logger.LogInformation($"[BASIC HUB] Tiempo restante: {tiempoRestante} segundos para participante {idParticipante}");
            }

            // Verificar si la votación aún está activa al finalizar la cuenta regresiva
            if (tiempoRestante <= 0 && _votacionesEnCurso.ContainsKey(idParticipante))
            {
                _logger.LogInformation($"[BASIC HUB] Tiempo agotado para participante {idParticipante}, finalizando automáticamente");

                // Enviar mensaje final antes de cerrar la votación
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BasicHub>>();

                        // Enviar mensaje final a administradores
                        await hubContext.Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(new
                        {
                            type = "countdown_finished",
                            idParticipante = idParticipante,
                            participante = nombreParticipante,
                            tiempoRestante = 0,
                            mensaje = $"¡Tiempo agotado! Finalizando votación para {nombreParticipante}"
                        }, JsonOptions));

                        // Enviar mensaje final a votantes
                        await hubContext.Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(new
                        {
                            type = "countdown_finished",
                            idParticipante = idParticipante,
                            participante = nombreParticipante,
                            tiempoRestante = 0,
                            mensaje = $"¡Tiempo agotado! Ya no puedes votar por {nombreParticipante}"
                        }, JsonOptions));
                    }

                    _logger.LogInformation($"[BASIC HUB] Mensaje final enviado - votación terminada");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[BASIC HUB] Error enviando mensaje final: {ex.Message}");
                }

                await FinalizarVotacionAutomatica(idParticipante);
            }
            else
            {
                _logger.LogInformation($"[BASIC HUB] Cuenta regresiva terminada para participante {idParticipante} - votación ya finalizada manualmente");
            }


            // Enviar datos actualizados del dashboard después de finalizar
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BasicHub>>();
                    var contextDb = scope.ServiceProvider.GetRequiredService<CatrinasDbContext>();
                    var chatHubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                    // Crear una instancia temporal para obtener los datos
                    var tempHub = new BasicHub(contextDb, _serviceScopeFactory, chatHubContext, _logger);
                    var adminDataActualizada = await tempHub.GetAdminDashboardData();
                    var votanteDataActualizada = await tempHub.GetVotanteData();

                    // Enviar datos actualizados
                    await hubContext.Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(adminDataActualizada, JsonOptions));
                    await hubContext.Clients.Group("Votantes").SendAsync("ServerResponse", JsonSerializer.Serialize(votanteDataActualizada, JsonOptions));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[BASIC HUB] Error enviando datos actualizados después de finalizar: {ex.Message}");
                //Aqui mandar mensaje de error al administrador 
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error en cuenta regresiva para participante {idParticipante}: {ex.Message}");
        }
    }

    /// <summary>
    /// Finaliza automáticamente la votación cuando se agota el tiempo
    /// </summary>
    private async Task FinalizarVotacionAutomatica(int idParticipante)
    {
        try
        {
            _logger.LogInformation($"[BASIC HUB] Iniciando finalización automática para participante {idParticipante}");

            // Crear un nuevo scope para el contexto de base de datos
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CatrinasDbContext>();

                var participanteEnVotacion = await context.Participantes
                    .Where(p => p.Id_Participante == idParticipante && p.Id_Estado == 2) // Verificar que aún esté en votación
                    .FirstOrDefaultAsync();

                if (participanteEnVotacion == null)
                {
                    _logger.LogInformation($"[BASIC HUB] Participante {idParticipante} ya no está en votación - saliendo");
                    return; // La votación ya fue finalizada manualmente
                }

                _logger.LogInformation($"[BASIC HUB] Participante {participanteEnVotacion.Nombre} encontrado, cambiando estado a Calificado");

                // Cambiar estado a Calificado (3)
                participanteEnVotacion.Id_Estado = 3;

                // Calcular puntaje total para el ranking
                var evaluaciones = await context.Evaluaciones
                    .Where(e => e.Id_Participante == idParticipante && e.Activo)
                    .ToListAsync();

                _logger.LogInformation($"[BASIC HUB] Encontradas {evaluaciones.Count} evaluaciones para participante {idParticipante}");

                decimal puntajeTotal = 0;
                int atuendoTotal = 0;
                int maquillajeTotal = 0;
                int tradicionesTotal = 0;
                int pasarelaTotal = 0;
                int interaccionTotal = 0;
                if (evaluaciones.Any())
                {
                    // Sumatoria de la columna Total de todas las evaluaciones
                    puntajeTotal = evaluaciones.Sum(e => e.Total);
                    _logger.LogInformation($"[BASIC HUB] Suma de columna Total: {puntajeTotal:F2}");
                    atuendoTotal = evaluaciones.Sum(e => e.Atuendo);
                    maquillajeTotal = evaluaciones.Sum(e => e.Maquillaje);
                    tradicionesTotal = evaluaciones.Sum(e => e.Tradiciones);
                    pasarelaTotal = evaluaciones.Sum(e => e.Pasarela);
                    interaccionTotal = evaluaciones.Sum(e => e.Interaccion);
                }

                // Verificar si ya existe un registro en Ranking para este participante
                var rankingExistente = await context.Rankings
                    .FirstOrDefaultAsync(r => r.Id_Participante == idParticipante);

                if (rankingExistente != null)
                {
                    // Actualizar puntaje existente
                    rankingExistente.Puntos = Math.Round(puntajeTotal, 2)+ rankingExistente.PuntosDesempate;//Se toman solo 2 decimales
                    rankingExistente.TotalAtuendo = atuendoTotal;
                    rankingExistente.TotalMaquillaje = maquillajeTotal;
                    rankingExistente.TotalTradiciones = tradicionesTotal;
                    rankingExistente.TotalPasarela = pasarelaTotal;
                    rankingExistente.TotalInteraccion = interaccionTotal;
                    rankingExistente.FechaActualizacion = DateTime.Now;
                    rankingExistente.Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos";
                    _logger.LogInformation($"[BASIC HUB] Actualizando ranking existente para participante {idParticipante}");
                }
                else
                {
                    // Crear nuevo registro en ranking
                    var nuevoRanking = new CatrinasAPI.Models.Ranking
                    {
                        Id_Participante = idParticipante,
                        Puntos = Math.Round(puntajeTotal, 2),
                        TotalAtuendo = atuendoTotal,
                        TotalMaquillaje = maquillajeTotal,
                        TotalTradiciones = tradicionesTotal,
                        TotalPasarela = pasarelaTotal,
                        TotalInteraccion = interaccionTotal,
                        FechaActualizacion = DateTime.Now,
                        Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos"
                    };
                    context.Rankings.Add(nuevoRanking);
                    _logger.LogInformation($"[BASIC HUB] Creando nuevo ranking para participante {idParticipante}");
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"[BASIC HUB] Cambios guardados en base de datos");

                // Remover de votaciones en curso
                _votacionesEnCurso.Remove(idParticipante);
                _logger.LogInformation($"[BASIC HUB] Removido de votaciones en curso. Votaciones activas: {_votacionesEnCurso.Count}");


            }

            // Notificar finalización automática a administradores y votantes usando scope independiente
            try
            {

                // Crear instancia temporal con el contexto del scope actual
                using var dataScope = _serviceScopeFactory.CreateScope();
                var tempContext = dataScope.ServiceProvider.GetRequiredService<CatrinasDbContext>();
                var tempChatHubContext = dataScope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                var tempLogger = dataScope.ServiceProvider.GetRequiredService<ILogger<BasicHub>>();

                var tempHub = new BasicHub(tempContext, _serviceScopeFactory, tempChatHubContext, tempLogger);

                // Preparar datos actualizados para administradores
                var adminDataActualizada = await tempHub.GetAdminDashboardData();
                // var adminMessage = new {
                //     type = "dashboard_updated",
                //     timestamp = DateTime.UtcNow,
                //     reason = "voting_finished_automatically",
                //     data = adminDataActualizada
                // };

                // Preparar datos actualizados para votantes
                var votanteDataActualizada = await tempHub.GetVotanteData();
                // var votanteMessage = new {
                //     type = "voting_status_updated", 
                //     timestamp = DateTime.UtcNow,
                //     reason = "voting_finished_automatically",
                //     data = votanteDataActualizada
                // };

                // Enviar mensajes específicos por grupo usando el hub context existente
                using var notificationScope = _serviceScopeFactory.CreateScope();
                var hubContext = notificationScope.ServiceProvider.GetRequiredService<IHubContext<BasicHub>>();

                await hubContext.Clients.Group("Administradores").SendAsync("ServerResponse",
                    JsonSerializer.Serialize(adminDataActualizada, JsonOptions));

                await hubContext.Clients.Group("Votantes").SendAsync("ServerResponse",
                    JsonSerializer.Serialize(votanteDataActualizada, JsonOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[BASIC HUB] Error enviando notificaciones de finalización: {ex.Message}");
            }

            _logger.LogInformation($"[BASIC HUB] Finalización automática completada para participante {idParticipante}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[BASIC HUB] Error en finalización automática: {ex.Message}");

            // Notificar error a administradores usando scope independiente
            try
            {
                using (var errorScope = _serviceScopeFactory.CreateScope())
                {
                    var hubContext = errorScope.ServiceProvider.GetRequiredService<IHubContext<BasicHub>>();

                    var errorMessage = new
                    {
                        type = "error_finalizacion",
                        timestamp = DateTime.UtcNow,
                        mensaje = $"Error al finalizar votación automáticamente: {ex.Message}"
                    };

                    await hubContext.Clients.Group("Administradores").SendAsync("ServerResponse", JsonSerializer.Serialize(errorMessage, JsonOptions));
                }
            }
            catch (Exception notificationEx)
            {
                _logger.LogError($"[BASIC HUB] Error enviando notificación de error: {notificationEx.Message}");
            }
        }
    }
}
