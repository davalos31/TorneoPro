namespace TorneoPro.API.DTOs.Arbitros.Response
{
    public class ArbitroResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? FotoPerfil { get; set; }
        public string? Especialidad { get; set; }
        public int? AniosExperiencia { get; set; }
        public string? Categoria { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public List<PartidoAsignadoResponse> PartidosAsignados { get; set; } = new();
        public EstadisticasArbitroResponse? Estadisticas { get; set; }
    }

    public class PartidoAsignadoResponse
    {
        public int IdPartido { get; set; }
        public string Partido { get; set; } = string.Empty;
        public string Local { get; set; } = string.Empty;
        public string Visitante { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; } = string.Empty; // PRINCIPAL, ASISTENTE1, ASISTENTE2, CUARTO
        public string Estado { get; set; } = string.Empty;
    }

    public class EstadisticasArbitroResponse
    {
        public int TotalPartidos { get; set; }
        public int PartidosComoPrincipal { get; set; }
        public int PartidosComoAsistente { get; set; }
        public int TotalTarjetasAmarillas { get; set; }
        public int TotalTarjetasRojas { get; set; }
        public double PromedioTarjetasPorPartido { get; set; }
        public double? PromedioCalificacion { get; set; }  // ← AGREGAR ESTA LÍNEA
    }
}
