using System.ComponentModel.DataAnnotations;

namespace CatrinasAPI.Models;

public class Acceso
{
    [Key]
    public int Id_Acceso { get; set; }
    
    [Required]
    [MaxLength(6)]
    public string Acceso1 { get; set; } = string.Empty;
    
    // Distinción entre jurado y público
    public bool EsJurado { get; set; } // true = jurado, false = público

    // Navegación
    public virtual ICollection<HistorialAcceso> HistorialAccesos { get; set; } = new List<HistorialAcceso>();
    public virtual ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
}