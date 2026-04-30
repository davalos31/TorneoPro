using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class metodos_pago
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? requiere_comprobante { get; set; }

    public bool? es_automatico { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<multas_historial_pago> multas_historial_pagos { get; set; } = new List<multas_historial_pago>();
}
