using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class tipos_rol
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public int nivel_jerarquia { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<enlaces_compartido> enlaces_compartidos { get; set; } = new List<enlaces_compartido>();

    public virtual ICollection<enlaces_historial_uso> enlaces_historial_usoid_rol_anteriorNavigations { get; set; } = new List<enlaces_historial_uso>();

    public virtual ICollection<enlaces_historial_uso> enlaces_historial_usoid_rol_nuevoNavigations { get; set; } = new List<enlaces_historial_uso>();

    public virtual ICollection<usuarios_role> usuarios_roles { get; set; } = new List<usuarios_role>();
}
