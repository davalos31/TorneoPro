namespace TorneoPro.API.DTOs.Equipo.Request
{
    public class SolicitarUnionRequest
    {
        public string? Mensaje { get; set; }
    }
    public class ProcesarSolicitudUnionRequest
    {
        public bool Aprobar { get; set; }
        public string? Comentario { get; set; }
    }
}
