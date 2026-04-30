namespace TorneoPro.API.DTOs.Usuarios.Response
{
    public class RolResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int NivelJerarquia { get; set; }
        public int? IdTorneo { get; set; }
        public string? NombreTorneo { get; set; }
        public int? IdEquipo { get; set; }
        public string? NombreEquipo { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}