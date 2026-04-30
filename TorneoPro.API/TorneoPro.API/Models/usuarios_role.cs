using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class usuarios_role
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_usuario { get; set; }

    public int id_rol { get; set; }

    public int? id_torneo { get; set; }

    public int? id_equipo { get; set; }

    public DateTime? fecha_asignacion { get; set; }

    public DateOnly fecha_inicio { get; set; }

    public DateOnly? fecha_fin { get; set; }

    public string? estado { get; set; }

    public int? id_usuario_asignador { get; set; }

    public string? origen_asignacion { get; set; }

    public int? id_enlace_origen { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public string? motivo_cambio { get; set; }

    public string? metadata { get; set; }

    public bool? activo { get; set; }

    public virtual enlaces_compartido? id_enlace_origenNavigation { get; set; }

    public virtual equipo? id_equipoNavigation { get; set; }

    public virtual tipos_rol id_rolNavigation { get; set; } = null!;

    public virtual torneo? id_torneoNavigation { get; set; }

    public virtual usuario id_usuarioNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_asignadorNavigation { get; set; }
}
