namespace TorneoPro.API.DTOs.Equipos.Request
{
    public class ActualizarJugadorRequest
    {
        public int? NumeroCamiseta { get; set; }
        public string? Posicion { get; set; }
        public bool? EsCapitan { get; set; }
    }
}
