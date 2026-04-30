using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class equipos_torneo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_equipo { get; set; }

    public int id_torneo { get; set; }

    public DateTime? fecha_inscripcion { get; set; }

    public bool? aprobado { get; set; }

    public DateTime? fecha_aprobacion { get; set; }

    public int? id_usuario_aprobacion { get; set; }

    public int? id_torneo_fase { get; set; }

    public bool? tiene_bye { get; set; }

    public int? id_fase_bye { get; set; }

    public string? estado { get; set; }

    public string? motivo_estado { get; set; }

    public string? metadata { get; set; }

    public bool? activo { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual torneos_fase? id_fase_byeNavigation { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual torneos_fase? id_torneo_faseNavigation { get; set; }

    public virtual usuario? id_usuario_aprobacionNavigation { get; set; }
}
