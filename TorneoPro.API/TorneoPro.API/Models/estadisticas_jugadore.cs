using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class estadisticas_jugadore
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_jugador { get; set; }

    public int id_equipo { get; set; }

    public int id_torneo { get; set; }

    public int? partidos_jugados { get; set; }

    public int? minutos_jugados { get; set; }

    public int? goles { get; set; }

    public int? asistencias { get; set; }

    public int? autogoles { get; set; }

    public int? tarjetas_amarillas { get; set; }

    public int? tarjetas_rojas { get; set; }

    public int? penales_marcados { get; set; }

    public int? penales_fallados { get; set; }

    public int? partidos_suspendido { get; set; }

    public DateTime? fecha_actualizacion { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
