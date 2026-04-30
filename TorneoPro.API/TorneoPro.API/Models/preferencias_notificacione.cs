using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class preferencias_notificacione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_usuario { get; set; }

    public int id_tipo_notificacion { get; set; }

    public bool? activado { get; set; }

    public bool? push_activado { get; set; }

    public bool? email_activado { get; set; }

    public bool? sms_activado { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public virtual tipos_notificacion id_tipo_notificacionNavigation { get; set; } = null!;

    public virtual usuario id_usuarioNavigation { get; set; } = null!;
}
