using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class fechas_bloqueada
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int? id_cancha { get; set; }

    public DateTime fecha_inicio { get; set; }

    public DateTime fecha_fin { get; set; }

    public string motivo { get; set; } = null!;

    public int? id_motivo_catalogo { get; set; }

    public bool? aplica_a_todas_canchas { get; set; }

    public int? id_usuario_registro { get; set; }

    public DateTime? fecha_registro { get; set; }

    public bool? activo { get; set; }

    public string? metadata { get; set; }

    public virtual cancha? id_canchaNavigation { get; set; }

    public virtual motivos_reprogramacion? id_motivo_catalogoNavigation { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_registroNavigation { get; set; }
}
