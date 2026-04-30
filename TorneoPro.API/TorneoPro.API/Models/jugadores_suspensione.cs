using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class jugadores_suspensione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_jugador { get; set; }

    public int id_equipo { get; set; }

    public int id_torneo { get; set; }

    public int? id_partido_origen { get; set; }

    public int? id_evento_origen { get; set; }

    public string motivo { get; set; } = null!;

    public int partidos_suspension { get; set; }

    public int? partidos_cumplidos { get; set; }

    public DateOnly fecha_inicio { get; set; }

    public DateOnly? fecha_fin_estimada { get; set; }

    public string? estado { get; set; }

    public bool? generada_automaticamente { get; set; }

    public string? regla_aplicada { get; set; }

    public int? id_usuario_registro { get; set; }

    public int? id_usuario_aprobacion { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual partidos_evento? id_evento_origenNavigation { get; set; }

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual partido? id_partido_origenNavigation { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_aprobacionNavigation { get; set; }

    public virtual usuario? id_usuario_registroNavigation { get; set; }

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();
}
