using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_notificacion
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public string? categoria { get; set; }

    public string? prioridad { get; set; }

    public bool? permite_configuracion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<notificacione> notificaciones { get; set; } = new List<notificacione>();

    public virtual ICollection<preferencias_notificacione> preferencias_notificaciones { get; set; } = new List<preferencias_notificacione>();
}
