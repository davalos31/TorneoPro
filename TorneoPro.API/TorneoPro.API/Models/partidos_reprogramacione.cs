using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class partidos_reprogramacione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public int id_motivo { get; set; }

    public DateTime fecha_hora_original { get; set; }

    public DateTime fecha_hora_nueva { get; set; }

    public int? id_cancha_original { get; set; }

    public int? id_cancha_nueva { get; set; }

    public int? id_usuario_solicitud { get; set; }

    public DateTime? fecha_solicitud { get; set; }

    public string? motivo_detallado { get; set; }

    public bool? requiere_aprobacion { get; set; }

    public string? estado { get; set; }

    public int? id_usuario_aprobacion { get; set; }

    public DateTime? fecha_aprobacion { get; set; }

    public string? motivo_rechazo { get; set; }

    public bool? generada_automaticamente { get; set; }

    public decimal? score_fecha_sugerida { get; set; }

    public string? metadata { get; set; }

    public virtual cancha? id_cancha_nuevaNavigation { get; set; }

    public virtual cancha? id_cancha_originalNavigation { get; set; }

    public virtual motivos_reprogramacion id_motivoNavigation { get; set; } = null!;

    public virtual partido id_partidoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_aprobacionNavigation { get; set; }

    public virtual usuario? id_usuario_solicitudNavigation { get; set; }
}
