using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tokens_recuperacion
{
    public int id { get; set; }

    public int id_usuario { get; set; }

    public string token { get; set; } = null!;

    public DateTime? fecha_creacion { get; set; }

    public DateTime fecha_expiracion { get; set; }

    public bool? usado { get; set; }

    public DateTime? fecha_uso { get; set; }

    public string? ip_solicitud { get; set; }

    public bool? activo { get; set; }

    public virtual usuario id_usuarioNavigation { get; set; } = null!;
}
