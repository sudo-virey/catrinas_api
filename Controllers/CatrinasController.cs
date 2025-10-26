using Microsoft.AspNetCore.Mvc;
using CatrinasAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace CatrinasAPI.Controllers;

[ApiController]
[Route("api")]
public class CatrinasController : ControllerBase
{
    private readonly CatrinasDbContext _context;
    private readonly IConfiguration _configuration;

    public CatrinasController(CatrinasDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("websocket-status")]
    public IActionResult GetWebSocketStatus()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var pathBase = Request.PathBase.Value ?? "";
        
        return Ok(new 
        { 
            mensaje = "WebSockets ACTIVO - Chat implementado",
            signalRHubUrl = $"{pathBase}/chatHub",
            urlCompleta = $"{baseUrl}{pathBase}/chatHub",
            estado = "ACTIVO ✅",
            corsConfigured = true,
            allowedOrigins = new[] { "https://ietam.org.mx", "http://localhost:3000", "http://localhost:5000" },
            instrucciones = new
            {
                conectar = $"Usar SignalR client para conectar a {pathBase}/chatHub",
                enviarMensaje = "Llamar método 'SendMessage(user, message)'",
                recibirMensajes = "Escuchar evento 'ReceiveMessage'",
                eventos = new[] { "UserConnected", "UserDisconnected", "ReceiveMessage" }
            },
            diagnostico = new
            {
                scheme = Request.Scheme,
                host = Request.Host.Value,
                pathBase = pathBase,
                fullPath = Request.Path.Value
            }
        });
    }

    [HttpGet("concurso-terminado")]
    public async Task<IActionResult> GetConcursoTerminado()
    {
        try
        {
            // Obtener el último registro de la tabla Ajustes ordenado por Id_Ajuste descendente
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            if (ultimoAjuste == null)
            {
                return Ok(new 
                { 
                    terminado = false,
                    mensaje = "No hay configuración disponible"
                });
            }

            // Verificar si Publicacion_Resultados es true (1)
            bool concursoTerminado = ultimoAjuste.Publicacion_Resultados;

            return Ok(new 
            { 
                terminado = concursoTerminado,
                ultimaActualizacion = ultimoAjuste.Fecha,
                ajusteId = ultimoAjuste.Id_Ajuste
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                terminado = false,
                error = "Error al consultar el estado del concurso",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("validar-acceso")]
    public async Task<IActionResult> ValidarAcceso([FromBody] ValidarAccesoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CodigoAcceso))
            {
                return BadRequest(new { mensaje = "El código de acceso es requerido" });
            }

            // Buscar el código de acceso en la base de datos
            var acceso = await _context.Accesos
                .FirstOrDefaultAsync(a => a.Acceso1 == request.CodigoAcceso);

            // Verificar el estado del concurso
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            bool concursoTerminado = ultimoAjuste?.Publicacion_Resultados ?? false;

            if (acceso == null)
            {
                return Ok(new 
                { 
                    accesoValido = false,
                    idAcceso = (int?)null,
                    concursoTerminado = concursoTerminado,
                    mensaje = "Código de acceso no válido"
                });
            }

            // Generar JWT token para usuario público
            var token = GenerarJwtTokenPublico(acceso);
            return Ok(new 
            { 
                accesoValido = true,
                idAcceso = acceso.Id_Acceso,
                concursoTerminado = concursoTerminado,
                token = token,
                mensaje = "Código de acceso válido"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error interno del servidor", detalle = ex.Message });
        }
    }

    [HttpGet("validar-acceso/{codigoAcceso}")]
    public async Task<IActionResult> ValidarAccesoGet(string codigoAcceso)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoAcceso))
            {
                return BadRequest(new 
                { 
                    accesoValido = false,
                    idAcceso = (int?)null,
                    concursoTerminado = false,
                    mensaje = "Código de acceso requerido"
                });
            }

            // Buscar el código de acceso en la tabla
            var acceso = await _context.Accesos
                .FirstOrDefaultAsync(a => a.Acceso1 == codigoAcceso);

            // Obtener el estado del concurso (último ajuste)
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            bool concursoTerminado = ultimoAjuste?.Publicacion_Resultados ?? false;

            if (acceso == null)
            {
                return Ok(new 
                { 
                    accesoValido = false,
                    idAcceso = (int?)null,
                    concursoTerminado = concursoTerminado,
                    mensaje = "Código de acceso no válido"
                });
            }

            // Generar JWT token para usuario público
            var token = GenerarJwtTokenPublico(acceso);
            return Ok(new 
            { 
                accesoValido = true,
                idAcceso = acceso.Id_Acceso,
                concursoTerminado = concursoTerminado,
                token = token,
                mensaje = "Acceso válido"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                accesoValido = false,
                idAcceso = (int?)null,
                concursoTerminado = false,
                error = "Error al validar el acceso",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new 
                { 
                    accesoPermitido = false,
                    idUsuario = (int?)null,
                    nombre = "",
                    token = "",
                    mensaje = "Usuario y contraseña son requeridos"
                });
            }

            // Buscar el usuario en la tabla UsuariosAdmin
            var usuario = await _context.UsuariosAdmin
                .FirstOrDefaultAsync(u => u.Usuario == request.Usuario && u.Activo);

            if (usuario == null || !VerificarPassword(request.Password, usuario.Password))
            {
                return Ok(new 
                { 
                    accesoPermitido = false,
                    idUsuario = (int?)null,
                    nombre = "",
                    token = "",
                    mensaje = "Credenciales inválidas"
                });
            }

            // Generar JWT token
            var token = GenerarJwtToken(usuario);

            return Ok(new 
            { 
                accesoPermitido = true,
                idUsuario = usuario.Id_Usuario,
                nombre = usuario.NombreCompleto ?? usuario.Usuario,
                token = token,
                mensaje = "Login exitoso"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                accesoPermitido = false,
                idUsuario = (int?)null,
                nombre = "",
                token = "",
                error = "Error en el proceso de login",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("test-jwt")]
    public IActionResult GenerarJwtDePrueba()
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                jwtSettings["Key"] ?? "Mi_Clave_Super_Secreta_Para_JWT_Que_Debe_Ser_Muy_Larga_Y_Segura_123456789"));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-123"),
                new Claim(ClaimTypes.Name, "Usuario de Prueba"),
                new Claim(ClaimTypes.Email, "test@ejemplo.com"),
                new Claim(ClaimTypes.Role, "TestUser"),
                new Claim("FullName", "Usuario de Prueba Chat"),
                new Claim("AccessType", "Admin"), // Tipo de acceso para prueba
                new Claim("idUsuario", "999"), // ID de usuario de prueba
                new Claim("IdAcceso", "0") // ID de acceso 0 para prueba tipo Admin
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "CatrinasAPI",
                audience: jwtSettings["Audience"] ?? "CatrinasClient",
                claims: claims,
                expires: DateTime.Now.AddHours(2), // Token válido por 2 horas
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new 
            { 
                mensaje = "JWT de prueba generado exitosamente",
                token = tokenString,
                expira = DateTime.Now.AddHours(2),
                usuario = "Usuario de Prueba",
                instrucciones = "Copia este token y úsalo en el campo JWT del chat-test.html"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                error = "Error generando JWT de prueba",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("verify-jwt")]
    [Authorize] // Requiere JWT válido
    public IActionResult VerificarJwt()
    {
        try
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { mensaje = "Token JWT no proporcionado o inválido" });
            }

            // Extraer todos los claims del JWT
            var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
            
            // Extraer información específica
            var accessType = User.FindFirst("AccessType")?.Value ?? "No especificado";
            var idUsuario = User.FindFirst("idUsuario")?.Value ?? "No especificado";
            var idAcceso = User.FindFirst("IdAcceso")?.Value ?? "No especificado";
            var fullName = User.FindFirst("FullName")?.Value ?? User.Identity.Name ?? "No especificado";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "No especificado";

            return Ok(new 
            { 
                mensaje = "JWT válido y autenticado",
                tokenInfo = new
                {
                    AccessType = accessType,
                    IdUsuario = idUsuario,
                    IdAcceso = idAcceso,
                    NombreCompleto = fullName,
                    Rol = role,
                    Usuario = User.Identity.Name,
                    EstaAutenticado = User.Identity.IsAuthenticated
                },
                todosLosClaims = claims,
                verificadoEn = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                error = "Error verificando JWT",
                detalle = ex.Message
            });
        }
    }

    private bool VerificarPassword(string passwordIngresado, string passwordAlmacenado)
    {
        // Por ahora comparación simple - en producción usar hash
        return passwordIngresado == passwordAlmacenado;
    }

    private string GenerarJwtToken(CatrinasAPI.Models.Usuarios usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            jwtSettings["Key"] ?? "Mi_Clave_Super_Secreta_Para_JWT_Que_Debe_Ser_Muy_Larga_Y_Segura_123456789"));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id_Usuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Usuario),
            new Claim(ClaimTypes.Email, usuario.Email ?? ""),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("FullName", usuario.NombreCompleto ?? usuario.Usuario),
            new Claim("AccessType", "Admin"), // AccessType Admin para login
            new Claim("idUsuario", usuario.Id_Usuario.ToString()), // idUsuario del usuario logueado
            new Claim("IdAcceso", "0") // IdAcceso 0 para login
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "CatrinasAPI",
            audience: jwtSettings["Audience"] ?? "CatrinasClient",
            claims: claims,
            expires: DateTime.Now.AddHours(8), // Token válido por 8 horas
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Modelo para recibir el request de login
    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Modelo para recibir el request de validación de acceso
    public class ValidarAccesoRequest
    {
        public string CodigoAcceso { get; set; } = string.Empty;
    }

    private string GenerarJwtTokenPublico(CatrinasAPI.Models.Acceso acceso)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            jwtSettings["Key"] ?? "Mi_Clave_Super_Secreta_Para_JWT_Que_Debe_Ser_Muy_Larga_Y_Segura_123456789"));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, acceso.Id_Acceso.ToString()),
            new Claim(ClaimTypes.Name, acceso.Acceso1),
            new Claim(ClaimTypes.Role, "Publico"),
            new Claim("AccessCode", acceso.Acceso1),
            new Claim("AccessType", "Votacion"), // AccessType Votacion para validar-acceso
            new Claim("idUsuario", "0"), // idUsuario 0 para validar-acceso
            new Claim("IdAcceso", acceso.Id_Acceso.ToString()) // IdAcceso del acceso validado
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "CatrinasAPI",
            audience: jwtSettings["Audience"] ?? "CatrinasClient",
            claims: claims,
            expires: DateTime.Now.AddHours(2), // Token válido por 2 horas para votación
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ===== ENDPOINTS ESPECIALIZADOS POR TIPO DE USUARIO =====

    /// <summary>
    /// Endpoint EXCLUSIVO para usuarios ADMINISTRADORES
    /// Devuelve datos completos de administración del concurso
    /// </summary>
    [HttpGet("admin/dashboard")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        try
        {
            // Verificar que es realmente un admin
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Administrador")
            {
                return Forbid("Acceso denegado: Solo administradores");
            }

            // Obtener votación en curso
            var votacionEnCurso = await ObtenerVotacionEnCurso();
            
            // Obtener todos los participantes con su estado
            var participantes = await _context.Participantes
                .Include(p => p.Estado)
                .OrderBy(p => p.Orden)
                .Select(p => new
                {
                    idParticipante = p.Id_Participante,
                    participante = p.Nombre,
                    estado = p.Id_Estado,
                    orden = p.Orden // Usar columna Orden de la tabla
                })
                .ToListAsync();

            // Obtener ranking completo
            var ranking = await ObtenerRankingCompleto();

            // Obtener ajustes del concurso
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            var response = new
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

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error obteniendo dashboard admin", detalle = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint EXCLUSIVO para usuarios VOTANTES/PUBLICOS
    /// Devuelve datos limitados apropiados para votantes
    /// </summary>
    [HttpGet("votante/dashboard")]
    [Authorize(Roles = "Publico")]
    public async Task<IActionResult> GetVotanteDashboard()
    {
        try
        {
            // Verificar que es realmente un votante
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var accessType = User.FindFirst("AccessType")?.Value;
            
            if (userRole != "Publico" || accessType != "Votacion")
            {
                return Forbid("Acceso denegado: Solo votantes públicos");
            }

            // Verificar si el concurso ha terminado
            var ultimoAjuste = await _context.Ajustes
                .OrderByDescending(a => a.Id_Ajuste)
                .FirstOrDefaultAsync();

            var concursoTerminado = ultimoAjuste?.Publicacion_Resultados ?? false;

            // Obtener votación en curso (si la hay)
            var votacionEnCurso = await ObtenerVotacionEnCurso();
            
            // Obtener ranking (solo si el concurso terminó o hay una votación activa)
            var ranking = concursoTerminado ? await ObtenerRankingCompleto() : new List<object>();

            var response = new
            {
                concursoTerminado = concursoTerminado,
                votacionEnCurso = votacionEnCurso != null,
                detallesVotacionEnCurso = votacionEnCurso,
                ranking = ranking
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error obteniendo dashboard votante", detalle = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint para validar acceso y redirigir al dashboard apropiado
    /// </summary>
    [HttpGet("dashboard/info")]
    [Authorize]
    public IActionResult GetDashboardInfo()
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var accessType = User.FindFirst("AccessType")?.Value;

            var dashboardInfo = new
            {
                usuario = userName,
                rol = userRole,
                tipoAcceso = accessType,
                endpoints = userRole switch
                {
                    "Administrador" => new { dashboard = "/api/admin/dashboard", tipo = "admin" },
                    "Publico" => new { dashboard = "/api/votante/dashboard", tipo = "votante" },
                    _ => new { dashboard = "", tipo = "unknown" }
                }
            };

            return Ok(dashboardInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error obteniendo info dashboard", detalle = ex.Message });
        }
    }

    // ===== MÉTODOS AUXILIARES =====

    private async Task<object?> ObtenerVotacionEnCurso()
    {
        // Por ahora simulamos - puedes implementar lógica real de votación en curso
        // Esto podría venir de una tabla de sesiones de votación o similar
        
        var participanteEnVotacion = await _context.Participantes
            .Include(p => p.Estado)
            .Where(p => p.Id_Estado == 2) // Estado "En Espera" (votación activa)
            .FirstOrDefaultAsync();

        if (participanteEnVotacion == null)
            return null;

        return new
        {
            idParticipante = participanteEnVotacion.Id_Participante,
            participante = participanteEnVotacion.Nombre,
            tiempoInicio = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), // Timestamp actual
            tiempoDuracion = 300 // 5 minutos por defecto
        };
    }

    private async Task<List<object>> ObtenerRankingCompleto()
    {
        // Obtener ranking basado en evaluaciones suma total - solo participantes calificados (estado 3)
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
                    SumaInteraccion = evaluaciones.Sum(e => (int?)e.Interaccion) ?? 0
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
            ordenRanking = index + 1
        }).Cast<object>().ToList();
    }
}
