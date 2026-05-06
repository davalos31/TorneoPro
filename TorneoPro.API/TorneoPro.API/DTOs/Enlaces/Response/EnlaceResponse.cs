namespace TorneoPro.API.DTOs.Enlaces.Response
{
    public class EnlaceResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string CodigoEnlace { get; set; } = string.Empty;
        public string UrlCompleta { get; set; } = string.Empty;
        public int IdTipoEnlace { get; set; }
        public string TipoEnlace { get; set; } = string.Empty;
        public int IdUsuarioCreador { get; set; }
        public string UsuarioCreador { get; set; } = string.Empty;
        public int IdRolAsignado { get; set; }
        public string RolAsignado { get; set; } = string.Empty;
        public int? IdTorneo { get; set; }
        public string? Torneo { get; set; }
        public int? IdEquipo { get; set; }
        public string? Equipo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaExpiracion { get; set; }
        public int? MaxUsos { get; set; }
        public int UsosActuales { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool EstaExpirado { get; set; }
        public bool EstaAgotado { get; set; }
    }
}