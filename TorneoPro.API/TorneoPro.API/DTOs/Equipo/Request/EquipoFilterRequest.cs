using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class EquipoFilterRequest : PaginacionRequest
    {
        public string? Buscar { get; set; }
        public string? Ciudad { get; set; }
        public string? Estado { get; set; }
        public bool? Verificado { get; set; }
        public int? IdTorneo { get; set; }
        public int? IdJugador { get; set; }
    }
}