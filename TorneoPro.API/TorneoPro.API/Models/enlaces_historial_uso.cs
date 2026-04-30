using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class enlaces_historial_uso
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_enlace { get; set; }

    public int id_usuario { get; set; }

    public DateTime? fecha_uso { get; set; }

    public string? ip_address { get; set; }

    public string? user_agent { get; set; }

    public int? id_rol_anterior { get; set; }

    public int id_rol_nuevo { get; set; }

    public bool? uso_exitoso { get; set; }

    public string? motivo_fallo { get; set; }

    public string? metadata { get; set; }

    public virtual enlaces_compartido id_enlaceNavigation { get; set; } = null!;

    public virtual tipos_rol? id_rol_anteriorNavigation { get; set; }

    public virtual tipos_rol id_rol_nuevoNavigation { get; set; } = null!;

    public virtual usuario id_usuarioNavigation { get; set; } = null!;
}
