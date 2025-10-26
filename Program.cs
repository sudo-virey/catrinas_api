using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using CatrinasAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configurar URLs para aceptar conexiones de red local
builder.WebHost.UseUrls("http://0.0.0.0:5001");

// Agregar servicios al contenedor
builder.Services.AddControllers();

// Configurar Entity Framework con SQL Server
builder.Services.AddDbContext<CatrinasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
        
        // Configuración para SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // Si es una petición al hub de SignalR y hay un token
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

// Agregar servicios de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catrinas API",
        Version = "v1",
        Description = "API para el Sistema de Concurso de Catrinas"
    });
});

// Configurar servicios de WebSocket y SignalR
builder.Services.AddSignalR();

// Configurar CORS para WebSockets
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(origin => 
               {
                   // Permitir localhost y direcciones IP de red local
                   if (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost"))
                       return true;
                   
                   // Permitir IPs de red local (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
                   if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                   {
                       var host = uri.Host;
                       return host.StartsWith("192.168.") || 
                              host.StartsWith("10.") || 
                              (host.StartsWith("172.") && int.TryParse(host.Split('.')[1], out var second) && second >= 16 && second <= 31);
                   }
                   
                   // Permitir también IETAM
                   return origin == "https://ietam.org.mx";
               })
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Configurar path base para el despliegue en subcarpeta
app.UsePathBase("/CATRINAS_API");

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Habilitar Swagger en todos los entornos
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catrinas API V1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Catrinas API - Documentación";
});

// Habilitar archivos estáticos para la página de prueba
app.UseStaticFiles();

// Configurar CORS (debe ir antes de UseRouting)
app.UseCors();

// Configurar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();
app.MapControllers();

// Configurar WebSockets - AHORA ACTIVO
app.MapHub<CatrinasAPI.Hubs.ChatHub>("/chatHub");

app.Run();