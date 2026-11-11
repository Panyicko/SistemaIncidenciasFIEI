/*
    SIGPI FIEI - esquema inicial y catálogos de referencia
    Crea las tablas que consume la aplicación Razor Pages.
    No inserta usuarios reales ni incidencias personales.
    Revisión: 2026-11-10
*/

IF DB_ID(N'SIGPI_FIEI') IS NULL
BEGIN
    CREATE DATABASE [SIGPI_FIEI];
END;
GO

USE [SIGPI_FIEI];
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Rol', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rol
    (
        IdRol INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rol PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL CONSTRAINT UQ_Rol_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario
    (
        IdUsuario INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
        Nombres NVARCHAR(200) NOT NULL,
        Apellidos NVARCHAR(200) NOT NULL,
        Correo NVARCHAR(300) NOT NULL CONSTRAINT UQ_Usuario_Correo UNIQUE,
        IdRol INT NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT (1),
        CONSTRAINT FK_Usuario_Rol FOREIGN KEY (IdRol) REFERENCES dbo.Rol(IdRol)
    );
END;
GO

IF OBJECT_ID(N'dbo.Lugar', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lugar
    (
        IdLugar INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lugar PRIMARY KEY,
        Codigo NVARCHAR(40) NOT NULL CONSTRAINT UQ_Lugar_Codigo UNIQUE,
        Nombre NVARCHAR(200) NOT NULL,
        Tipo NVARCHAR(60) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Equipo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipo
    (
        IdEquipo INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipo PRIMARY KEY,
        Codigo NVARCHAR(40) NOT NULL CONSTRAINT UQ_Equipo_Codigo UNIQUE,
        Nombre NVARCHAR(200) NOT NULL,
        Tipo NVARCHAR(60) NOT NULL,
        IdLugar INT NOT NULL,
        CONSTRAINT FK_Equipo_Lugar FOREIGN KEY (IdLugar) REFERENCES dbo.Lugar(IdLugar)
    );
END;
GO

IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categoria
    (
        IdCategoria INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categoria PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL CONSTRAINT UQ_Categoria_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Estado', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Estado
    (
        IdEstado INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Estado PRIMARY KEY,
        Nombre NVARCHAR(40) NOT NULL CONSTRAINT UQ_Estado_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Impacto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Impacto
    (
        IdImpacto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Impacto PRIMARY KEY,
        Nombre NVARCHAR(20) NOT NULL CONSTRAINT UQ_Impacto_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Urgencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Urgencia
    (
        IdUrgencia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Urgencia PRIMARY KEY,
        Nombre NVARCHAR(20) NOT NULL CONSTRAINT UQ_Urgencia_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Prioridad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Prioridad
    (
        IdPrioridad INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Prioridad PRIMARY KEY,
        Nombre NVARCHAR(20) NOT NULL CONSTRAINT UQ_Prioridad_Nombre UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.ReglaPrioridad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReglaPrioridad
    (
        IdImpacto INT NOT NULL,
        IdUrgencia INT NOT NULL,
        IdPrioridad INT NOT NULL,
        CONSTRAINT PK_ReglaPrioridad PRIMARY KEY (IdImpacto, IdUrgencia),
        CONSTRAINT FK_ReglaPrioridad_Impacto FOREIGN KEY (IdImpacto) REFERENCES dbo.Impacto(IdImpacto),
        CONSTRAINT FK_ReglaPrioridad_Urgencia FOREIGN KEY (IdUrgencia) REFERENCES dbo.Urgencia(IdUrgencia),
        CONSTRAINT FK_ReglaPrioridad_Prioridad FOREIGN KEY (IdPrioridad) REFERENCES dbo.Prioridad(IdPrioridad)
    );
END;
GO

IF OBJECT_ID(N'dbo.Incidencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Incidencia
    (
        IdIncidencia INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Incidencia PRIMARY KEY,
        Titulo NVARCHAR(120) NOT NULL,
        Descripcion NVARCHAR(1000) NOT NULL,
        IdReportante INT NOT NULL,
        IdLugar INT NOT NULL,
        IdEquipo INT NULL,
        IdCategoria INT NOT NULL,
        IdEstado INT NOT NULL,
        IdImpacto INT NOT NULL,
        IdUrgencia INT NOT NULL,
        FechaReporte DATETIME2 NOT NULL CONSTRAINT DF_Incidencia_FechaReporte DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_Incidencia_Usuario FOREIGN KEY (IdReportante) REFERENCES dbo.Usuario(IdUsuario),
        CONSTRAINT FK_Incidencia_Lugar FOREIGN KEY (IdLugar) REFERENCES dbo.Lugar(IdLugar),
        CONSTRAINT FK_Incidencia_Equipo FOREIGN KEY (IdEquipo) REFERENCES dbo.Equipo(IdEquipo),
        CONSTRAINT FK_Incidencia_Categoria FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria(IdCategoria),
        CONSTRAINT FK_Incidencia_Estado FOREIGN KEY (IdEstado) REFERENCES dbo.Estado(IdEstado),
        CONSTRAINT FK_Incidencia_Impacto FOREIGN KEY (IdImpacto) REFERENCES dbo.Impacto(IdImpacto),
        CONSTRAINT FK_Incidencia_Urgencia FOREIGN KEY (IdUrgencia) REFERENCES dbo.Urgencia(IdUrgencia)
    );
END;
GO

/* Catálogos: solo valores iniciales, sin usuarios ni tickets reales. */
INSERT INTO dbo.Rol (Nombre)
SELECT v.Nombre FROM (VALUES (N'Alumno'), (N'Coordinador'), (N'Docente'), (N'Técnico')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Rol AS r WHERE r.Nombre = v.Nombre);

INSERT INTO dbo.Categoria (Nombre)
SELECT v.Nombre FROM (VALUES
    (N'Computadora'), (N'Equipo audiovisual'), (N'Otro'),
    (N'Problema del salón'), (N'Red')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categoria AS c WHERE c.Nombre = v.Nombre);

INSERT INTO dbo.Estado (Nombre)
SELECT v.Nombre FROM (VALUES
    (N'Reportada'), (N'Asignada'), (N'En proceso'), (N'Resuelta'), (N'Cerrada')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Estado AS e WHERE e.Nombre = v.Nombre);

INSERT INTO dbo.Impacto (Nombre)
SELECT v.Nombre FROM (VALUES (N'Bajo'), (N'Medio'), (N'Alto')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Impacto AS i WHERE i.Nombre = v.Nombre);

INSERT INTO dbo.Urgencia (Nombre)
SELECT v.Nombre FROM (VALUES (N'Baja'), (N'Media'), (N'Alta')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Urgencia AS u WHERE u.Nombre = v.Nombre);

INSERT INTO dbo.Prioridad (Nombre)
SELECT v.Nombre FROM (VALUES (N'Baja'), (N'Media'), (N'Alta'), (N'Crítica')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Prioridad AS p WHERE p.Nombre = v.Nombre);

INSERT INTO dbo.ReglaPrioridad (IdImpacto, IdUrgencia, IdPrioridad)
SELECT i.IdImpacto, u.IdUrgencia, p.IdPrioridad
FROM (VALUES
    (N'Bajo', N'Baja', N'Baja'), (N'Bajo', N'Media', N'Baja'), (N'Bajo', N'Alta', N'Media'),
    (N'Medio', N'Baja', N'Baja'), (N'Medio', N'Media', N'Media'), (N'Medio', N'Alta', N'Alta'),
    (N'Alto', N'Baja', N'Media'), (N'Alto', N'Media', N'Alta'), (N'Alto', N'Alta', N'Crítica')
) AS v(Impacto, Urgencia, Prioridad)
JOIN dbo.Impacto AS i ON i.Nombre = v.Impacto
JOIN dbo.Urgencia AS u ON u.Nombre = v.Urgencia
JOIN dbo.Prioridad AS p ON p.Nombre = v.Prioridad
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ReglaPrioridad AS rp
    WHERE rp.IdImpacto = i.IdImpacto AND rp.IdUrgencia = u.IdUrgencia
);
GO

PRINT N'Esquema y catálogos iniciales de SIGPI_FIEI listos.';
GO
