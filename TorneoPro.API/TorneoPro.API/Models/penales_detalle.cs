using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class penales_detalle
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_tanda_penales { get; set; }

    public int id_equipo { get; set; }

    public int id_jugador { get; set; }

    public int numero_orden { get; set; }

    public bool anotado { get; set; }

    public string? resultado { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual penales_tandum id_tanda_penalesNavigation { get; set; } = null!;
}
