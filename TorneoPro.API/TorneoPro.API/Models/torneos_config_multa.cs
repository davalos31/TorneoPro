using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_config_multa
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int id_tipo_multa { get; set; }

    public decimal monto { get; set; }

    public string? moneda { get; set; }

    public string? descripcion { get; set; }

    public bool? aplica_automaticamente { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public bool? activo { get; set; }

    public virtual tipos_multum id_tipo_multaNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;
}
