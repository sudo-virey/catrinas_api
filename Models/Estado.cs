using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class Estado
{
    [Key]
    public int Id_Estado { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Estado1 { get; set; } = string.Empty; // Ej: "Registrado", "En Espera", "En Votación", "Calificado"
    
    [MaxLength(200)]
    public string? Descripcion { get; set; } // Descripción opcional del estado
    
    // Navegación
    public virtual ICollection<Participante> Participantes { get; set; } = new List<Participante>();
}