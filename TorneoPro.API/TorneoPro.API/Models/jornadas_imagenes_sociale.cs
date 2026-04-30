using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class jornadas_imagenes_sociale
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int jornada { get; set; }

    public string? imagen_url { get; set; }

    public string? opciones_diseno_json { get; set; }

    public DateTime? fecha_generacion { get; set; }

    public int? id_usuario_generacion { get; set; }

    public string? metadata { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_generacionNavigation { get; set; }
}
