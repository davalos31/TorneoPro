using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class credenciales_jugadore
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_jugador { get; set; }

    public int id_equipo { get; set; }

    public int id_torneo { get; set; }

    public string? numero_credencial { get; set; }

    public string? qr_code_url { get; set; }

    public string? pdf_url { get; set; }

    public string? estado { get; set; }

    public DateTime? fecha_emision { get; set; }

    public DateOnly? fecha_vencimiento { get; set; }

    public int? id_usuario_generacion { get; set; }

    public DateTime? fecha_generacion { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual usuario id_jugadorNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual usuario? id_usuario_generacionNavigation { get; set; }
}
