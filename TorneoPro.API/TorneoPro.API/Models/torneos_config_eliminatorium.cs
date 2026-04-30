using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_config_eliminatorium
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public bool? ida_y_vuelta { get; set; }

    public bool? aplica_gol_visitante { get; set; }

    public bool? aplica_prorroga { get; set; }

    public bool? aplica_penales_empate { get; set; }

    public bool? aplica_muerte_subita { get; set; }

    public bool? tiene_dieciseisavos { get; set; }

    public bool? tiene_octavos { get; set; }

    public bool? tiene_cuartos { get; set; }

    public bool? tiene_semifinales { get; set; }

    public bool? tiene_tercer_lugar { get; set; }

    public bool? tiene_final { get; set; }

    public bool? aplica_sembrado { get; set; }

    public string? criterio_sembrado { get; set; }

    public string? sistema_bye { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public bool? activo { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
