namespace TorneoPro.API.DTOs.Canchas.Response
{
    public class DisponibilidadHorariaResponse
    {
        public DateTime HoraInicio { get; set; }
        public DateTime HoraFin { get; set; }
        public bool Disponible { get; set; }
        public int? IdPartido { get; set; }
    }
}
