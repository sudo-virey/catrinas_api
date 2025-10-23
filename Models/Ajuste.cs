using System.ComponentModel.DataAnnotations;

namespace CatrinasAPI.Models;

public class Ajuste
{
    [Key]
    public int Id_Ajuste { get; set; }
    
    [Required]
    public int Tiempo_de_Votacion { get; set; }
    
    [Required]
    public bool Publicacion_Resultados { get; set; } = false;
    
    [Required]
    public DateTime Fecha { get; set; } = DateTime.Now;
    
    [Required]
    public bool Activo { get; set; } = true;
}