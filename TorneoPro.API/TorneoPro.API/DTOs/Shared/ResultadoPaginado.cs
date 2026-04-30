namespace TorneoPro.API.DTOs.Shared
{
    public class ResultadoPaginado<T>
    {
        public List<T> Items { get; set; } = new();
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalItems { get; set; }
        public int TotalPaginas { get; set; }
        public bool TienePaginaAnterior => Pagina > 1;
        public bool TienePaginaSiguiente => Pagina < TotalPaginas;

        public static ResultadoPaginado<T> Crear(List<T> items, int totalItems, int pagina, int tamanoPagina)
        {
            return new ResultadoPaginado<T>
            {
                Items = items,
                TotalItems = totalItems,
                Pagina = pagina,
                TamanoPagina = tamanoPagina,
                TotalPaginas = (int)Math.Ceiling(totalItems / (double)tamanoPagina)
            };
        }
    }
}