using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TorneoPro.API.Hubs
{
    [Authorize]
    public class EstadisticasHub : Hub
    {
        private readonly ILogger<EstadisticasHub> _logger;
        private static readonly Dictionary<int, HashSet<string>> _partidoConnections = new();

        public EstadisticasHub(ILogger<EstadisticasHub> logger)
        {
            _logger = logger;
        }

        public async Task SuscribirsePartido(int partidoId)
        {
            var groupName = $"partido_{partidoId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            lock (_partidoConnections)
            {
                if (!_partidoConnections.ContainsKey(partidoId))
                    _partidoConnections[partidoId] = new HashSet<string>();

                _partidoConnections[partidoId].Add(Context.ConnectionId);
            }

            _logger.LogInformation("Cliente suscrito al partido {PartidoId}", partidoId);
        }

        public async Task DesuscribirsePartido(int partidoId)
        {
            var groupName = $"partido_{partidoId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            lock (_partidoConnections)
            {
                if (_partidoConnections.ContainsKey(partidoId))
                    _partidoConnections[partidoId].Remove(Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Limpiar suscripciones
            lock (_partidoConnections)
            {
                foreach (var kvp in _partidoConnections)
                {
                    kvp.Value.Remove(Context.ConnectionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static async Task ActualizarEstadisticasPartido(IHubContext<EstadisticasHub> hubContext, int partidoId, object estadisticas)
        {
            var groupName = $"partido_{partidoId}";
            await hubContext.Clients.Group(groupName).SendAsync("EstadisticasActualizadas", estadisticas);
        }
    }
}
