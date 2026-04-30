namespace TorneoPro.API.DTOs.Canchas.Response
{
    public class BloqueoResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdCancha { get; set; }
        public string Cancha { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public bool AplicaATodasCanchas { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}