using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class penales_tandum
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_partido { get; set; }

    public int id_equipo_ganador { get; set; }

    public int goles_equipo_local { get; set; }

    public int goles_equipo_visitante { get; set; }

    public bool? hubo_muerte_subita { get; set; }

    public int numero_rondas { get; set; }

    public DateTime? fecha_registro { get; set; }

    public string? metadata { get; set; }

    public virtual equipo id_equipo_ganadorNavigation { get; set; } = null!;

    public virtual partido id_partidoNavigation { get; set; } = null!;

    public virtual ICollection<penales_detalle> penales_detalles { get; set; } = new List<penales_detalle>();
}
