using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class auditoria_acceso
{
    public int id { get; set; }

    public int? id_usuario { get; set; }

    public string accion { get; set; } = null!;

    public string? tabla_afectada { get; set; }

    public int? id_registro { get; set; }

    public string? datos_anteriores { get; set; }

    public string? datos_nuevos { get; set; }

    public string? ip_address { get; set; }

    public string? user_agent { get; set; }

    public DateTime? fecha_accion { get; set; }

    public bool? exitoso { get; set; }

    public string? detalle_error { get; set; }

    public virtual usuario? id_usuarioNavigation { get; set; }
}
