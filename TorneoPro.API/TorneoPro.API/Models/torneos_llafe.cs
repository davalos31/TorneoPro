using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class torneos_llafe
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public int id_torneo { get; set; }

    public int id_torneo_fase { get; set; }

    public int numero_llave { get; set; }

    public int? id_equipo_superior { get; set; }

    public int? id_equipo_inferior { get; set; }

    public bool? tiene_bye { get; set; }

    public int? id_equipo_con_bye { get; set; }

    public int? id_partido { get; set; }

    public int? id_llave_siguiente { get; set; }

    public string? posicion_siguiente { get; set; }

    public int? id_equipo_ganador { get; set; }

    public DateTime? fecha_definicion { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<torneos_llafe> Inverseid_llave_siguienteNavigation { get; set; } = new List<torneos_llafe>();

    public virtual equipo? id_equipo_con_byeNavigation { get; set; }

    public virtual equipo? id_equipo_ganadorNavigation { get; set; }

    public virtual equipo? id_equipo_inferiorNavigation { get; set; }

    public virtual equipo? id_equipo_superiorNavigation { get; set; }

    public virtual torneos_llafe? id_llave_siguienteNavigation { get; set; }

    public virtual partido? id_partidoNavigation { get; set; }

    public virtual torneo id_torneoNavigation { get; set; } = null!;

    public virtual torneos_fase id_torneo_faseNavigation { get; set; } = null!;

    public virtual ICollection<partido> partidos { get; set; } = new List<partido>();
}
