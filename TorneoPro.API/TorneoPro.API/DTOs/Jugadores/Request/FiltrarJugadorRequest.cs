using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Jugadores.Request
{
    public class FiltrarJugadorRequest : PaginacionRequest
    {
        public string? Buscar { get; set; }
        public int? IdEquipo { get; set; }
        public int? IdTorneo { get; set; }
        public string? Posicion { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public bool? SoloActivos { get; set; }
        public bool? SoloDisponibles { get; set; }
        public int? EdadMinima { get; set; }
        public int? EdadMaxima { get; set; }
    }
}