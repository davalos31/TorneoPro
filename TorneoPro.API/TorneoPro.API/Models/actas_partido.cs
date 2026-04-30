using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class actas_partido
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public string? numero_acta { get; set; }

    public DateTime? fecha_generacion { get; set; }

    public int? id_arbitro_firmante { get; set; }

    public string? firma_arbitro_url { get; set; }

    public DateTime? fecha_firma { get; set; }

    public string? contenido_json { get; set; }

    public string? pdf_url { get; set; }

    public bool? pdf_generado { get; set; }

    public DateTime? fecha_pdf { get; set; }

    public string? estado { get; set; }

    public string? observaciones_finales { get; set; }

    public int? id_usuario_creacion { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public string? metadata { get; set; }

    public virtual usuario? id_arbitro_firmanteNavigation { get; set; }

    public virtual partido id_partidoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_creacionNavigation { get; set; }
}
