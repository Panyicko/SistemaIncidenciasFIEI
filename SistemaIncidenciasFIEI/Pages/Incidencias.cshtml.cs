using System.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace SistemaIncidenciasFIEI.Pages
{
    public class IncidenciasModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IncidenciasModel> _logger;

        public IncidenciasModel(IConfiguration configuration, ILogger<IncidenciasModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public List<IncidenciaFila> Incidencias { get; private set; } = new();
        public string? Error { get; private set; }

        public async Task OnGetAsync()
        {
            var connectionString = _configuration.GetConnectionString("SIGPI");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Error = "Falta configurar la conexión a SQL Server.";
                return;
            }

            const string sql = @"
SELECT inc.IdIncidencia,
       inc.Titulo,
       inc.Descripcion,
       u.Correo,
       l.Nombre AS Lugar,
       COALESCE(e.Nombre, e.Codigo, N'No indicado') AS Equipo,
       c.Nombre AS Categoria,
       imp.Nombre AS Impacto,
       urg.Nombre AS Urgencia,
       COALESCE(p.Nombre, N'Pendiente') AS Prioridad,
       est.Nombre AS Estado,
       inc.FechaReporte
FROM dbo.Incidencia AS inc
JOIN dbo.Usuario AS u ON u.IdUsuario = inc.IdReportante
JOIN dbo.Lugar AS l ON l.IdLugar = inc.IdLugar
LEFT JOIN dbo.Equipo AS e ON e.IdEquipo = inc.IdEquipo
JOIN dbo.Categoria AS c ON c.IdCategoria = inc.IdCategoria
JOIN dbo.Estado AS est ON est.IdEstado = inc.IdEstado
JOIN dbo.Impacto AS imp ON imp.IdImpacto = inc.IdImpacto
JOIN dbo.Urgencia AS urg ON urg.IdUrgencia = inc.IdUrgencia
LEFT JOIN dbo.ReglaPrioridad AS rp
    ON rp.IdImpacto = inc.IdImpacto AND rp.IdUrgencia = inc.IdUrgencia
LEFT JOIN dbo.Prioridad AS p ON p.IdPrioridad = rp.IdPrioridad
ORDER BY inc.IdIncidencia DESC;";

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Incidencias.Add(new IncidenciaFila
                    {
                        Id = reader.GetInt32(0),
                        Titulo = reader.GetString(1),
                        Descripcion = reader.GetString(2),
                        Correo = reader.GetString(3),
                        Lugar = reader.GetString(4),
                        Equipo = reader.GetString(5),
                        Categoria = reader.GetString(6),
                        Impacto = reader.GetString(7),
                        Urgencia = reader.GetString(8),
                        Prioridad = reader.GetString(9),
                        Estado = reader.GetString(10),
                        Fecha = reader.GetDateTime(11)
                    });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error al consultar las incidencias en SQL Server.");
                Error = "No se pudieron cargar los reportes. Comprueba la conexión y que exista la columna FechaReporte en dbo.Incidencia.";
            }
        }

        public sealed class IncidenciaFila
        {
            public int Id { get; init; }
            public string Titulo { get; init; } = "";
            public string Descripcion { get; init; } = "";
            public string Correo { get; init; } = "";
            public string Lugar { get; init; } = "";
            public string Equipo { get; init; } = "";
            public string Categoria { get; init; } = "";
            public string Impacto { get; init; } = "";
            public string Urgencia { get; init; } = "";
            public string Prioridad { get; init; } = "";
            public string Estado { get; init; } = "";
            public DateTime Fecha { get; init; }
        }
    }
}
