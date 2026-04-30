using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class estadisticas_equipos_torneo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_equipo { get; set; }

    public int id_torneo { get; set; }

    public int? id_torneo_fase { get; set; }

    public int? partidos_jugados { get; set; }

    public int? partidos_ganados { get; set; }

    public int? partidos_empatados { get; set; }

    public int? partidos_perdidos { get; set; }

    public int? goles_favor { get; set; }

    public int? goles_contra { get; set; }

    public int? diferencia_goles { get; set; }

    public int? puntos { get; set; }

    public int? tarjetas_amarillas { get; set; }

    public int? tarjetas_rojas { get; set; }

    public int? partidos_wo_ganados { get; set; }

    public int? partidos_wo_perdidos { get; set; }

    public int? posicion { get; set; }

    public string? racha_actual { get; set; }

    public DateTime? fecha_actualizacion { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual torneos_fase? id_torneo_faseNavigation { get; set; }
}
