using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class solicitudes_equipo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_equipo { get; set; }

    public int id_jugador { get; set; }

    public string? mensaje { get; set; }

    public string? comentario { get; set; }

    public string estado { get; set; } = null!;

    public DateTime fecha_solicitud { get; set; }

    public DateTime? fecha_procesamiento { get; set; }

    public int? id_usuario_procesador { get; set; }

    public bool activo { get; set; }

    public DateTime fecha_creacion { get; set; }

    public DateTime fecha_modificacion { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_procesadorNavigation { get; set; }
}
