using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class equipo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? nombre_corto { get; set; }

    public string? escudo_url { get; set; }

    public string? color_primario { get; set; }

    public string? color_secundario { get; set; }

    public DateOnly? fecha_fundacion { get; set; }

    public string? ciudad { get; set; }

    public string? pais { get; set; }

    public string? estadio_habitual { get; set; }

    public string? email { get; set; }

    public string? telefono { get; set; }

    public string? sitio_web { get; set; }

    public int? id_capitan { get; set; }

    public int id_creador { get; set; }

    public string? estado { get; set; }

    public bool? verificado { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public bool? activo { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<credenciales_jugadore> credenciales_jugadores { get; set; } = new List<credenciales_jugadore>();

    public virtual ICollection<enlaces_compartido> enlaces_compartidos { get; set; } = new List<enlaces_compartido>();

    public virtual ICollection<equipos_torneo> equipos_torneos { get; set; } = new List<equipos_torneo>();

    public virtual ICollection<estadisticas_equipos_torneo> estadisticas_equipos_torneos { get; set; } = new List<estadisticas_equipos_torneo>();

    public virtual ICollection<estadisticas_jugadore> estadisticas_jugadores { get; set; } = new List<estadisticas_jugadore>();

    public virtual usuario? id_capitanNavigation { get; set; }

    public virtual usuario id_creadorNavigation { get; set; } = null!;

    public virtual ICollection<jugadores_equipo> jugadores_equipos { get; set; } = new List<jugadores_equipo>();

    public virtual ICollection<jugadores_suspensione> jugadores_suspensiones { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<multa> multa { get; set; } = new List<multa>();

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();

    public virtual ICollection<partido> partidoid_equipo_ganador_woNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_equipo_localNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_equipo_visitanteNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partidos_asistencium> partidos_asistencia { get; set; } = new List<partidos_asistencium>();

    public virtual ICollection<partidos_evento> partidos_eventos { get; set; } = new List<partidos_evento>();

    public virtual ICollection<penales_detalle> penales_detalles { get; set; } = new List<penales_detalle>();

    public virtual ICollection<penales_tandum> penales_tanda { get; set; } = new List<penales_tandum>();

    public virtual ICollection<torneos_llafe> torneos_llafeid_equipo_con_byeNavigations { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_llafe> torneos_llafeid_equipo_ganadorNavigations { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_llafe> torneos_llafeid_equipo_inferiorNavigations { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_llafe> torneos_llafeid_equipo_superiorNavigations { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_sembrado> torneos_sembrados { get; set; } = new List<torneos_sembrado>();

    public virtual ICollection<usuarios_role> usuarios_roles { get; set; } = new List<usuarios_role>();
}
