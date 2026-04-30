using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class multa
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int id_tipo_multa { get; set; }

    public int? id_equipo { get; set; }

    public int? id_jugador { get; set; }

    public int? id_partido { get; set; }

    public int? id_evento { get; set; }

    public bool? generada_automaticamente { get; set; }

    public decimal monto { get; set; }

    public decimal? monto_pagado { get; set; }

    public string? moneda { get; set; }

    public string? estado { get; set; }

    public DateOnly? fecha_limite_pago { get; set; }

    public DateTime? fecha_aplicacion { get; set; }

    public string? descripcion { get; set; }

    public string? notas_internas { get; set; }

    public int? id_usuario_registro { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public string? metadata { get; set; }

    public virtual equipo? id_equipoNavigation { get; set; }

    public virtual partidos_evento? id_eventoNavigation { get; set; }

    public virtual usuario? id_jugadorNavigation { get; set; }

    public virtual partido? id_partidoNavigation { get; set; }

    public virtual tipos_multum id_tipo_multaNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_registroNavigation { get; set; }

    public virtual ICollection<multas_historial_pago> multas_historial_pagos { get; set; } = new List<multas_historial_pago>();

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();
}
