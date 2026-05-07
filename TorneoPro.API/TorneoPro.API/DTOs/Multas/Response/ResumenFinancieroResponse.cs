namespace TorneoPro.API.DTOs.Multas.Response
{
    public class ResumenFinancieroResponse
    {
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public decimal TotalMultas { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal TotalPendiente { get; set; }
        public decimal TotalVencido { get; set; }
        public int CantidadMultasPendientes { get; set; }
        public int CantidadMultasPagadas { get; set; }
        public int CantidadMultasVencidas { get; set; }
        public List<ResumenPorEquipo> PorEquipo { get; set; } = new();
        public List<ResumenPorTipoMulta> PorTipoMulta { get; set; } = new();
    }

    public class ResumenPorEquipo
    {
        public int IdEquipo { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public decimal TotalMultas { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public int CantidadMultas { get; set; }
    }

    public class ResumenPorTipoMulta
    {
        public int IdTipoMulta { get; set; }
        public string TipoMulta { get; set; } = string.Empty;
        public decimal TotalMonto { get; set; }
        public decimal Pagado { get; set; }
        public int Cantidad { get; set; }
    }
}