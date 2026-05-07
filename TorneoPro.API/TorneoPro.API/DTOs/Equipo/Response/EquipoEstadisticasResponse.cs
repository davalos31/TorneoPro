namespace TorneoPro.API.DTOs.Equipo.Response
{
    public class EquipoEstadisticasResponse
    {
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public int TotalPartidos { get; set; }
        public int PartidosGanados { get; set; }
        public int PartidosEmpatados { get; set; }
        public int PartidosPerdidos { get; set; }
        public int GolesFavor { get; set; }
        public int GolesContra { get; set; }
        public int DiferenciaGoles { get; set; }
        public double PromedioGolesFavor { get; set; }
        public double PromedioGolesContra { get; set; }
        public int RachaVictorias { get; set; }
        public int RachaDerrotas { get; set; }
        public int TotalTarjetasAmarillas { get; set; }
        public int TotalTarjetasRojas { get; set; }
        public int MejorRachaInvicto { get; set; }
        public GoleadorEquipoResponse? MaximoGoleador { get; set; }
        public List<ResultadoRecienteResponse> ResultadosRecientes { get; set; } = new();
    }

    public class GoleadorEquipoResponse
    {
        public int IdJugador { get; set; }
        public string Jugador { get; set; } = string.Empty;
        public int Goles { get; set; }
    }

    public class ResultadoRecienteResponse
    {
        public int IdPartido { get; set; }
        public string Rival { get; set; } = string.Empty;
        public int GolesFavor { get; set; }
        public int GolesContra { get; set; }
        public string Resultado { get; set; } = string.Empty; // GANADO, EMPATADO, PERDIDO
        public DateTime FechaHora { get; set; }
        public bool EsLocal { get; set; }
    }



    public class PartidoEquipoResponse
    {
        public int IdPartido { get; set; }
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public string Rival { get; set; } = string.Empty;
        public bool EsLocal { get; set; }
        public string Cancha { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? GolesEquipo { get; set; }
        public int? GolesRival { get; set; }
    }

// DTOs/Equipos/SolicitudUnionResponse.cs

    public class SolicitudUnionResponse
    {
        public int Id { get; set; }
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public int IdJugador { get; set; }
        public string Jugador { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
        public string? Mensaje { get; set; }
    }



    public class JugadorEstadisticaEquipoResponse
    {
        public int IdJugador { get; set; }
        public string Jugador { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public int NumeroCamiseta { get; set; }
        public string Posicion { get; set; } = string.Empty;
        public bool EsCapitan { get; set; }
        public int PartidosJugados { get; set; }
        public int Goles { get; set; }
        public int Asistencias { get; set; }
        public int TarjetasAmarillas { get; set; }
        public int TarjetasRojas { get; set; }
        public int MinutosJugados { get; set; }
        public double PromedioCalificacion { get; set; }
    }

    public class InvitacionEquipoResponse
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string EmailDestino { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool FueUtilizada { get; set; }
    }
}
