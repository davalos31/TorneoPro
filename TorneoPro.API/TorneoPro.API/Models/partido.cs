using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class partido
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int? id_torneo_fase { get; set; }

    public int? id_llave { get; set; }

    public int id_equipo_local { get; set; }

    public int id_equipo_visitante { get; set; }

    public DateTime fecha_hora { get; set; }

    public int? jornada { get; set; }

    public int? id_cancha { get; set; }

    public string? cancha_descripcion { get; set; }

    public bool? es_partido_ida { get; set; }

    public bool? es_partido_vuelta { get; set; }

    public int? id_partido_ida { get; set; }

    public int? id_arbitro_principal { get; set; }

    public int? id_arbitro_asistente_1 { get; set; }

    public int? id_arbitro_asistente_2 { get; set; }

    public int? id_cuarto_arbitro { get; set; }

    public int? goles_local { get; set; }

    public int? goles_visitante { get; set; }

    public int? goles_local_prorroga { get; set; }

    public int? goles_visitante_prorroga { get; set; }

    public bool? hubo_prorroga { get; set; }

    public bool? hubo_penales { get; set; }

    public int? goles_local_penales { get; set; }

    public int? goles_visitante_penales { get; set; }

    public string? estado { get; set; }

    public int? minuto_actual { get; set; }

    public string? tiempo_actual { get; set; }

    public bool? es_wo { get; set; }

    public int? id_equipo_ganador_wo { get; set; }

    public string? motivo_wo { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public int? id_usuario_modificacion { get; set; }

    public bool? activo { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<partido> Inverseid_partido_idaNavigation { get; set; } = new List<partido>();

    public virtual actas_partido? actas_partido { get; set; }

    public virtual ICollection<canchas_disponibilidad> canchas_disponibilidads { get; set; } = new List<canchas_disponibilidad>();

    public virtual usuario? id_arbitro_asistente_1Navigation { get; set; }

    public virtual usuario? id_arbitro_asistente_2Navigation { get; set; }

    public virtual usuario? id_arbitro_principalNavigation { get; set; }

    public virtual cancha? id_canchaNavigation { get; set; }

    public virtual usuario? id_cuarto_arbitroNavigation { get; set; }

    public virtual equipo? id_equipo_ganador_woNavigation { get; set; }

    public virtual equipo id_equipo_localNavigation { get; set; } = null!;

    public virtual equipo id_equipo_visitanteNavigation { get; set; } = null!;

    public virtual torneos_llafe? id_llaveNavigation { get; set; }

    public virtual partido? id_partido_idaNavigation { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual torneos_fase? id_torneo_faseNavigation { get; set; }

    public virtual usuario? id_usuario_modificacionNavigation { get; set; }

    public virtual ICollection<jugadores_suspensione> jugadores_suspensiones { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<multa> multa { get; set; } = new List<multa>();

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();

    public virtual ICollection<partidos_asistencium> partidos_asistencia { get; set; } = new List<partidos_asistencium>();

    public virtual ICollection<partidos_evento> partidos_eventos { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_observacione> partidos_observaciones { get; set; } = new List<partidos_observacione>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramaciones { get; set; } = new List<partidos_reprogramacione>();

    public virtual penales_tandum? penales_tandum { get; set; }

    public virtual ICollection<torneos_llafe> torneos_llaves { get; set; } = new List<torneos_llafe>();
}
