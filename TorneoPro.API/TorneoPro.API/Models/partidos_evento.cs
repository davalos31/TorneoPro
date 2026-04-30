using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class partidos_evento
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public int id_equipo { get; set; }

    public int? id_jugador { get; set; }

    public string tipo_evento { get; set; } = null!;

    public int minuto { get; set; }

    public string? tiempo { get; set; }

    public string? descripcion { get; set; }

    public int? id_jugador_sale { get; set; }

    public int? id_jugador_entra { get; set; }

    public bool? es_autogol { get; set; }

    public bool? es_penal { get; set; }

    public int? id_jugador_asistencia { get; set; }

    public DateTime? fecha_registro { get; set; }

    public int? id_usuario_registro { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario? id_jugadorNavigation { get; set; }

    public virtual usuario? id_jugador_asistenciaNavigation { get; set; }

    public virtual usuario? id_jugador_entraNavigation { get; set; }

    public virtual usuario? id_jugador_saleNavigation { get; set; }

    public virtual partido id_partidoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_registroNavigation { get; set; }

    public virtual ICollection<jugadores_suspensione> jugadores_suspensiones { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<multa> multa { get; set; } = new List<multa>();
}
