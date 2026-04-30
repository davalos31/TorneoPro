using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class partidos_observacione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public int id_arbitro { get; set; }

    public string observacion { get; set; } = null!;

    public bool? es_publica { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual usuario id_arbitroNavigation { get; set; } = null!;

    public virtual partido id_partidoNavigation { get; set; } = null!;
}
