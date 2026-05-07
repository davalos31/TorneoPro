using System.ComponentModel.DataAnnotations;

namespace TorneoPro.API.DTOs.Jugadores.Response
{
    public class PartidoJugadorResponse
    {
        public int IdPartido { get; set; }
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public string Local { get; set; } = string.Empty;
        public string Visitante { get; set; } = string.Empty;
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? Goles { get; set; }
        public int? Asistencias { get; set; }
        public int? TarjetasAmarillas { get; set; }
        public int? TarjetasRojas { get; set; }
        public int MinutosJugados { get; set; }
        public string? Calificacion { get; set; }
    }

    public class EstadisticasPorTorneoResponse
    {
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public int PartidosJugados { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public decimal PromedioGoles { get; set; }
        public decimal PromedioCalificacion { get; set; }
    }

    public class CompañeroEquipoResponse
    {
        public int IdJugador { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public string Posicion { get; set; } = string.Empty;
        public int? NumeroCamiseta { get; set; }
        public bool EsCapitan { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
    }

    public class SolicitarTransferenciaRequest
    {
        [Required]
        public int IdEquipoDestino { get; set; }
        public string? Motivo { get; set; }
    }

    public class SolicitudTransferenciaResponse
    {
        public int Id { get; set; }
        public int IdJugador { get; set; }
        public int IdEquipoOrigen { get; set; }
        public string EquipoOrigen { get; set; } = string.Empty;
        public int IdEquipoDestino { get; set; }
        public string EquipoDestino { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public string? Motivo { get; set; }
    }

    public class EstadisticasAvanzadasResponse
    {
        public int PartidosTitular { get; set; }
        public int PartidosSuplente { get; set; }
        public int MinutosPromedio { get; set; }
        public int GolesPorPartido { get; set; }
        public double EfectividadTiros { get; set; }
        public int PasesCompletados { get; set; }
        public double PrecisionPases { get; set; }
        public int Recuperaciones { get; set; }
        public int FaltasCometidas { get; set; }
        public int FaltasRecibidas { get; set; }
        public int ManOfTheMatch { get; set; }
    }

    public class ResumenTemporadaResponse
    {
        public int Anio { get; set; }
        public int TorneosDisputados { get; set; }
        public int PartidosJugados { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public int TitulosGanados { get; set; }
        public List<TituloResponse> Titulos { get; set; } = new();
    }

    public class TituloResponse
    {
        public string Torneo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string? Equipo { get; set; }
    }
}
