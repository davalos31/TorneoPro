using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class notificacione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_tipo_notificacion { get; set; }

    public int id_usuario_destino { get; set; }

    public string titulo { get; set; } = null!;

    public string mensaje { get; set; } = null!;

    public string? prioridad { get; set; }

    public int? id_torneo { get; set; }

    public int? id_equipo { get; set; }

    public int? id_partido { get; set; }

    public int? id_multa { get; set; }

    public int? id_suspension { get; set; }

    public string? accion_url { get; set; }

    public string? accion_tipo { get; set; }

    public bool? leida { get; set; }

    public DateTime? fecha_lectura { get; set; }

    public bool? archivada { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_programada_envio { get; set; }

    public bool? enviada { get; set; }

    public DateTime? fecha_envio { get; set; }

    public string? canal { get; set; }

    public bool? email_enviado { get; set; }

    public bool? push_enviado { get; set; }

    public bool? sms_enviado { get; set; }

    public string? metadata { get; set; }

    public virtual equipo? id_equipoNavigation { get; set; }

    public virtual multa? id_multaNavigation { get; set; }

    public virtual partido? id_partidoNavigation { get; set; }

    public virtual jugadores_suspensione? id_suspensionNavigation { get; set; }

    public virtual tipos_notificacion id_tipo_notificacionNavigation { get; set; } = null!;

    public virtual torneo? id_torneoNavigation { get; set; }

    public virtual usuario id_usuario_destinoNavigation { get; set; } = null!;
}
