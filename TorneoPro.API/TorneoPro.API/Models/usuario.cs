using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class usuario
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_tipo_usuario { get; set; }

    public string nombres { get; set; } = null!;

    public string apellidos { get; set; } = null!;

    public DateOnly? fecha_nacimiento { get; set; }

    public string? genero { get; set; }

    public int? id_tipo_documento { get; set; }

    public string? numero_documento { get; set; }

    public string? foto_documento_url { get; set; }

    public string email { get; set; } = null!;

    public string? telefono { get; set; }

    public string? telefono_emergencia { get; set; }

    public decimal? peso_kg { get; set; }

    public decimal? altura_cm { get; set; }

    public string? pais { get; set; }

    public string? ciudad { get; set; }

    public string? direccion { get; set; }

    public string password_hash { get; set; } = null!;

    public string? salt { get; set; }

    public int? intentos_fallidos { get; set; }

    public DateTime? bloqueado_hasta { get; set; }

    public string? foto_perfil_url { get; set; }

    public string? biografia { get; set; }

    public bool? email_verificado { get; set; }

    public bool? telefono_verificado { get; set; }

    public bool? activo { get; set; }

    public DateTime? fecha_ultima_conexion { get; set; }

    public DateTime? fecha_registro { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<actas_partido> actas_partidoid_arbitro_firmanteNavigations { get; set; } = new List<actas_partido>();

    public virtual ICollection<actas_partido> actas_partidoid_usuario_creacionNavigations { get; set; } = new List<actas_partido>();

    public virtual ICollection<auditoria_acceso> auditoria_accesos { get; set; } = new List<auditoria_acceso>();

    public virtual ICollection<canchas_disponibilidad> canchas_disponibilidads { get; set; } = new List<canchas_disponibilidad>();

    public virtual ICollection<credenciales_jugadore> credenciales_jugadoreid_jugadorNavigations { get; set; } = new List<credenciales_jugadore>();

    public virtual ICollection<credenciales_jugadore> credenciales_jugadoreid_usuario_generacionNavigations { get; set; } = new List<credenciales_jugadore>();

    public virtual ICollection<enlaces_compartido> enlaces_compartidos { get; set; } = new List<enlaces_compartido>();

    public virtual ICollection<enlaces_historial_uso> enlaces_historial_usos { get; set; } = new List<enlaces_historial_uso>();

    public virtual ICollection<equipo> equipoid_capitanNavigations { get; set; } = new List<equipo>();

    public virtual ICollection<equipo> equipoid_creadorNavigations { get; set; } = new List<equipo>();

    public virtual ICollection<equipos_torneo> equipos_torneos { get; set; } = new List<equipos_torneo>();

    public virtual ICollection<estadisticas_jugadore> estadisticas_jugadores { get; set; } = new List<estadisticas_jugadore>();

    public virtual ICollection<fechas_bloqueada> fechas_bloqueada { get; set; } = new List<fechas_bloqueada>();

    public virtual ICollection<ia_recomendacione> ia_recomendaciones { get; set; } = new List<ia_recomendacione>();

    public virtual tipos_documento? id_tipo_documentoNavigation { get; set; }

    public virtual tipos_usuario id_tipo_usuarioNavigation { get; set; } = null!;

    public virtual ICollection<jornadas_imagenes_sociale> jornadas_imagenes_sociales { get; set; } = new List<jornadas_imagenes_sociale>();

    public virtual ICollection<jugadores_equipo> jugadores_equipos { get; set; } = new List<jugadores_equipo>();

    public virtual ICollection<jugadores_suspensione> jugadores_suspensioneid_jugadorNavigations { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<jugadores_suspensione> jugadores_suspensioneid_usuario_aprobacionNavigations { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<jugadores_suspensione> jugadores_suspensioneid_usuario_registroNavigations { get; set; } = new List<jugadores_suspensione>();

    public virtual ICollection<multa> multaid_jugadorNavigations { get; set; } = new List<multa>();

    public virtual ICollection<multa> multaid_usuario_registroNavigations { get; set; } = new List<multa>();

    public virtual ICollection<multas_historial_pago> multas_historial_pagoid_usuario_pagoNavigations { get; set; } = new List<multas_historial_pago>();

    public virtual ICollection<multas_historial_pago> multas_historial_pagoid_usuario_verificacionNavigations { get; set; } = new List<multas_historial_pago>();

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();

    public virtual ICollection<partido> partidoid_arbitro_asistente_1Navigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_arbitro_asistente_2Navigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_arbitro_principalNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_cuarto_arbitroNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partido> partidoid_usuario_modificacionNavigations { get; set; } = new List<partido>();

    public virtual ICollection<partidos_asistencium> partidos_asistenciumid_jugadorNavigations { get; set; } = new List<partidos_asistencium>();

    public virtual ICollection<partidos_asistencium> partidos_asistenciumid_usuario_registroNavigations { get; set; } = new List<partidos_asistencium>();

    public virtual ICollection<partidos_evento> partidos_eventoid_jugadorNavigations { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_evento> partidos_eventoid_jugador_asistenciaNavigations { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_evento> partidos_eventoid_jugador_entraNavigations { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_evento> partidos_eventoid_jugador_saleNavigations { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_evento> partidos_eventoid_usuario_registroNavigations { get; set; } = new List<partidos_evento>();

    public virtual ICollection<partidos_observacione> partidos_observaciones { get; set; } = new List<partidos_observacione>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramacioneid_usuario_aprobacionNavigations { get; set; } = new List<partidos_reprogramacione>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramacioneid_usuario_solicitudNavigations { get; set; } = new List<partidos_reprogramacione>();

    public virtual ICollection<penales_detalle> penales_detalles { get; set; } = new List<penales_detalle>();

    public virtual ICollection<preferencias_notificacione> preferencias_notificaciones { get; set; } = new List<preferencias_notificacione>();

    public virtual ICollection<tokens_recuperacion> tokens_recuperacions { get; set; } = new List<tokens_recuperacion>();

    public virtual ICollection<torneo> torneoid_organizadorNavigations { get; set; } = new List<torneo>();

    public virtual ICollection<torneo> torneoid_usuario_modificacionNavigations { get; set; } = new List<torneo>();

    public virtual ICollection<torneos_historial_clonacion> torneos_historial_clonacions { get; set; } = new List<torneos_historial_clonacion>();

    public virtual ICollection<usuarios_role> usuarios_roleid_usuarioNavigations { get; set; } = new List<usuarios_role>();

    public virtual ICollection<usuarios_role> usuarios_roleid_usuario_asignadorNavigations { get; set; } = new List<usuarios_role>();
}
