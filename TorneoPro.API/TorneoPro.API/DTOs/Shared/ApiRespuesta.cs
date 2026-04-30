namespace TorneoPro.API.DTOs.Shared
{
    public class ApiRespuesta<T>
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; }
        public List<string> Errores { get; set; } = new();

        public static ApiRespuesta<T> Success(T data, string mensaje = "Operación exitosa")
        {
            return new ApiRespuesta<T>
            {
                Exitoso = true,
                Mensaje = mensaje,
                Datos = data
            };
        }

        public static ApiRespuesta<T> Error(string mensaje, List<string>? errores = null)
        {
            return new ApiRespuesta<T>
            {
                Exitoso = false,
                Mensaje = mensaje,
                Errores = errores ?? new List<string>()
            };
        }
    }
}