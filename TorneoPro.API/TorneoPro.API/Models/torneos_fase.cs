using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_fase
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int id_fase_catalogo { get; set; }

    public string nombre { get; set; } = null!;

    public int orden { get; set; }

    public string? letra_grupo { get; set; }

    public string? nombre_grupo { get; set; }

    public DateOnly? fecha_inicio { get; set; }

    public DateOnly? fecha_fin { get; set; }

    public string? estado { get; set; }

    public bool? esta_activa { get; set; }

    public string? metadata { get; set; }

    public bool? activo { get; set; }

    public virtual ICollection<equipos_torneo> equipos_torneoid_fase_byeNavigations { get; set; } = new List<equipos_torneo>();

    public virtual ICollection<equipos_torneo> equipos_torneoid_torneo_faseNavigations { get; set; } = new List<equipos_torneo>();

    public virtual ICollection<estadisticas_equipos_torneo> estadisticas_equipos_torneos { get; set; } = new List<estadisticas_equipos_torneo>();

    public virtual fases_torneo_catalogo id_fase_catalogoNavigation { get; set; } = null!;

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual ICollection<partido> partidos { get; set; } = new List<partido>();

    public virtual ICollection<torneos_llafe> torneos_llaves { get; set; } = new List<torneos_llafe>();

    public virtual ICollection<torneos_sembrado> torneos_sembrados { get; set; } = new List<torneos_sembrado>();
}
