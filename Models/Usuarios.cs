using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class Usuarios
{
    [Key]
    public int Id_Usuario { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Usuario { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Password { get; set; } = string.Empty; // Se recomienda hashear en producción
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? NombreCompleto { get; set; }
    
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    public bool Activo { get; set; } = true;
    
    [MaxLength(20)]
    public string Rol { get; set; } = "Administrador"; // Ej: "Administrador", "Moderador"
}