namespace TorneoPro.API.DTOs.Jugadores.Response
{
    public class JugadorResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? FotoPerfil { get; set; }
        public string? PosicionPreferida { get; set; }
        public int? NumeroCamisetaPreferido { get; set; }
        public decimal? PesoKg { get; set; }
        public decimal? AlturaCm { get; set; }
        public bool Activo { get; set; }
        public bool EmailVerificado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public List<EquipoJugadorResponse> Equipos { get; set; } = new();
        public EstadisticasJugadorResumen? Estadisticas { get; set; }
    }

    public class EquipoJugadorResponse
    {
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public string? EscudoUrl { get; set; }
        public int? NumeroCamiseta { get; set; }
        public string? Posicion { get; set; }
        public bool EsCapitan { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
    }

    public class EstadisticasJugadorResumen
    {
        public int TotalPartidos { get; set; }
        public int TotalGoles { get; set; }
        public int TotalAsistencias { get; set; }
        public int TotalTarjetasAmarillas { get; set; }
        public int TotalTarjetasRojas { get; set; }
        public decimal PromedioGolesPorPartido { get; set; }
    }
}