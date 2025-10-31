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
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

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
                
                // Si es una petición a cualquier hub de SignalR y hay un token
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/chatHub") || path.StartsWithSegments("/basicHub")))
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
builder.Services.AddSignalR(options =>
{
    // Aumentar timeout para evitar desconexiones frecuentes
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(5); // Default: 30 segundos
    options.KeepAliveInterval = TimeSpan.FromMinutes(2); // Default: 15 segundos
    options.HandshakeTimeout = TimeSpan.FromSeconds(30); // Default: 15 segundos
});

// Configurar CORS para WebSockets
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
/*builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});*/

var app = builder.Build();

// Configurar path base para el despliegue en subcarpeta
app.UsePathBase("/API_PRUEBA");

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Habilitar Swagger en todos los entornos
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
        {
            new Microsoft.OpenApi.Models.OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host}{httpReq.PathBase}" }
        };
    });
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/API_PRUEBA/swagger/v1/swagger.json", "Catrinas API V1");
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
app.MapHub<CatrinasAPI.Hubs.BasicHub>("/basicHub");

app.Run();