using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace SistemaIncidenciasFIEI.Pages
{
    public class ReportarModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportarModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public ReportarModel(IConfiguration configuration, ILogger<ReportarModel> logger, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
        }

        public string Mensaje { get; set; } = string.Empty;
        public bool EsError { get; set; }
        public List<string> Categorias { get; private set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                await using var connection = new SqlConnection(_configuration.GetConnectionString("SIGPI"));
                await connection.OpenAsync();
                await CargarCatalogosAsync(connection);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "No se pudieron cargar los catálogos del formulario.");
                EsError = true;
                Mensaje = _environment.IsDevelopment()
                    ? $"Error SQL al cargar opciones: {ex.Message}"
                    : "No se pudieron cargar los lugares y equipos desde SQL Server.";
            }
        }

        private async Task CargarCatalogosAsync(SqlConnection connection)
        {
            const string categoriasSql = "SELECT Nombre FROM dbo.Categoria ORDER BY Nombre;";
            await using (var command = new SqlCommand(categoriasSql, connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) Categorias.Add(reader.GetString(0));
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var nombres = Request.Form["nombres"].ToString().Trim();
            var apellidos = Request.Form["apellidos"].ToString().Trim();
            var correo = Request.Form["correo"].ToString().Trim();
            var titulo = Request.Form["titulo"].ToString().Trim();
            var lugar = Request.Form["lugar"].ToString().Trim();
            var tipoLugar = Request.Form["tipoLugar"].ToString().Trim();
            var equipo = Request.Form["equipo"].ToString().Trim();
            var tipoEquipo = Request.Form["tipoEquipo"].ToString().Trim();
            var categoria = Request.Form["categoria"].ToString().Trim();
            var descripcion = Request.Form["descripcion"].ToString().Trim();
            var impacto = Request.Form["impacto"].ToString().Trim();
            var urgencia = Request.Form["urgencia"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos) ||
                string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(titulo) ||
                string.IsNullOrWhiteSpace(lugar) || string.IsNullOrWhiteSpace(tipoLugar) ||
                string.IsNullOrWhiteSpace(descripcion))
            {
                EsError = true;
                Mensaje = "Completa nombres, apellidos, correo, título, lugar, tipo de lugar y descripción.";
                return Page();
            }

            if (!string.IsNullOrWhiteSpace(equipo) && string.IsNullOrWhiteSpace(tipoEquipo))
            {
                EsError = true;
                Mensaje = "Selecciona el tipo de equipo o deja el campo de equipo vacío si no corresponde.";
                return Page();
            }

            var connectionString = _configuration.GetConnectionString("SIGPI");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                EsError = true;
                Mensaje = "Falta configurar la conexión a SQL Server.";
                return Page();
            }

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await CargarCatalogosAsync(connection);
                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

                // Primero comprobamos que los catálogos existan para no registrar
                // un usuario nuevo si después el ticket no pudiera guardarse.
                const string catalogosSql = @"
SELECT N'categoría' WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Categoria WHERE Nombre = @Categoria)
UNION ALL
SELECT N'estado Reportada' WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Estado WHERE Nombre = N'Reportada')
UNION ALL
SELECT N'impacto' WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Impacto WHERE Nombre = @Impacto)
UNION ALL
SELECT N'urgencia' WHERE NOT EXISTS
    (SELECT 1 FROM dbo.Urgencia WHERE Nombre = @Urgencia);";

                var faltantes = new List<string>();
                await using (var catalogosCommand = new SqlCommand(catalogosSql, connection, transaction))
                {
                    catalogosCommand.Parameters.Add("@Lugar", SqlDbType.NVarChar, 100).Value = lugar;
                    catalogosCommand.Parameters.Add("@Equipo", SqlDbType.NVarChar, 100).Value = equipo;
                    catalogosCommand.Parameters.Add("@Categoria", SqlDbType.NVarChar, 80).Value = categoria;
                    catalogosCommand.Parameters.Add("@Impacto", SqlDbType.NVarChar, 20).Value = impacto;
                    catalogosCommand.Parameters.Add("@Urgencia", SqlDbType.NVarChar, 20).Value = urgencia;
                    await using var reader = await catalogosCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) faltantes.Add(reader.GetString(0));
                }

                if (faltantes.Count > 0)
                {
                    EsError = true;
                    Mensaje = "No se guardó. No se encontraron estos datos en los catálogos de SIGPI_FIEI: " + string.Join(", ", faltantes) + ". Revisa cómo están escritos en SQL Server.";
                    return Page();
                }

                int idLugar;
                const string buscarLugarSql = @"
SELECT IdLugar
FROM dbo.Lugar WITH (UPDLOCK, HOLDLOCK)
WHERE Nombre = @Lugar OR Codigo = @Lugar;";
                await using (var buscarLugar = new SqlCommand(buscarLugarSql, connection, transaction))
                {
                    buscarLugar.Parameters.Add("@Lugar", SqlDbType.NVarChar, 100).Value = lugar;
                    var resultadoLugar = await buscarLugar.ExecuteScalarAsync();
                    if (resultadoLugar is not null && resultadoLugar is not DBNull)
                    {
                        idLugar = Convert.ToInt32(resultadoLugar);
                    }
                    else
                    {
                        const string crearLugarSql = @"
INSERT INTO dbo.Lugar (Codigo, Nombre, Tipo)
OUTPUT INSERTED.IdLugar
VALUES (@Codigo, @Lugar, @Tipo);";
                        await using var crearLugar = new SqlCommand(crearLugarSql, connection, transaction);
                        crearLugar.Parameters.Add("@Codigo", SqlDbType.NVarChar, 20).Value = "LUG" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
                        crearLugar.Parameters.Add("@Lugar", SqlDbType.NVarChar, 100).Value = lugar;
                        crearLugar.Parameters.Add("@Tipo", SqlDbType.NVarChar, 60).Value = tipoLugar;
                        idLugar = Convert.ToInt32(await crearLugar.ExecuteScalarAsync());
                    }
                }

                int? idEquipo = null;
                if (!string.IsNullOrWhiteSpace(equipo))
                {
                    const string buscarEquipoSql = @"
SELECT IdEquipo
FROM dbo.Equipo WITH (UPDLOCK, HOLDLOCK)
WHERE IdLugar = @IdLugar AND (Nombre = @Equipo OR Codigo = @Equipo);";
                    await using var buscarEquipo = new SqlCommand(buscarEquipoSql, connection, transaction);
                    buscarEquipo.Parameters.Add("@IdLugar", SqlDbType.Int).Value = idLugar;
                    buscarEquipo.Parameters.Add("@Equipo", SqlDbType.NVarChar, 100).Value = equipo;
                    var resultadoEquipo = await buscarEquipo.ExecuteScalarAsync();
                    if (resultadoEquipo is not null && resultadoEquipo is not DBNull)
                    {
                        idEquipo = Convert.ToInt32(resultadoEquipo);
                    }
                    else
                    {
                        const string crearEquipoSql = @"
INSERT INTO dbo.Equipo (Codigo, Nombre, Tipo, IdLugar)
OUTPUT INSERTED.IdEquipo
VALUES (@Codigo, @Equipo, @Tipo, @IdLugar);";
                        await using var crearEquipo = new SqlCommand(crearEquipoSql, connection, transaction);
                        crearEquipo.Parameters.Add("@Codigo", SqlDbType.NVarChar, 20).Value = "EQP" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
                        crearEquipo.Parameters.Add("@Equipo", SqlDbType.NVarChar, 100).Value = equipo;
                        crearEquipo.Parameters.Add("@Tipo", SqlDbType.NVarChar, 60).Value = tipoEquipo;
                        crearEquipo.Parameters.Add("@IdLugar", SqlDbType.Int).Value = idLugar;
                        idEquipo = Convert.ToInt32(await crearEquipo.ExecuteScalarAsync());
                    }
                }

                int? idUsuario = null;
                const string buscarUsuarioSql = @"
SELECT IdUsuario
FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
WHERE Correo = @Correo;";
                await using (var buscarUsuario = new SqlCommand(buscarUsuarioSql, connection, transaction))
                {
                    buscarUsuario.Parameters.Add("@Correo", SqlDbType.NVarChar, 300).Value = correo;
                    var resultadoUsuario = await buscarUsuario.ExecuteScalarAsync();
                    if (resultadoUsuario is not null && resultadoUsuario is not DBNull)
                        idUsuario = Convert.ToInt32(resultadoUsuario);
                }

                if (idUsuario is null)
                {
                    if (!correo.EndsWith("@unfv.edu.pe", StringComparison.OrdinalIgnoreCase))
                    {
                        EsError = true;
                        Mensaje = "Para registrar un correo nuevo, usa tu correo institucional terminado en @unfv.edu.pe.";
                        return Page();
                    }

                    const string rolAlumnoSql = "SELECT IdRol FROM dbo.Rol WHERE Nombre = N'Alumno';";
                    int? idRolAlumno;
                    await using (var rolCommand = new SqlCommand(rolAlumnoSql, connection, transaction))
                    {
                        var resultadoRol = await rolCommand.ExecuteScalarAsync();
                        idRolAlumno = resultadoRol is null || resultadoRol is DBNull
                            ? null
                            : Convert.ToInt32(resultadoRol);
                    }

                    if (idRolAlumno is null)
                    {
                        EsError = true;
                        Mensaje = "No se encontró el rol Alumno en dbo.Rol; no se pudo registrar el correo nuevo.";
                        return Page();
                    }

                    const string registrarUsuarioSql = @"
INSERT INTO dbo.Usuario (Nombres, Apellidos, Correo, IdRol, Activo)
OUTPUT INSERTED.IdUsuario
VALUES (@Nombres, @Apellidos, @Correo, @IdRol, 1);";
                    await using var registrarUsuario = new SqlCommand(registrarUsuarioSql, connection, transaction);
                    registrarUsuario.Parameters.Add("@Nombres", SqlDbType.NVarChar, 200).Value = nombres;
                    registrarUsuario.Parameters.Add("@Apellidos", SqlDbType.NVarChar, 200).Value = apellidos;
                    registrarUsuario.Parameters.Add("@Correo", SqlDbType.NVarChar, 300).Value = correo;
                    registrarUsuario.Parameters.Add("@IdRol", SqlDbType.Int).Value = idRolAlumno.Value;
                    idUsuario = Convert.ToInt32(await registrarUsuario.ExecuteScalarAsync());
                }

                const string guardarIncidenciaSql = @"
INSERT INTO dbo.Incidencia
    (Titulo, Descripcion, IdReportante, IdLugar, IdEquipo,
     IdCategoria, IdEstado, IdImpacto, IdUrgencia, FechaReporte)
OUTPUT INSERTED.IdIncidencia
SELECT @Titulo, @Descripcion, @IdUsuario, @IdLugar, @IdEquipo,
       c.IdCategoria, s.IdEstado, i.IdImpacto, ur.IdUrgencia, GETDATE()
FROM dbo.Categoria c
JOIN dbo.Estado s ON s.Nombre = N'Reportada'
JOIN dbo.Impacto i ON i.Nombre = @Impacto
JOIN dbo.Urgencia ur ON ur.Nombre = @Urgencia
WHERE c.Nombre = @Categoria;";

                int idIncidencia;
                await using (var incidenciaCommand = new SqlCommand(guardarIncidenciaSql, connection, transaction))
                {
                    incidenciaCommand.Parameters.Add("@Titulo", SqlDbType.NVarChar, 120).Value = titulo;
                    incidenciaCommand.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = descripcion;
                    incidenciaCommand.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario.Value;
                    incidenciaCommand.Parameters.Add("@IdLugar", SqlDbType.Int).Value = idLugar;
                    incidenciaCommand.Parameters.Add("@IdEquipo", SqlDbType.Int).Value = (object?)idEquipo ?? DBNull.Value;
                    incidenciaCommand.Parameters.Add("@Categoria", SqlDbType.NVarChar, 80).Value = categoria;
                    incidenciaCommand.Parameters.Add("@Impacto", SqlDbType.NVarChar, 20).Value = impacto;
                    incidenciaCommand.Parameters.Add("@Urgencia", SqlDbType.NVarChar, 20).Value = urgencia;
                    var resultadoIncidencia = await incidenciaCommand.ExecuteScalarAsync();
                    if (resultadoIncidencia is null || resultadoIncidencia is DBNull)
                    {
                        EsError = true;
                        Mensaje = "No se guardó el ticket. Revisa el lugar y el equipo seleccionados.";
                        return Page();
                    }
                    idIncidencia = Convert.ToInt32(resultadoIncidencia);
                }

                const string prioritySql = @"
SELECT p.Nombre
FROM dbo.Incidencia inc
JOIN dbo.ReglaPrioridad rp
  ON rp.IdImpacto = inc.IdImpacto AND rp.IdUrgencia = inc.IdUrgencia
JOIN dbo.Prioridad p ON p.IdPrioridad = rp.IdPrioridad
WHERE inc.IdIncidencia = @IdIncidencia;";
                string prioridad;
                await using (var priorityCommand = new SqlCommand(prioritySql, connection, transaction))
                {
                    priorityCommand.Parameters.Add("@IdIncidencia", SqlDbType.Int).Value = idIncidencia;
                    prioridad = (await priorityCommand.ExecuteScalarAsync())?.ToString() ?? "Pendiente";
                }

                await transaction.CommitAsync();
                TempData["Mensaje"] = $"Ticket #{idIncidencia} guardado con fecha y hora. Prioridad: {prioridad}.";
                return RedirectToPage("/Incidencias");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error al guardar una incidencia en SQL Server.");
                EsError = true;
                Mensaje = _environment.IsDevelopment()
                    ? $"Error SQL {ex.Number}: {ex.Message}"
                    : "No se pudo guardar el ticket. Verifica los datos ingresados o contacta al administrador.";
            }

            return Page();
        }
    }
}


