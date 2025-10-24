using Microsoft.EntityFrameworkCore;
using CatrinasAPI.Models;

namespace CatrinasAPI.Data;

public class CatrinasDbContext : DbContext
{
    public CatrinasDbContext(DbContextOptions<CatrinasDbContext> options) : base(options)
    {
    }

    // DbSets para cada tabla
    public DbSet<Estado> Estados { get; set; }
    public DbSet<Participante> Participantes { get; set; }
    public DbSet<Acceso> Accesos { get; set; }
    public DbSet<HistorialAcceso> HistorialAccesos { get; set; }
    public DbSet<Ajuste> Ajustes { get; set; }
    public DbSet<Evaluacion> Evaluaciones { get; set; }
    public DbSet<Models.Usuarios> UsuariosAdmin { get; set; } // Tabla para administradores
    public DbSet<Ranking> Rankings { get; set; } // Tabla para ranking de participantes

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuraciones específicas de las entidades
        
        // Estado
        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.Id_Estado);
            entity.Property(e => e.Estado1).HasColumnName("Estado");
            entity.HasIndex(e => e.Estado1).IsUnique();
        });

        // Participante
        modelBuilder.Entity<Participante>(entity =>
        {
            entity.HasKey(e => e.Id_Participante);
            entity.HasOne(p => p.Estado)
                  .WithMany(e => e.Participantes)
                  .HasForeignKey(p => p.Id_Estado)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Acceso
        modelBuilder.Entity<Acceso>(entity =>
        {
            entity.HasKey(e => e.Id_Acceso);
            entity.Property(e => e.Acceso1).HasColumnName("Acceso");
            entity.HasIndex(e => e.Acceso1).IsUnique();
        });

        // HistorialAcceso
        modelBuilder.Entity<HistorialAcceso>(entity =>
        {
            entity.HasKey(e => e.Id_Historial_Acceso);
            entity.HasOne(h => h.Acceso)
                  .WithMany(a => a.HistorialAccesos)
                  .HasForeignKey(h => h.Id_Acceso)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Evaluacion
        modelBuilder.Entity<Evaluacion>(entity =>
        {
            entity.HasKey(e => e.Id_Evaluacion);
            
            entity.HasOne(e => e.Participante)
                  .WithMany(p => p.Evaluaciones)
                  .HasForeignKey(e => e.Id_Participante)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Acceso)
                  .WithMany(a => a.Evaluaciones)
                  .HasForeignKey(e => e.Id_Acceso)
                  .OnDelete(DeleteBehavior.Restrict);

            // Índice único para evitar que un acceso evalúe al mismo participante dos veces
            entity.HasIndex(e => new { e.Id_Participante, e.Id_Acceso }).IsUnique();
        });

        // Usuarios Admin (tabla para administradores)
        modelBuilder.Entity<Models.Usuarios>(entity =>
        {
            entity.HasKey(e => e.Id_Usuario);
            entity.HasIndex(e => e.Usuario).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Ranking (nueva tabla)
        modelBuilder.Entity<Ranking>(entity =>
        {
            entity.HasKey(e => e.Id_Ranking);
            
            // Configurar precisión decimal para Puntos
            entity.Property(e => e.Puntos)
                  .HasPrecision(10, 2); // 10 dígitos totales, 2 decimales
            
            entity.HasOne(r => r.Participante)
                  .WithMany(p => p.Rankings)
                  .HasForeignKey(r => r.Id_Participante)
                  .OnDelete(DeleteBehavior.Cascade);

            // Índice único para que cada participante solo tenga un ranking activo
            entity.HasIndex(e => e.Id_Participante).IsUnique();
        });

        // Datos semilla
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Estados del participante (NO estados geográficos)
        modelBuilder.Entity<Estado>().HasData(
            new Estado { Id_Estado = 1, Estado1 = "Registrado", Descripcion = "Participante registrado en el concurso" },
            new Estado { Id_Estado = 2, Estado1 = "En Espera", Descripcion = "Participante en espera de evaluación" },
            new Estado { Id_Estado = 3, Estado1 = "En Votación", Descripcion = "Participante siendo evaluado por jueces" },
            new Estado { Id_Estado = 4, Estado1 = "Calificado", Descripcion = "Participante ya calificado por todos los jueces" },
            new Estado { Id_Estado = 5, Estado1 = "Descalificado", Descripcion = "Participante descalificado del concurso" },
            new Estado { Id_Estado = 6, Estado1 = "Finalista", Descripcion = "Participante clasificado como finalista" }
        );

        // Ajustes por defecto
        modelBuilder.Entity<Ajuste>().HasData(
            new Ajuste 
            { 
                Id_Ajuste = 1, 
                Tiempo_de_Votacion = 30, // 30 minutos
                Publicacion_Resultados = false, 
                Fecha = new DateTime(2025, 10, 23, 12, 0, 0),
                Activo = true 
            }
        );

        // Códigos de acceso de ejemplo
        modelBuilder.Entity<Acceso>().HasData(
            new Acceso { Id_Acceso = 1, Acceso1 = "ADM001" }, // Admin
            new Acceso { Id_Acceso = 2, Acceso1 = "JUE001" }, // Juez 1
            new Acceso { Id_Acceso = 3, Acceso1 = "JUE002" }, // Juez 2
            new Acceso { Id_Acceso = 4, Acceso1 = "JUE003" }  // Juez 3
        );

        // Usuario administrador por defecto
        modelBuilder.Entity<Models.Usuarios>().HasData(
            new Models.Usuarios 
            { 
                Id_Usuario = 1, 
                Usuario = "admin", 
                Password = "admin123", // En producción debe ser hasheado
                Email = "admin@catrinas.com",
                NombreCompleto = "Administrador del Sistema",
                FechaCreacion = new DateTime(2025, 10, 23),
                Activo = true,
                Rol = "Administrador"
            }
        );

        // Participantes de ejemplo
        modelBuilder.Entity<Participante>().HasData(
            new Participante { Id_Participante = 1, Nombre = "Ana García Martínez", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 2, Nombre = "Luis Rodríguez López", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 3, Nombre = "Carmen Flores Sánchez", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 4, Nombre = "Jorge Hernández Vega", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 5, Nombre = "María Isabel Jiménez", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 6, Nombre = "Carlos Eduardo Morales", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 7, Nombre = "Sofia Alejandra Ruiz", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 8, Nombre = "Ricardo Daniel Torres", Id_Estado = 2, Activo = true },
            new Participante { Id_Participante = 9, Nombre = "Alejandra Beatriz Luna", Id_Estado = 1, Activo = true },
            new Participante { Id_Participante = 10, Nombre = "Fernando Javier Castro", Id_Estado = 1, Activo = true }
        );
    }
}