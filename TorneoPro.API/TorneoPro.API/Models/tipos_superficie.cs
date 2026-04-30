using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_superficie
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<cancha> canchas { get; set; } = new List<cancha>();
}
