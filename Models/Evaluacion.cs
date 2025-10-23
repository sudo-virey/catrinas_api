using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class Evaluacion
{
    [Key]
    public int Id_Evaluacion { get; set; }
    
    [Required]
    public int Id_Participante { get; set; }
    
    [Required]
    public int Id_Acceso { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Atuendo { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Maquillaje { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Tradiciones { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Pasarela { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Interaccion { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal Total { get; set; }
    
    [Required]
    public bool Activo { get; set; } = true;
    
    public DateTime FechaEvaluacion { get; set; } = DateTime.Now;
    
    // Navegación
    [ForeignKey("Id_Participante")]
    public virtual Participante Participante { get; set; } = null!;
    
    [ForeignKey("Id_Acceso")]
    public virtual Acceso Acceso { get; set; } = null!;
}