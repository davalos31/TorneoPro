namespace TorneoPro.API.DTOs.Enlaces.Response
{
    public class EnlaceEstadisticasResponse
    {
        public int TotalEnlaces { get; set; }
        public int EnlacesActivos { get; set; }
        public int EnlacesExpirados { get; set; }
        public int EnlacesAgotados { get; set; }
        public int TotalUsos { get; set; }
        public int TotalUsosExitosos { get; set; }
        public double PromedioUsosPorEnlace { get; set; }
    }
}
