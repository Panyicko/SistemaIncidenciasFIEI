# SIGPI FIEI

Sistema web académico para registrar y consultar incidencias tecnológicas de laboratorios y equipos de la FIEI-UNFV. El prototipo está construido con ASP.NET Core Razor Pages, C# y SQL Server.

## Funciones actuales

- Formulario para reportar incidencias.
- Cálculo de prioridad según impacto y urgencia.
- Registro de fecha y hora del ticket en SQL Server.
- Página para consultar incidencias registradas.

La aplicación aún está en desarrollo. La asignación de técnicos, historial de atención, cierre de tickets, autenticación institucional y despliegue multiusuario están pendientes.

## Requisitos

- .NET 10 SDK.
- SQL Server.
- Base de datos `SIGPI_FIEI`, creada con el script incluido en `BaseDatos`.

El archivo `BaseDatos/01_Crear_SIGPI_FIEI.sql` crea las tablas y catálogos iniciales que requiere la aplicación. El script no contiene los reportes, correos ni demás datos reales de tu base local. GitHub guarda este archivo como código; no aloja la instancia activa de SQL Server.

Para preparar la base en otro equipo, abre el script en SSMS y ejecútalo. Necesitas permisos para crear la base de datos `SIGPI_FIEI` y sus tablas.

## Configurar la conexión local

La configuración específica de desarrollo no se comparte. En PowerShell, desde la carpeta del repositorio, define la cadena de conexión para tu propio SQL Server:

```powershell
$env:ConnectionStrings__SIGPI = "Server=localhost;Database=SIGPI_FIEI;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
```

Reemplaza `localhost` por el nombre de la instancia SQL Server de tu equipo, si corresponde. `TrustServerCertificate=True` se usa aquí únicamente para desarrollo local; en un servidor publicado se debe configurar un certificado confiable.

## Ejecutar

Desde la carpeta que contiene `SistemaIncidenciasFIEI.slnx`:

```powershell
dotnet restore .\SistemaIncidenciasFIEI.slnx
dotnet run --project .\SistemaIncidenciasFIEI\SistemaIncidenciasFIEI.csproj
```

La aplicación abre en la dirección local indicada por Visual Studio o por el comando `dotnet run`.

## Compartir con el equipo

Cada colaborador debe clonar este repositorio, instalar los requisitos, tener acceso a una base `SIGPI_FIEI` compatible y configurar su propia cadena de conexión. No se deben subir contraseñas, archivos `appsettings.Development.json`, copias de bases de datos ni datos personales reales.
