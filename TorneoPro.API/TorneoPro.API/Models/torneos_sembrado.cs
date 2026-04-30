using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_sembrado
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int id_torneo_fase { get; set; }

    public int id_equipo { get; set; }

    public int posicion_sembrado { get; set; }

    public string? criterio_base { get; set; }

    public decimal? puntos_base { get; set; }

    public DateTime? fecha_registro { get; set; }

    public virtual equipo id_equipoNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual torneos_fase id_torneo_faseNavigation { get; set; } = null!;
}
