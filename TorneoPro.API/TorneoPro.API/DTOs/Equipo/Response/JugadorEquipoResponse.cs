namespace TorneoPro.API.DTOs.Equipos.Response
{
    public class JugadorEquipoResponse
    {
        public int Id { get; set; }
        public int IdJugador { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public int? NumeroCamiseta { get; set; }
        public string? Posicion { get; set; }
        public bool EsCapitan { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}