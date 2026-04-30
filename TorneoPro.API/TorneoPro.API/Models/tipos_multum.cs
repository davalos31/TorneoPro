using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_multum
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public string? categoria { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<multa> multa { get; set; } = new List<multa>();

    public virtual ICollection<torneos_config_multa> torneos_config_multa { get; set; } = new List<torneos_config_multa>();
}
