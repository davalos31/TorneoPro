namespace TorneoPro.API.DTOs.Canchas.Response
{
    public class DisponibilidadResponse
    {
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public bool Disponible { get; set; }
        public int? IdPartido { get; set; }
        public string? MotivoBloqueo { get; set; }
    }
}