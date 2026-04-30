using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_config_horarios_liga
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int dia_semana { get; set; }

    public TimeOnly hora_inicio { get; set; }

    public TimeOnly hora_fin { get; set; }

    public int? partidos_por_jornada { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public bool? activo { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
