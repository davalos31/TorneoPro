using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TorneoPro.API.DTOs.Shared;

namespace TorneoPro.API.Helpers
{
    public static class PaginacionHelper
    {
        public static ResultadoPaginado<T> Paginar<T>(
            IQueryable<T> query,
            int pagina,
            int tamanoPagina,
            string? ordenarPor = null,
            bool ordenDescendente = false)
        {
            // Validar parámetros
            pagina = pagina < 1 ? 1 : pagina;
            tamanoPagina = tamanoPagina < 1 ? 10 : (tamanoPagina > 100 ? 100 : tamanoPagina);

            var totalItems = query.Count();

            // Aplicar ordenamiento
            if (!string.IsNullOrEmpty(ordenarPor))
            {
                query = AplicarOrdenamiento(query, ordenarPor, ordenDescendente);
            }

            var items = query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToList();

            return ResultadoPaginado<T>.Crear(items, totalItems, pagina, tamanoPagina);
        }

        public static ResultadoPaginado<T> PaginarLista<T>(
            List<T> lista,
            int pagina,
            int tamanoPagina,
            Func<T, object>? ordenarPor = null,
            bool ordenDescendente = false)
        {
            pagina = pagina < 1 ? 1 : pagina;
            tamanoPagina = tamanoPagina < 1 ? 10 : (tamanoPagina > 100 ? 100 : tamanoPagina);

            var totalItems = lista.Count;
            IEnumerable<T> query = lista;

            if (ordenarPor != null)
            {
                query = ordenDescendente
                    ? lista.OrderByDescending(ordenarPor)
                    : lista.OrderBy(ordenarPor);
            }

            var items = query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToList();

            return ResultadoPaginado<T>.Crear(items, totalItems, pagina, tamanoPagina);
        }

        private static IQueryable<T> AplicarOrdenamiento<T>(
            IQueryable<T> query,
            string propiedad,
            bool descendente)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.PropertyOrField(param, propiedad);
            var lambda = Expression.Lambda(property, param);

            string metodo = descendente ? "OrderByDescending" : "OrderBy";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                metodo,
                new Type[] { typeof(T), property.Type },
                query.Expression,
                Expression.Quote(lambda)
            );

            return query.Provider.CreateQuery<T>(resultExpression);
        }

        public static (int Pagina, int TamanoPagina, int TotalPaginas) CalcularMetadatos(
            int totalItems,
            int pagina,
            int tamanoPagina)
        {
            pagina = pagina < 1 ? 1 : pagina;
            tamanoPagina = tamanoPagina < 1 ? 10 : (tamanoPagina > 100 ? 100 : tamanoPagina);

            var totalPaginas = (int)Math.Ceiling(totalItems / (double)tamanoPagina);

            return (pagina, tamanoPagina, totalPaginas);
        }

        public static bool TienePaginaAnterior(int pagina)
        {
            return pagina > 1;
        }

        public static bool TienePaginaSiguiente(int pagina, int totalPaginas)
        {
            return pagina < totalPaginas;
        }

        public static int ObtenerSkip(int pagina, int tamanoPagina)
        {
            pagina = pagina < 1 ? 1 : pagina;
            tamanoPagina = tamanoPagina < 1 ? 10 : tamanoPagina;

            return (pagina - 1) * tamanoPagina;
        }

        public static (int Desde, int Hasta) ObtenerRangoPagina(
            int pagina,
            int tamanoPagina,
            int totalItems)
        {
            var desde = (pagina - 1) * tamanoPagina + 1;
            var hasta = Math.Min(pagina * tamanoPagina, totalItems);

            return (desde, hasta);
        }
    }
}