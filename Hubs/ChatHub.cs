using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CatrinasAPI.Data;
using System.Security.Claims;

namespace CatrinasAPI.Hubs;

[Authorize] // Requiere autenticación JWT
public class ChatHub : Hub
{
    private readonly CatrinasDbContext _context;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<ChatHub> _hubContext;
    
    // Almacenar datos de votación en curso temporalmente
    private static readonly Dictionary<int, VotacionEnCurso> _votacionesEnCurso = new();

    public ChatHub(CatrinasDbContext context, IServiceScopeFactory serviceScopeFactory, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
    }

    // Clase para almacenar datos de votación en curso
    private class VotacionEnCurso
    {
        public int IdParticipante { get; set; }
        public string Participante { get; set; } = string.Empty;
        public long TiempoInicio { get; set; }
        public int TiempoDuracion { get; set; }
    }

    // ===== MÉTODOS PÚBLICOS DEL HUB =====

    /// <summary>
    /// Solicita datos del dashboard según el tipo de usuario
    /// </summary>
    public async Task RequestDashboardData()
    {
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var accessType = Context.User?.FindFirst("AccessType")?.Value;

        try
        {
            if (userRole == "Administrador")
            {
                var adminData = await GetAdminDashboardData();
                await Clients.Caller.SendAsync("DashboardDataAdmin", adminData);
            }
            else if (userRole == "Publico" && accessType == "Votacion")
            {
                var votanteData = await GetVotanteDashboardData();
                await Clients.Caller.SendAsync("DashboardDataVotante", votanteData);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Tipo de usuario no válido para solicitar datos");
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Error obteniendo datos: {ex.Message}");
        }
    }

    /// <summary>
    /// Solicita el ranking - respuesta diferenciada por tipo de usuario
    /// </summary>
    public async Task RequestRanking()
    {
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var accessType = Context.User?.FindFirst("AccessType")?.Value;

        try
        {
            if (userRole == "Administrador")
            {
                // Admin ve ranking completo con detalles
                var adminRanking = await GetAdminRanking();
                await Clients.Caller.SendAsync("RankingDataAdmin", adminRanking);
            }
            else if (userRole == "Publico" && accessType == "Votacion")
            {
                // Votante ve ranking limitado solo si el concurso terminó
                var votanteRanking = await GetVotanteRanking();
                await Clients.Caller.SendAsync("RankingDataVotante", votanteRanking);
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Error obteniendo ranking: {ex.Message}");
        }
    }

    /// <summary>
    /// VOTANTE: Enviar voto para el participante en votación actual
    /// </summary>
    [Authorize(Roles = "Publico")]
    public async Task EnviarVoto(int atuendo, int maquillaje, int tradiciones, int pasarela, int interaccion)
    {
        try
        {
            var accessCode = Context.User?.FindFirst("AccessCode")?.Value;
            var idAcceso = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Buscar el acceso en la BD
            var acceso = await _context.Accesos.FirstOrDefaultAsync(a => a.Acceso1 == accessCode);
            if (acceso == null)
            {
                await Clients.Caller.SendAsync("Error", "Acceso no válido");
                return;
            }

            // Buscar participante en votación (estado 2: En Votación)
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Votación
                .FirstOrDefaultAsync();

            if (participanteEnVotacion == null)
            {
                await Clients.Caller.SendAsync("Error", "No hay votación activa en este momento");
                return;
            }

            // Verificar si ya votó
            var votoExistente = await _context.Evaluaciones
                .FirstOrDefaultAsync(e => e.Id_Participante == participanteEnVotacion.Id_Participante && 
                                         e.Id_Acceso == acceso.Id_Acceso);

            if (votoExistente != null)
            {
                await Clients.Caller.SendAsync("Error", "Ya has votado por este participante");
                return;
            }

            // Validar que las puntuaciones estén en el rango correcto (1-5)
            if (atuendo < 1 || atuendo > 5 || maquillaje < 1 || maquillaje > 5 || 
                tradiciones < 1 || tradiciones > 5 || pasarela < 1 || pasarela > 5 || 
                interaccion < 1 || interaccion > 5)
            {
                await Clients.Caller.SendAsync("Error", "Todas las puntuaciones deben estar entre 1 y 5");
                return;
            }

            // Calcular total en criterios (máximo 25) y convertir a escala de 10
            var totalCriterios = atuendo + maquillaje + tradiciones + pasarela + interaccion;
            var totalPuntos = (decimal)totalCriterios * 10 / 25; // Conversión: 25 criterios = 10 puntos

            // Crear nueva evaluación
            var evaluacion = new CatrinasAPI.Models.Evaluacion
            {
                Id_Participante = participanteEnVotacion.Id_Participante,
                Id_Acceso = acceso.Id_Acceso,
                Atuendo = atuendo,
                Maquillaje = maquillaje,
                Tradiciones = tradiciones,
                Pasarela = pasarela,
                Interaccion = interaccion,
                Total = totalPuntos,
                Activo = true,
                FechaEvaluacion = DateTime.Now
            };

            _context.Evaluaciones.Add(evaluacion);
            await _context.SaveChangesAsync();

            // SOLO confirmar al votante - NO enviar actualizaciones al admin
            await Clients.Caller.SendAsync("VotoConfirmado", new
            {
                participante = participanteEnVotacion.Nombre,
                criterios = new
                {
                    atuendo = atuendo,
                    maquillaje = maquillaje,
                    tradiciones = tradiciones,
                    pasarela = pasarela,
                    interaccion = interaccion,
                    totalCriterios = totalCriterios
                },
                puntosFinal = totalPuntos,
                mensaje = "Tu voto ha sido registrado exitosamente"
            });

        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Error enviando voto: {ex.Message}");
        }
    }

    /// <summary>
    /// ADMIN: Iniciar votación para un participante
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public async Task IniciarVotacion(int idParticipante, int tiempoDuracion = 60) // Por defecto 1 minuto
    {
        try
        {
            // Cambiar estado del participante anterior (si existe) a Calificado
            var participanteAnterior = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Votación
                .FirstOrDefaultAsync();

            if (participanteAnterior != null)
            {
                participanteAnterior.Id_Estado = 3; // Calificado
            }

            // Actualizar estado del nuevo participante a "En Votación"
            var participante = await _context.Participantes.FindAsync(idParticipante);
            if (participante == null)
            {
                await Clients.Caller.SendAsync("Error", "Participante no encontrado");
                return;
            }

            participante.Id_Estado = 2; // En Votación
            await _context.SaveChangesAsync();

            var tiempoInicio = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Almacenar datos de votación en curso
            _votacionesEnCurso[participante.Id_Participante] = new VotacionEnCurso
            {
                IdParticipante = participante.Id_Participante,
                Participante = participante.Nombre,
                TiempoInicio = tiempoInicio,
                TiempoDuracion = tiempoDuracion
            };

            // Datos para administradores
            var datosAdmin = new
            {
                idParticipante = participante.Id_Participante,
                participante = participante.Nombre,
                tiempoInicio = tiempoInicio,
                tiempoDuracion = tiempoDuracion,
                estadoAnterior = participanteAnterior?.Nombre,
                totalVotosEsperados = await _context.Accesos.CountAsync(a => a.Acceso1.StartsWith("JUE"))
            };

            // Datos para votantes
            var datosVotantes = new
            {
                votacionEnCurso = true,
                idParticipante = participante.Id_Participante,
                participante = participante.Nombre,
                tiempoInicio = tiempoInicio,
                tiempoDuracion = tiempoDuracion,
                instrucciones = "Califica del 1 al 5 cada categoría. Total máximo: 25 puntos (equivale a 10 puntos finales)"
            };

            // Notificar a ADMINISTRADORES
            await Clients.Group("Administradores").SendAsync("VotacionIniciada", datosAdmin);

            // Notificar a VOTANTES
            await Clients.Group("Votantes").SendAsync("NuevaVotacionParaVotar", datosVotantes);

            // RESPUESTA INMEDIATA al administrador que inició la votación
            await Clients.Caller.SendAsync("VotacionIniciadaExitosamente", new
            {
                success = true,
                votacionEnCurso = new
                {
                    idParticipante = participante.Id_Participante,
                    participante = participante.Nombre,
                    tiempoInicio = tiempoInicio,
                    tiempoDuracion = tiempoDuracion
                },
                mensaje = $"Votación iniciada para {participante.Nombre} por {tiempoDuracion} segundos"
            });

            // Programar finalización automática de la votación Y cuenta regresiva
            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"[TIMER] Iniciando cuenta regresiva para participante {idParticipante} por {tiempoDuracion} segundos");
                    var tiempoRestante = tiempoDuracion;
                    
                    // Enviar actualizaciones de cuenta regresiva cada segundo
                    while (tiempoRestante > 0)
                    {
                        await Task.Delay(1000); // Esperar 1 segundo
                        tiempoRestante--;
                        
                        Console.WriteLine($"[TIMER] Tiempo restante: {tiempoRestante} segundos para participante {idParticipante}");
                        
                        // Verificar si la votación aún está activa (puede haber sido finalizada manualmente)
                        if (!_votacionesEnCurso.ContainsKey(idParticipante))
                        {
                            Console.WriteLine($"[TIMER] Votación ya finalizada manualmente para participante {idParticipante}");
                            break; // Salir si la votación fue finalizada manualmente
                        }
                        
                        try
                        {
                            // Usar el IHubContext en lugar de la instancia del Hub
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                                
                                // Enviar actualización de cuenta regresiva a todos
                                await hubContext.Clients.Group("Administradores").SendAsync("CuentaRegresiva", new
                                {
                                    idParticipante = idParticipante,
                                    participante = participante.Nombre,
                                    tiempoRestante = tiempoRestante,
                                    tiempoTotal = tiempoDuracion
                                });
                                
                                await hubContext.Clients.Group("Votantes").SendAsync("CuentaRegresiva", new
                                {
                                    idParticipante = idParticipante,
                                    participante = participante.Nombre,
                                    tiempoRestante = tiempoRestante,
                                    mensaje = $"Tiempo restante para votar: {tiempoRestante} segundos"
                                });
                                
                                Console.WriteLine($"[TIMER] Enviado CuentaRegresiva: {tiempoRestante}s restantes");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TIMER ERROR] Error enviando cuenta regresiva: {ex.Message}");
                        }
                    }
                    
                    // Finalizar votación automáticamente cuando llegue a 0
                    if (tiempoRestante <= 0 && _votacionesEnCurso.ContainsKey(idParticipante))
                    {
                        Console.WriteLine($"[TIMER] ¡TIEMPO TERMINADO! Finalizando votación automáticamente para participante {idParticipante}");
                        await FinalizarVotacionAutomaticaConScope(idParticipante);
                    }
                    else
                    {
                        Console.WriteLine($"[TIMER] No se finalizará automáticamente - tiempoRestante: {tiempoRestante}, contiene key: {_votacionesEnCurso.ContainsKey(idParticipante)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TIMER FATAL ERROR] Error en task de cuenta regresiva: {ex.Message}");
                    Console.WriteLine($"[TIMER FATAL ERROR] StackTrace: {ex.StackTrace}");
                }
            });

            // Enviar actualización completa del dashboard a los administradores
            var dashboardActualizado = await GetAdminDashboardData();
            await Clients.Group("Administradores").SendAsync("DashboardDataAdmin", dashboardActualizado);

        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Error iniciando votación: {ex.Message}");
        }
    }

    /// <summary>
    /// Finaliza automáticamente la votación cuando termina el tiempo - Versión con scope independiente
    /// </summary>
    private async Task FinalizarVotacionAutomaticaConScope(int idParticipante)
    {
        try
        {
            Console.WriteLine($"[FINALIZACION AUTO SCOPE] Iniciando finalización automática para participante {idParticipante}");
            
            // Crear un nuevo scope para el contexto de base de datos Y SignalR
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Scope creado correctamente");
                var context = scope.ServiceProvider.GetRequiredService<CatrinasDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Contextos obtenidos correctamente");
                
                var participanteEnVotacion = await context.Participantes
                    .Where(p => p.Id_Participante == idParticipante && p.Id_Estado == 2) // Verificar que aún esté en votación
                    .FirstOrDefaultAsync();

                if (participanteEnVotacion == null)
                {
                    Console.WriteLine($"[FINALIZACION AUTO SCOPE] Participante {idParticipante} ya no está en votación - saliendo");
                    return; // La votación ya fue finalizada manualmente
                }

                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Participante {participanteEnVotacion.Nombre} encontrado, cambiando estado a Calificado");
                
                // Cambiar estado a Calificado
                participanteEnVotacion.Id_Estado = 3;

                // Calcular puntaje total promedio para el ranking
                var evaluaciones = await context.Evaluaciones
                    .Where(e => e.Id_Participante == idParticipante && e.Activo)
                    .ToListAsync();

                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Encontradas {evaluaciones.Count} evaluaciones para participante {idParticipante}");

                decimal puntajeTotal = 0;
                if (evaluaciones.Any())
                {
                    // Sumatoria de la columna Total de todas las evaluaciones
                    puntajeTotal = evaluaciones.Sum(e => e.Total);
                    
                    Console.WriteLine($"[FINALIZACION AUTO SCOPE] Suma de columna Total: {puntajeTotal:F2}");
                }

                // Verificar si ya existe un registro en Ranking para este participante
                var rankingExistente = await context.Rankings
                    .FirstOrDefaultAsync(r => r.Id_Participante == idParticipante);

                if (rankingExistente != null)
                {
                    // Actualizar puntaje existente
                    rankingExistente.Puntos = Math.Round(puntajeTotal, 2);
                    rankingExistente.FechaActualizacion = DateTime.Now;
                    rankingExistente.Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos";
                    Console.WriteLine($"[FINALIZACION AUTO SCOPE] Actualizando ranking existente para participante {idParticipante}");
                }
                else
                {
                    // Crear nuevo registro en ranking
                    var nuevoRanking = new CatrinasAPI.Models.Ranking
                    {
                        Id_Participante = idParticipante,
                        Puntos = Math.Round(puntajeTotal, 2),
                        FechaActualizacion = DateTime.Now,
                        Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos"
                    };
                    context.Rankings.Add(nuevoRanking);
                    Console.WriteLine($"[FINALIZACION AUTO SCOPE] Creando nuevo ranking para participante {idParticipante}");
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Cambios guardados en base de datos");

                // Remover de votaciones en curso (esto deja 0 votaciones activas)
                _votacionesEnCurso.Remove(idParticipante);
                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Removido de votaciones en curso. Votaciones activas: {_votacionesEnCurso.Count}");

                // Calcular resultados finales para notificación
                var resultadosFinales = await ObtenerEstadisticasVotacion(participanteEnVotacion.Id_Participante, context);

                // Notificar SOLO a administradores con los resultados usando hubContext independiente
                await hubContext.Clients.Group("Administradores").SendAsync("VotacionFinalizada", new
                {
                    participante = participanteEnVotacion.Nombre,
                    idParticipante = participanteEnVotacion.Id_Participante,
                    resultados = resultadosFinales,
                    puntajeRanking = Math.Round(puntajeTotal, 2),
                    votosRecibidos = evaluaciones.Count,
                    finalizacionAutomatica = true,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    mensaje = $"Votación finalizada automáticamente. Puntaje en ranking: {Math.Round(puntajeTotal, 2)} puntos"
                });

                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Notificación VotacionFinalizada enviada a administradores");

                // Notificar a votantes que terminó
                await hubContext.Clients.Group("Votantes").SendAsync("VotacionTerminada", new
                {
                    participante = participanteEnVotacion.Nombre,
                    puntajeFinal = Math.Round(puntajeTotal, 2),
                    mensaje = "El tiempo de votación ha terminado. Resultados guardados en el ranking."
                });

                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Notificación VotacionTerminada enviada a votantes");

                // ACTUALIZAR DATOS DEL DASHBOARD PARA ADMINISTRADORES
                // Enviar datos actualizados del dashboard (sin votación en curso, ranking actualizado)
                var adminDataActualizada = await GetAdminDashboardDataFromContext(context);
                await hubContext.Clients.Group("Administradores").SendAsync("DashboardDataAdmin", adminDataActualizada);
                
                Console.WriteLine($"[FINALIZACION AUTO SCOPE] Dashboard actualizado enviado a administradores");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            Console.WriteLine($"[FINALIZACION AUTO SCOPE ERROR] Error en finalización automática: {ex.Message}");
            Console.WriteLine($"[FINALIZACION AUTO SCOPE ERROR] StackTrace: {ex.StackTrace}");
            
            // Intentar notificar error usando scope independiente
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();
                    await hubContext.Clients.Group("Administradores").SendAsync("Error", 
                        $"Error al finalizar votación automáticamente: {ex.Message}");
                }
            }
            catch
            {
                Console.WriteLine($"[FINALIZACION AUTO SCOPE ERROR] No se pudo enviar notificación de error");
            }
        }
    }

    /// <summary>
    /// Finaliza automáticamente la votación cuando termina el tiempo
    /// </summary>
    private async Task FinalizarVotacionAutomatica(int idParticipante)
    {
        try
        {
            Console.WriteLine($"[FINALIZACION AUTO] Iniciando finalización automática para participante {idParticipante}");
            
            // Crear un nuevo scope para el contexto de base de datos
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                Console.WriteLine($"[FINALIZACION AUTO] Scope creado correctamente");
                var context = scope.ServiceProvider.GetRequiredService<CatrinasDbContext>();
                Console.WriteLine($"[FINALIZACION AUTO] Contexto obtenido correctamente");
                
                var participanteEnVotacion = await context.Participantes
                    .Where(p => p.Id_Participante == idParticipante && p.Id_Estado == 2) // Verificar que aún esté en votación
                    .FirstOrDefaultAsync();

                if (participanteEnVotacion == null)
                {
                    Console.WriteLine($"[FINALIZACION AUTO] Participante {idParticipante} ya no está en votación - saliendo");
                    return; // La votación ya fue finalizada manualmente
                }

                Console.WriteLine($"[FINALIZACION AUTO] Participante {participanteEnVotacion.Nombre} encontrado, cambiando estado a Calificado");
                
                // Cambiar estado a Calificado
                participanteEnVotacion.Id_Estado = 3;

                // Calcular puntaje total promedio para el ranking
                var evaluaciones = await context.Evaluaciones
                    .Where(e => e.Id_Participante == idParticipante && e.Activo)
                    .ToListAsync();

                Console.WriteLine($"[FINALIZACION AUTO] Encontradas {evaluaciones.Count} evaluaciones para participante {idParticipante}");

                decimal puntajeTotal = 0;
                if (evaluaciones.Any())
                {
                    // Sumatoria de la columna Total de todas las evaluaciones
                    puntajeTotal = evaluaciones.Sum(e => e.Total);
                    
                    Console.WriteLine($"[FINALIZACION AUTO] Suma de columna Total: {puntajeTotal:F2}");
                }

                // Verificar si ya existe un registro en Ranking para este participante
                var rankingExistente = await context.Rankings
                    .FirstOrDefaultAsync(r => r.Id_Participante == idParticipante);

                if (rankingExistente != null)
                {
                    // Actualizar puntaje existente
                    rankingExistente.Puntos = Math.Round(puntajeTotal, 2);
                    rankingExistente.FechaActualizacion = DateTime.Now;
                    rankingExistente.Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos";
                    Console.WriteLine($"[FINALIZACION AUTO] Actualizando ranking existente para participante {idParticipante}");
                }
                else
                {
                    // Crear nuevo registro en ranking
                    var nuevoRanking = new CatrinasAPI.Models.Ranking
                    {
                        Id_Participante = idParticipante,
                        Puntos = Math.Round(puntajeTotal, 2),
                        FechaActualizacion = DateTime.Now,
                        Observaciones = $"Votación finalizada automáticamente - {evaluaciones.Count} votos recibidos"
                    };
                    context.Rankings.Add(nuevoRanking);
                    Console.WriteLine($"[FINALIZACION AUTO] Creando nuevo ranking para participante {idParticipante}");
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"[FINALIZACION AUTO] Cambios guardados en base de datos");

                // Remover de votaciones en curso (esto deja 0 votaciones activas)
                _votacionesEnCurso.Remove(idParticipante);
                Console.WriteLine($"[FINALIZACION AUTO] Removido de votaciones en curso. Votaciones activas: {_votacionesEnCurso.Count}");

                // Calcular resultados finales para notificación
                var resultadosFinales = await ObtenerEstadisticasVotacion(participanteEnVotacion.Id_Participante, context);

                // Notificar SOLO a administradores con los resultados
                await Clients.Group("Administradores").SendAsync("VotacionFinalizada", new
                {
                    participante = participanteEnVotacion.Nombre,
                    idParticipante = participanteEnVotacion.Id_Participante,
                    resultados = resultadosFinales,
                    puntajeRanking = Math.Round(puntajeTotal, 2),
                    votosRecibidos = evaluaciones.Count,
                    finalizacionAutomatica = true,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    mensaje = $"Votación finalizada automáticamente. Puntaje en ranking: {Math.Round(puntajeTotal, 2)} puntos"
                });

                Console.WriteLine($"[FINALIZACION AUTO] Notificación VotacionFinalizada enviada a administradores");

                // Notificar a votantes que terminó
                await Clients.Group("Votantes").SendAsync("VotacionTerminada", new
                {
                    participante = participanteEnVotacion.Nombre,
                    puntajeFinal = Math.Round(puntajeTotal, 2),
                    mensaje = "El tiempo de votación ha terminado. Resultados guardados en el ranking."
                });

                Console.WriteLine($"[FINALIZACION AUTO] Notificación VotacionTerminada enviada a votantes");

                // ACTUALIZAR DATOS DEL DASHBOARD PARA ADMINISTRADORES
                // Enviar datos actualizados del dashboard (sin votación en curso, ranking actualizado)
                var adminDataActualizada = await GetAdminDashboardDataFromContext(context);
                await Clients.Group("Administradores").SendAsync("DashboardDataAdmin", adminDataActualizada);
                
                Console.WriteLine($"[FINALIZACION AUTO] Dashboard actualizado enviado a administradores");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the application
            Console.WriteLine($"[FINALIZACION AUTO ERROR] Error en finalización automática: {ex.Message}");
            Console.WriteLine($"[FINALIZACION AUTO ERROR] StackTrace: {ex.StackTrace}");
            
            // Notificar a administradores del error
            await Clients.Group("Administradores").SendAsync("Error", 
                $"Error al finalizar votación automáticamente: {ex.Message}");
        }
    }

    /// <summary>
    /// ADMIN: Finalizar votación actual
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public async Task FinalizarVotacion()
    {
        try
        {
            var participanteEnVotacion = await _context.Participantes
                .Where(p => p.Id_Estado == 2) // En Votación
                .FirstOrDefaultAsync();

            if (participanteEnVotacion == null)
            {
                await Clients.Caller.SendAsync("Error", "No hay votación activa");
                return;
            }

            // Cambiar estado a Calificado
            participanteEnVotacion.Id_Estado = 3;

            // Calcular puntaje total promedio para el ranking
            var evaluaciones = await _context.Evaluaciones
                .Where(e => e.Id_Participante == participanteEnVotacion.Id_Participante && e.Activo)
                .ToListAsync();

            decimal puntajeTotal = 0;
            if (evaluaciones.Any())
            {
                // Sumatoria de la columna Total de todas las evaluaciones
                puntajeTotal = evaluaciones.Sum(e => e.Total);
            }

            // Verificar si ya existe un registro en Ranking para este participante
            var rankingExistente = await _context.Rankings
                .FirstOrDefaultAsync(r => r.Id_Participante == participanteEnVotacion.Id_Participante);

            if (rankingExistente != null)
            {
                // Actualizar puntaje existente
                rankingExistente.Puntos = Math.Round(puntajeTotal, 2);
                rankingExistente.FechaActualizacion = DateTime.Now;
                rankingExistente.Observaciones = $"Votación finalizada manualmente - {evaluaciones.Count} votos recibidos";
            }
            else
            {
                // Crear nuevo registro en ranking
                var nuevoRanking = new CatrinasAPI.Models.Ranking
                {
                    Id_Participante = participanteEnVotacion.Id_Participante,
                    Puntos = Math.Round(puntajeTotal, 2),
                    FechaActualizacion = DateTime.Now,
                    Observaciones = $"Votación finalizada manualmente - {evaluaciones.Count} votos recibidos"
                };
                _context.Rankings.Add(nuevoRanking);
            }

            await _context.SaveChangesAsync();

            // Remover de votaciones en curso (esto deja 0 votaciones activas)
            _votacionesEnCurso.Remove(participanteEnVotacion.Id_Participante);

            // Obtener resultados finales
            var resultadosFinales = await ObtenerEstadisticasVotacion(participanteEnVotacion.Id_Participante);

            // Notificar a administradores con resultados completos
            await Clients.Group("Administradores").SendAsync("VotacionFinalizada", new
            {
                participante = participanteEnVotacion.Nombre,
                idParticipante = participanteEnVotacion.Id_Participante,
                resultados = resultadosFinales,
                puntajeRanking = Math.Round(puntajeTotal, 2),
                votosRecibidos = evaluaciones.Count,
                finalizacionManual = true,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                mensaje = $"Votación finalizada manualmente. Puntaje en ranking: {Math.Round(puntajeTotal, 2)} puntos"
            });

            // Notificar a votantes que terminó la votación
            await Clients.Group("Votantes").SendAsync("VotacionTerminada", new
            {
                participante = participanteEnVotacion.Nombre,
                puntajeFinal = Math.Round(puntajeTotal, 2),
                mensaje = "La votación ha sido finalizada por el administrador. Resultados guardados en el ranking."
            });

            // ACTUALIZAR DATOS DEL DASHBOARD PARA ADMINISTRADORES
            // Enviar datos actualizados del dashboard (sin votación en curso, ranking actualizado)
            var adminDataActualizada = await GetAdminDashboardData();
            await Clients.Group("Administradores").SendAsync("DashboardDataAdmin", adminDataActualizada);

        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", $"Error finalizando votación: {ex.Message}");
        }
    }

    // ===== EVENTOS DE CONEXIÓN =====

    public override async Task OnConnectedAsync()
    {
        var userName = Context.User?.Identity?.Name ?? "Usuario";
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var accessType = Context.User?.FindFirst("AccessType")?.Value;
        var accessCode = Context.User?.FindFirst("AccessCode")?.Value;

        string userIcon = GetUserIcon(userRole, accessType);

        // Agregar a grupos según el rol
        if (userRole == "Administrador") {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Administradores");
            
            // Mensaje de bienvenida para admin
            //await Clients.Caller.SendAsync("ReceiveMessage", "Sistema",  $"👑 ¡Bienvenido {userName}! Acceso ADMINISTRATIVO activado.");
            
            // Enviar datos de dashboard automáticamente
            var adminData = await GetAdminDashboardData();
            await Clients.Caller.SendAsync("DashboardDataAdmin", adminData);
        }


        else if (userRole == "Publico" && accessType == "Votacion"){
            await Groups.AddToGroupAsync(Context.ConnectionId, "Votantes");
            
            // Mensaje de bienvenida para votante
            //await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", $"�️ ¡Bienvenido! Código: {accessCode} - Acceso para VOTACIÓN activado.");
            
            // Enviar datos de dashboard automáticamente
            var votanteData = await GetVotanteDashboardData();
            await Clients.Caller.SendAsync("DashboardDataVotante", votanteData);
        }

        // VERIFICAR SI HAY VOTACIÓN ACTIVA al conectarse
        var participanteEnVotacion = await _context.Participantes
            .Where(p => p.Id_Estado == 2) // En Votación
            .FirstOrDefaultAsync();

        if (participanteEnVotacion != null)
        {
            // Hay una votación activa, notificar según el tipo de usuario
            if (userRole == "Administrador")
            {
                // Datos completos para administrador
                var datosVotacionAdmin = _votacionesEnCurso.ContainsKey(participanteEnVotacion.Id_Participante) 
                    ? _votacionesEnCurso[participanteEnVotacion.Id_Participante] 
                    : null;

                await Clients.Caller.SendAsync("VotacionActivaDetectada", new
                {
                    esAdmin = true,
                    idParticipante = participanteEnVotacion.Id_Participante,
                    participante = participanteEnVotacion.Nombre,
                    tiempoInicio = datosVotacionAdmin?.TiempoInicio,
                    tiempoDuracion = datosVotacionAdmin?.TiempoDuracion,
                    mensaje = $"Votación activa detectada para: {participanteEnVotacion.Nombre}"
                });
            }
            else if (userRole == "Publico" && accessType == "Votacion")
            {
                // Datos limitados para votante
                var datosVotacionVotante = _votacionesEnCurso.ContainsKey(participanteEnVotacion.Id_Participante) 
                    ? _votacionesEnCurso[participanteEnVotacion.Id_Participante] 
                    : null;

                await Clients.Caller.SendAsync("VotacionActivaDetectada", new
                {
                    esAdmin = false,
                    votacionEnCurso = true,
                    idParticipante = participanteEnVotacion.Id_Participante,
                    participante = participanteEnVotacion.Nombre,
                    tiempoInicio = datosVotacionVotante?.TiempoInicio,
                    tiempoDuracion = datosVotacionVotante?.TiempoDuracion,
                    instrucciones = "Califica del 1 al 5 cada categoría. Total máximo: 25 puntos",
                    mensaje = $"¡Votación en curso para: {participanteEnVotacion.Nombre}! Puedes votar ahora."
                });
            }
        }

        // Información de conexión
        await Clients.Caller.SendAsync("ReceiveMessage", "Sistema", 
            $"🔗 Conectado: {DateTime.Now:HH:mm:ss} | ID: {Context.ConnectionId[..8]}...");

        // Notificar a otros usuarios
        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            $"{userIcon} {userName} se ha conectado");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "Usuario";
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var accessType = Context.User?.FindFirst("AccessType")?.Value;

        string userIcon = GetUserIcon(userRole, accessType);

        await Clients.Others.SendAsync("ReceiveMessage", "Sistema", 
            $"👋 {userIcon} {userName} se ha desconectado");

        await base.OnDisconnectedAsync(exception);
    }

    // ===== MÉTODOS PRIVADOS PARA OBTENER DATOS =====

    private async Task<object> GetAdminDashboardData()
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

    private async Task<object> GetAdminDashboardDataFromContext(CatrinasDbContext context)
    {
        // Obtener votación en curso desde el diccionario en memoria
        var participanteEnVotacion = await context.Participantes
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
        var participantes = await context.Participantes
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

        // Obtener ranking completo usando el contexto específico
        var ranking = await GetAdminRankingFromContext(context);

        // Obtener ajustes
        var ultimoAjuste = await context.Ajustes
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

    private async Task<object> GetVotanteDashboardData()
    {
        // Verificar si concurso terminó
        var ultimoAjuste = await _context.Ajustes
            .OrderByDescending(a => a.Id_Ajuste)
            .FirstOrDefaultAsync();

        var concursoTerminado = ultimoAjuste?.Publicacion_Resultados ?? false;

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

        // Ranking solo si terminó
        var ranking = concursoTerminado ? await GetVotanteRanking() : new List<object>();

        return new
        {
            concursoTerminado = concursoTerminado,
            votacionEnCurso = votacionEnCurso != null,
            detallesVotacionEnCurso = votacionEnCurso,
            ranking = ranking
        };
    }

    private async Task<List<object>> GetAdminRanking()
    {
        // Ranking completo con todos los detalles para admin - solo participantes calificados (estado 3)
        var rankings = await _context.Participantes
            .Where(p => p.Id_Estado == 3) // Solo participantes calificados
            .GroupJoin(_context.Evaluaciones.Where(e => e.Activo),
                p => p.Id_Participante,
                e => e.Id_Participante,
                (p, evaluaciones) => new
                {
                    IdParticipante = p.Id_Participante,
                    Participante = p.Nombre,
                    SumaTotal = evaluaciones.Sum(e => (decimal?)e.Total) ?? 0,
                    SumaAtuendo = evaluaciones.Sum(e => (int?)e.Atuendo) ?? 0,
                    SumaMaquillaje = evaluaciones.Sum(e => (int?)e.Maquillaje) ?? 0,
                    SumaTradiciones = evaluaciones.Sum(e => (int?)e.Tradiciones) ?? 0,
                    SumaPasarela = evaluaciones.Sum(e => (int?)e.Pasarela) ?? 0,
                    SumaInteraccion = evaluaciones.Sum(e => (int?)e.Interaccion) ?? 0,
                    NumeroEvaluaciones = evaluaciones.Count()
                })
            .OrderByDescending(r => r.SumaTotal)
            .ToListAsync();

        return rankings.Select((r, index) => new
        {
            idParticipante = r.IdParticipante,
            participante = r.Participante,
            puntaje = (decimal)r.SumaTotal,
            detallePuntaje = new
            {
                atuendo = (decimal)r.SumaAtuendo,
                maquillaje = (decimal)r.SumaMaquillaje,
                tradiciones = (decimal)r.SumaTradiciones,
                pasarela = (decimal)r.SumaPasarela,
                interaccion = (decimal)r.SumaInteraccion
            },
            ordenRanking = index + 1,
            numeroEvaluaciones = r.NumeroEvaluaciones // Solo admin ve esto
        }).Cast<object>().ToList();
    }

    private async Task<List<object>> GetAdminRankingFromContext(CatrinasDbContext context)
    {
        // Ranking completo con todos los detalles para admin usando contexto específico - solo participantes calificados (estado 3)
        var rankings = await context.Participantes
            .Where(p => p.Id_Estado == 3) // Solo participantes calificados
            .GroupJoin(context.Evaluaciones.Where(e => e.Activo),
                p => p.Id_Participante,
                e => e.Id_Participante,
                (p, evaluaciones) => new
                {
                    IdParticipante = p.Id_Participante,
                    Participante = p.Nombre,
                    SumaTotal = evaluaciones.Sum(e => (decimal?)e.Total) ?? 0,
                    SumaAtuendo = evaluaciones.Sum(e => (int?)e.Atuendo) ?? 0,
                    SumaMaquillaje = evaluaciones.Sum(e => (int?)e.Maquillaje) ?? 0,
                    SumaTradiciones = evaluaciones.Sum(e => (int?)e.Tradiciones) ?? 0,
                    SumaPasarela = evaluaciones.Sum(e => (int?)e.Pasarela) ?? 0,
                    SumaInteraccion = evaluaciones.Sum(e => (int?)e.Interaccion) ?? 0,
                    NumeroEvaluaciones = evaluaciones.Count()
                })
            .OrderByDescending(r => r.SumaTotal)
            .ToListAsync();

        return rankings.Select((r, index) => new
        {
            idParticipante = r.IdParticipante,
            participante = r.Participante,
            puntaje = (decimal)r.SumaTotal,
            detallePuntaje = new
            {
                atuendo = (decimal)r.SumaAtuendo,
                maquillaje = (decimal)r.SumaMaquillaje,
                tradiciones = (decimal)r.SumaTradiciones,
                pasarela = (decimal)r.SumaPasarela,
                interaccion = (decimal)r.SumaInteraccion
            },
            ordenRanking = index + 1,
            numeroEvaluaciones = r.NumeroEvaluaciones // Solo admin ve esto
        }).Cast<object>().ToList();
    }

    private async Task<List<object>> GetVotanteRanking()
    {
        // Ranking limitado para votantes
        var rankings = await _context.Evaluaciones
            .Include(e => e.Participante)
            .Where(e => e.Activo)
            .GroupBy(e => e.Id_Participante)
            .Select(g => new
            {
                IdParticipante = g.Key,
                Participante = g.First().Participante.Nombre,
                SumaTotal = g.Sum(e => e.Total)
            })
            .OrderByDescending(r => r.SumaTotal)
            .ToListAsync();

        return rankings.Select((r, index) => new
        {
            idParticipante = r.IdParticipante,
            participante = r.Participante,
            puntaje = (decimal)r.SumaTotal, // Menos decimales
            ordenRanking = index + 1
            // Sin detalles de puntaje para votantes
        }).Cast<object>().ToList();
    }

    private async Task<object> ObtenerEstadisticasVotacion(int idParticipante, CatrinasDbContext context)
    {
        var evaluaciones = await context.Evaluaciones
            .Include(e => e.Acceso)
            .Where(e => e.Id_Participante == idParticipante && e.Activo)
            .ToListAsync();

        var totalVotos = evaluaciones.Count;
        var votosEsperados = await context.Accesos.CountAsync(a => a.Acceso1.StartsWith("JUE"));

        if (totalVotos == 0)
        {
            return new
            {
                totalVotos = 0,
                votosEsperados = votosEsperados,
                porcentajeCompletado = 0,
                promedios = new { },
                votosDetalle = new List<object>()
            };
        }

        return new
        {
            totalVotos = totalVotos,
            votosEsperados = votosEsperados,
            porcentajeCompletado = Math.Round((double)totalVotos / votosEsperados * 100, 1),
            promedios = new
            {
                atuendo = Math.Round(evaluaciones.Average(e => e.Atuendo), 2),
                maquillaje = Math.Round(evaluaciones.Average(e => e.Maquillaje), 2),
                tradiciones = Math.Round(evaluaciones.Average(e => e.Tradiciones), 2),
                pasarela = Math.Round(evaluaciones.Average(e => e.Pasarela), 2),
                interaccion = Math.Round(evaluaciones.Average(e => e.Interaccion), 2),
                total = Math.Round(evaluaciones.Average(e => e.Total), 2)
            },
            votosDetalle = evaluaciones.Select(e => new
            {
                juez = e.Acceso.Acceso1,
                atuendo = e.Atuendo,
                maquillaje = e.Maquillaje,
                tradiciones = e.Tradiciones,
                pasarela = e.Pasarela,
                interaccion = e.Interaccion,
                total = e.Total,
                fechaVoto = e.FechaEvaluacion
            }).ToList()
        };
    }

    private async Task<object> ObtenerEstadisticasVotacion(int idParticipante)
    {
        var evaluaciones = await _context.Evaluaciones
            .Include(e => e.Acceso)
            .Where(e => e.Id_Participante == idParticipante && e.Activo)
            .ToListAsync();

        var totalVotos = evaluaciones.Count;
        var votosEsperados = await _context.Accesos.CountAsync(a => a.Acceso1.StartsWith("JUE"));

        if (totalVotos == 0)
        {
            return new
            {
                totalVotos = 0,
                votosEsperados = votosEsperados,
                porcentajeCompletado = 0,
                promedios = new { },
                votosDetalle = new List<object>()
            };
        }

        return new
        {
            totalVotos = totalVotos,
            votosEsperados = votosEsperados,
            porcentajeCompletado = Math.Round((double)totalVotos / votosEsperados * 100, 1),
            promedios = new
            {
                atuendo = Math.Round(evaluaciones.Average(e => e.Atuendo), 2),
                maquillaje = Math.Round(evaluaciones.Average(e => e.Maquillaje), 2),
                tradiciones = Math.Round(evaluaciones.Average(e => e.Tradiciones), 2),
                pasarela = Math.Round(evaluaciones.Average(e => e.Pasarela), 2),
                interaccion = Math.Round(evaluaciones.Average(e => e.Interaccion), 2),
                total = Math.Round(evaluaciones.Average(e => e.Total), 2)
            },
            votosDetalle = evaluaciones.Select(e => new
            {
                juez = e.Acceso.Acceso1,
                atuendo = e.Atuendo,
                maquillaje = e.Maquillaje,
                tradiciones = e.Tradiciones,
                pasarela = e.Pasarela,
                interaccion = e.Interaccion,
                total = e.Total,
                fechaVoto = e.FechaEvaluacion
            }).ToList()
        };
    }

    private static string GetUserIcon(string? userRole, string? accessType)
    {
        return userRole switch
        {
            "Administrador" => "👑",
            "Publico" when accessType == "Votacion" => "🗳️",
            _ => "👤"
        };
    }
}