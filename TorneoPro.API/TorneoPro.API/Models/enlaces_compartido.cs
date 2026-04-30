using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class enlaces_compartido
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string codigo_enlace { get; set; } = null!;

    public int id_tipo_enlace { get; set; }

    public int id_usuario_creador { get; set; }

    public int? id_torneo { get; set; }

    public int? id_equipo { get; set; }

    public int id_rol_asignado { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_expiracion { get; set; }

    public int? max_usos { get; set; }

    public int? usos_actuales { get; set; }

    public string? estado { get; set; }

    public DateTime? fecha_desactivacion { get; set; }

    public string? motivo_desactivacion { get; set; }

    public string? metadata { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<enlaces_historial_uso> enlaces_historial_usos { get; set; } = new List<enlaces_historial_uso>();

    public virtual equipo? id_equipoNavigation { get; set; }

    public virtual tipos_rol id_rol_asignadoNavigation { get; set; } = null!;

    public virtual tipos_enlace id_tipo_enlaceNavigation { get; set; } = null!;

    public virtual torneo? id_torneoNavigation { get; set; }

    public virtual usuario id_usuario_creadorNavigation { get; set; } = null!;

    public virtual ICollection<usuarios_role> usuarios_roles { get; set; } = new List<usuarios_role>();
}
