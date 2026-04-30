using System;
using System.Collections.Generic;

namespace TorneoPro.API.Models;

public partial class cancha
{
    public int id { get; set; }

    public string codigo { get; set; } = null!;

    public string nombre { get; set; } = null!;

    public string? nombre_corto { get; set; }

    public int id_tipo_superficie { get; set; }

    public string? direccion { get; set; }

    public string? ciudad { get; set; }

    public string? pais { get; set; }

    public decimal? latitud { get; set; }

    public decimal? longitud { get; set; }

    public string? url_mapa { get; set; }

    public int? capacidad_espectadores { get; set; }

    public bool? tiene_iluminacion { get; set; }

    public bool? tiene_vestuarios { get; set; }

    public bool? tiene_estacionamiento { get; set; }

    public string? foto_url { get; set; }

    public string? estado { get; set; }

    public bool? activo { get; set; }

    public DateTime? fecha_creacion { get; set; }

    public DateTime? fecha_modificacion { get; set; }

    public string? metadata { get; set; }

    public virtual ICollection<canchas_disponibilidad> canchas_disponibilidads { get; set; } = new List<canchas_disponibilidad>();

    public virtual ICollection<fechas_bloqueada> fechas_bloqueada { get; set; } = new List<fechas_bloqueada>();

    public virtual tipos_superficie id_tipo_superficieNavigation { get; set; } = null!;

    public virtual ICollection<partido> partidos { get; set; } = new List<partido>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramacioneid_cancha_nuevaNavigations { get; set; } = new List<partidos_reprogramacione>();

    public virtual ICollection<partidos_reprogramacione> partidos_reprogramacioneid_cancha_originalNavigations { get; set; } = new List<partidos_reprogramacione>();
}
