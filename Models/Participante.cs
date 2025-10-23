using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class Participante
{
    [Key]
    public int Id_Participante { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    
    [Required]
    public int Id_Estado { get; set; }
    
    [Required]
    public bool Activo { get; set; } = true;
    
    // Navegación
    [ForeignKey("Id_Estado")]
    public virtual Estado Estado { get; set; } = null!;
    
    public virtual ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
    
    public virtual ICollection<Ranking> Rankings { get; set; } = new List<Ranking>();
}