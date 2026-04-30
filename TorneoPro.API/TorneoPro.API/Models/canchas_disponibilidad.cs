using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class canchas_disponibilidad
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_cancha { get; set; }

    public int? id_torneo { get; set; }

    public DateTime fecha_hora_inicio { get; set; }

    public DateTime fecha_hora_fin { get; set; }

    public bool? disponible { get; set; }

    public string? motivo_bloqueo { get; set; }

    public int? id_partido { get; set; }

    public int? id_usuario_registro { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual cancha id_canchaNavigation { get; set; } = null!;

    public virtual partido? id_partidoNavigation { get; set; }

    public virtual torneo? id_torneoNavigation { get; set; }

    public virtual usuario? id_usuario_registroNavigation { get; set; }
}
