using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneo
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public int id_deporte { get; set; }

    public int id_formato { get; set; }

    public int? id_torneo_origen { get; set; }

    public bool? fue_clonado { get; set; }

    public int? numero_clonacion { get; set; }

    public DateOnly fecha_inicio { get; set; }

    public DateOnly? fecha_fin { get; set; }

    public DateOnly? fecha_limite_inscripcion { get; set; }

    public string? ubicacion { get; set; }

    public string? ciudad { get; set; }

    public string? pais { get; set; }

    public decimal? latitud { get; set; }

    public decimal? longitud { get; set; }

    public int equipos_minimos { get; set; }

    public int? equipos_maximos { get; set; }

    public bool? permite_equipos_impares { get; set; }

    public int? edad_minima { get; set; }

    public int? edad_maxima { get; set; }

    public string? genero_permitido { get; set; }

    public string? estado { get; set; }

    public string? fase_actual { get; set; }

    public bool? es_publico { get; set; }

    public bool? requiere_aprobacion_equipos { get; set; }

    public string? reglamento_url { get; set; }

    public string? reglamento_nombre_archivo { get; set; }

    public DateTime? reglamento_fecha_subida { get; set; }

    public int id_organizador { get; set; }

    public string? logo_url { get; set; }

    public string? banner_url { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public int? id_usuario_modificacion { get; set; }

    public bool? activo { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<torneo> Inverseid_torneo_origenNavigation { get; set; } = new List<torneo>();

    public virtual ICollection<canchas_disponibilidad> canchas_disponibilidads { get; set; } = new List<canchas_disponibilidad>();

    public virtual ICollection<credenciales_jugadore> credenciales_jugadores { get; set; } = new List<credenciales_jugadore>();

    public virtual ICollection<enlaces_compartido> enlaces_compartidos { get; set; } = new List<enlaces_compartido>();

    public virtual ICollection<equipos_torneo> equipos_torneos { get; set; } = new List<equipos_torneo>();

    public virtual ICollection<estadisticas_equipos_torneo> estadisticas_equipos_torneos { get; set; } = new List<estadisticas_equipos_torneo>();

    public virtual ICollection<estadisticas_jugadore> estadisticas_jugadores { get; set; } = new List<estadisticas_jugadore>();

    public virtual ICollection<fechas_bloqueada> fechas_bloqueada { get; set; } = new List<fechas_bloqueada>();

    public virtual deporte id_deporteNavigation { get; set; } = null!;

    public virtual tipos_formato_torneo id_formatoNavigation { get; set; } = null!;

    public virtual usuario id_organizadorNavigation { get; set; } = null!;

    public virtual torneo? id_torneo_origenNavigation { get; set; }

    public virtual usuario? id_usuario_modificacionNavigation { get; set; }

    public virtual ICollection<jornadas_imagenes_sociale> jornadas_imagenes_sociales { get; set; } = new List<jornadas_imagenes_sociale>();

    public virtual ICollection<jugadores_suspensione> jugadores_suspensiones { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<multa> multa { get; set; } = new List<multa>();

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();

    public virtual ICollection<partido> partidos { get; set; } = new List<partido>();

    public virtual torneos_config_eliminatorium? torneos_config_eliminatorium { get; set; }

    public virtual torneos_config_grupos_eliminatorium? torneos_config_grupos_eliminatorium { get; set; }

    public virtual ICollection<torneos_config_horarios_liga> torneos_config_horarios_ligas { get; set; } = new List<torneos_config_horarios_liga>();

    public virtual torneos_config_liga? torneos_config_liga { get; set; }

    public virtual ICollection<torneos_config_multa> torneos_config_multa { get; set; } = new List<torneos_config_multa>();

    public virtual ICollection<torneos_fase> torneos_fases { get; set; } = new List<torneos_fase>();

    public virtual ICollection<torneos_historial_clonacion> torneos_historial_clonacionid_torneo_clonadoNavigations { get; set; } = new List<torneos_historial_clonacion>();

    public virtual ICollection<torneos_historial_clonacion> torneos_historial_clonacionid_torneo_origenNavigations { get; set; } = new List<torneos_historial_clonacion>();

    public virtual ICollection<torneos_llafe> torneos_llaves { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_sembrado> torneos_sembrados { get; set; } = new List<torneos_sembrado>();

    public virtual ICollection<usuarios_role> usuarios_roles { get; set; } = new List<usuarios_role>();
}
