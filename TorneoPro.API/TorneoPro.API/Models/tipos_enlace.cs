using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_enlace
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? permite_expiracion { get; set; }

    public int? max_usos_recomendado { get; set; }

    public string alcance { get; set; } = null!;

    public bool? activo { get; set; }

    public virtual ICollection<enlaces_compartido> enlaces_compartidos { get; set; } = new List<enlaces_compartido>();
}
