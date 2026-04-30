using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_config_grupos_eliminatorium
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int numero_grupos { get; set; }

    public int equipos_por_grupo { get; set; }

    public bool? grupos_ida_y_vuelta { get; set; }

    public int? numero_vueltas_grupos { get; set; }

    public int? equipos_clasifican_por_grupo { get; set; }

    public bool? aplica_mejores_terceros { get; set; }

    public int? cantidad_mejores_terceros { get; set; }

    public string? criterio_mejor_tercero_1 { get; set; }

    public string? criterio_mejor_tercero_2 { get; set; }

    public string? criterio_mejor_tercero_3 { get; set; }

    public int? puntos_victoria_grupos { get; set; }

    public int? puntos_empate_grupos { get; set; }

    public int? puntos_derrota_grupos { get; set; }

    public int? puntos_wo_ganador_grupos { get; set; }

    public int? puntos_wo_perdedor_grupos { get; set; }

    public string? criterio_desempate_grupo_1 { get; set; }

    public string? criterio_desempate_grupo_2 { get; set; }

    public string? criterio_desempate_grupo_3 { get; set; }

    public string? criterio_desempate_grupo_4 { get; set; }

    public string? criterio_desempate_grupo_5 { get; set; }

    public string? criterio_desempate_grupo_6 { get; set; }

    public bool? elim_ida_y_vuelta { get; set; }

    public bool? elim_gol_visitante { get; set; }

    public bool? elim_aplica_prorroga { get; set; }

    public bool? elim_aplica_penales { get; set; }

    public bool? elim_tiene_tercer_lugar { get; set; }

    public bool? elim_aplica_sembrado { get; set; }

    public string? elim_criterio_sembrado { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public bool? activo { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
