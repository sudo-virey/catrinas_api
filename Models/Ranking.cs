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
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal PuntosDesempate { get; set; } = 0;
    
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    
    [MaxLength(500)]
    public string? Observaciones { get; set; }
    
    // Columnas para totales por categoría
    public int TotalAtuendo { get; set; } = 0;
    public int TotalMaquillaje { get; set; } = 0;
    public int TotalTradiciones { get; set; } = 0;
    public int TotalPasarela { get; set; } = 0;
    public int TotalInteraccion { get; set; } = 0;
    
    // Relación con Participante
    [ForeignKey("Id_Participante")]
    public virtual Participante Participante { get; set; } = null!;
}