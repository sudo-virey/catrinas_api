using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CatrinasAPI.Models;

public class HistorialAcceso
{
    [Key]
    public int Id_Historial_Acceso { get; set; }
    
    [Required]
    public int Id_Acceso { get; set; }
    
    [Required]
    public DateTime Fecha { get; set; } = DateTime.Now;
    
    // Navegación
    [ForeignKey("Id_Acceso")]
    public virtual Acceso Acceso { get; set; } = null!;
}