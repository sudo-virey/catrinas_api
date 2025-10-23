using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class Ranking
{
    [Key]
    public int Id_Ranking { get; set; }
    
    [Required]
    public int Id_Participante { get; set; }
    
    [Required]
    [Range(0, 1000)]
    public decimal Puntos { get; set; } = 0;
    
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    
    [MaxLength(500)]
    public string? Observaciones { get; set; }
    
    // Relación con Participante
    [ForeignKey("Id_Participante")]
    public virtual Participante Participante { get; set; } = null!;
}