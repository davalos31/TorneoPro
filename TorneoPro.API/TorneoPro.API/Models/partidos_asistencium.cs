using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class partidos_asistencium
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public int id_jugador { get; set; }

    public int id_equipo { get; set; }

    public bool asistio { get; set; }

    public string? motivo_ausencia { get; set; }

    public int? id_usuario_registro { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual partido id_partidoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_registroNavigation { get; set; }
}
