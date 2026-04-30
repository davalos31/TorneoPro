using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class motivos_reprogramacion
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? requiere_aprobacion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<fechas_bloqueada> fechas_bloqueada { get; set; } = new List<fechas_bloqueada>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramaciones { get; set; } = new List<partidos_reprogramacione>();
}
