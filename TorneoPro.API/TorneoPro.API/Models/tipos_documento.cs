using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_documento
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public bool? requiere_foto { get; set; }

    public string? patron_validacion { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<usuario> usuarios { get; set; } = new List<usuario>();
}
