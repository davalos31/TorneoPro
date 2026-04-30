using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_formato_torneo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<torneo> torneos { get; set; } = new List<torneo>();
}
