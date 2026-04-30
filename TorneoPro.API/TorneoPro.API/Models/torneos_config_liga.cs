using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_config_liga
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public bool? ida_y_vuelta { get; set; }

    public int? numero_vueltas { get; set; }

    public int? puntos_victoria { get; set; }

    public int? puntos_empate { get; set; }

    public int? puntos_derrota { get; set; }

    public int? puntos_wo_ganador { get; set; }

    public int? puntos_wo_perdedor { get; set; }

    public bool? aplica_penales_en_empate { get; set; }

    public int? puntos_victoria_penales { get; set; }

    public int? puntos_derrota_penales { get; set; }

    public string? criterio_desempate_1 { get; set; }

    public string? criterio_desempate_2 { get; set; }

    public string? criterio_desempate_3 { get; set; }

    public string? criterio_desempate_4 { get; set; }

    public string? criterio_desempate_5 { get; set; }

    public string? criterio_desempate_6 { get; set; }

    public string? criterio_desempate_7 { get; set; }

    public int? jornadas_totales { get; set; }

    public int? descansos_por_equipo { get; set; }

    public int? equipos_descienden { get; set; }

    public int? equipos_ascienden { get; set; }

    public int? equipos_clasifican_copa { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public bool? activo { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
