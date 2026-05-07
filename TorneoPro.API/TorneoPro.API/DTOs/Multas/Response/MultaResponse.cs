namespace TorneoPro.API.DTOs.Multas.Response
{
    public class MultaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdTorneo { get; set; }
        public string Torneo { get; set; } = string.Empty;
        public int IdTipoMulta { get; set; }
        public string TipoMulta { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int? IdEquipo { get; set; }
        public string? Equipo { get; set; }
        public int? IdJugador { get; set; }
        public string? Jugador { get; set; }
        public decimal Monto { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaLimitePago { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool EstaVencida { get; set; }
    }
}