namespace TorneoPro.API.DTOs.Arbitro.Response
{
    public class DisponibilidadArbitroResponse
    {
        public DateTime FechaHora { get; set; }
        public bool Disponible { get; set; }
        public string? MotivoNoDisponibilidad { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
