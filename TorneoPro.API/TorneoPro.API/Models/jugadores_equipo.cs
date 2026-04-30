using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class jugadores_equipo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_jugador { get; set; }

    public int id_equipo { get; set; }

    public int? numero_camiseta { get; set; }

    public string? posicion { get; set; }

    public bool? es_capitan { get; set; }

    public string? estado { get; set; }

    public DateTime? fecha_inicio { get; set; }

    public DateTime? fecha_fin { get; set; }

    public bool? activo { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;
}
