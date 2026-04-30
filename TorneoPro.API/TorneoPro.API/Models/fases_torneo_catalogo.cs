using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class fases_torneo_catalogo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public int orden { get; set; }

    public int? equipos_requeridos { get; set; }

    public string? descripcion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<torneos_fase> torneos_fases { get; set; } = new List<torneos_fase>();
}
