using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_historial_clonacion
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo_origen { get; set; }

    public int id_torneo_clonado { get; set; }

    public int id_usuario_clonador { get; set; }

    public DateTime? fecha_clonacion { get; set; }

    public bool? clon_equipos { get; set; }

    public bool? clon_configuracion { get; set; }

    public bool? clon_multas { get; set; }

    public bool? clon_calendario { get; set; }

    public bool? cambios_nombre { get; set; }

    public string? nombre_anterior { get; set; }

    public string? nombre_nuevo { get; set; }

    public bool? cambios_formato { get; set; }

    public string? formato_anterior { get; set; }

    public string? formato_nuevo { get; set; }

    public string? notas { get; set; }

    public string? metadata { get; set; }

    public virtual torneo id_torneo_clonadoNavigation { get; set; } = null!;

    public virtual torneo id_torneo_origenNavigation { get; set; } = null!;

    public virtual usuario id_usuario_clonadorNavigation { get; set; } = null!;
}
