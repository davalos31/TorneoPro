using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class deporte
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? descripcion { get; set; }

    public int jugadores_por_equipo { get; set; }

    public bool? permite_empate { get; set; }

    public int tiempo_partido_minutos { get; set; }

    public int? tiempo_medio_tiempo_minutos { get; set; }

    public bool? permite_prorroga { get; set; }

    public int? tiempo_prorroga_minutos { get; set; }

    public bool? permite_penales { get; set; }

    public string? icono_url { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<torneo> torneos { get; set; } = new List<torneo>();
}
