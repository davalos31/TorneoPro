using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class ia_recomendacione
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_usuario { get; set; }

    public string tipo { get; set; } = null!;

    public decimal? peso_kg { get; set; }

    public decimal? altura_cm { get; set; }

    public int? edad { get; set; }

    public string? objetivo { get; set; }

    public string? nivel_actividad { get; set; }

    public string? contenido_json { get; set; }

    public string? pdf_url { get; set; }

    public DateTime? fecha_generacion { get; set; }

    public string? modelo_ia { get; set; }

    public string? metadata { get; set; }

    public virtual usuario id_usuarioNavigation { get; set; } = null!;
}
