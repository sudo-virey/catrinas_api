-- ========================================
-- SCRIPT COMPLETO DE BASE DE DATOS CATRINAS
-- Generado para producción
-- Fecha: 30 de octubre de 2025
-- ========================================

USE master;
GO

-- Crear la base de datos si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CATRINAS')
BEGIN
    CREATE DATABASE [CATRINAS];
    PRINT 'Base de datos CATRINAS creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La base de datos CATRINAS ya existe.';
END
GO

-- Usar la base de datos
USE [CATRINAS];
GO

-- Configurar opciones de la base de datos
IF SERVERPROPERTY('EngineEdition') <> 5
BEGIN
    ALTER DATABASE [CATRINAS] SET READ_COMMITTED_SNAPSHOT ON;
END;
GO

-- ========================================
-- CREAR TABLAS PRINCIPALES
-- ========================================

-- Tabla de historial de migraciones
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Tabla __EFMigrationsHistory creada.';
END
GO

-- Tabla Accesos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Accesos')
BEGIN
    CREATE TABLE [Accesos] (
        [Id_Acceso] int NOT NULL IDENTITY,
        [Acceso] nvarchar(6) NOT NULL,
        CONSTRAINT [PK_Accesos] PRIMARY KEY ([Id_Acceso])
    );
    PRINT 'Tabla Accesos creada.';
END
GO

-- Tabla Ajustes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ajustes')
BEGIN
    CREATE TABLE [Ajustes] (
        [Id_Ajuste] int NOT NULL IDENTITY,
        [Tiempo_de_Votacion] int NOT NULL,
        [Publicacion_Resultados] bit NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_Ajustes] PRIMARY KEY ([Id_Ajuste])
    );
    PRINT 'Tabla Ajustes creada.';
END
GO

-- Tabla Estados
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Estados')
BEGIN
    CREATE TABLE [Estados] (
        [Id_Estado] int NOT NULL IDENTITY,
        [Estado] nvarchar(50) NOT NULL,
        [Descripcion] nvarchar(200) NULL,
        CONSTRAINT [PK_Estados] PRIMARY KEY ([Id_Estado])
    );
    PRINT 'Tabla Estados creada.';
END
GO

-- Tabla HistorialAccesos
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HistorialAccesos')
BEGIN
    CREATE TABLE [HistorialAccesos] (
        [Id_Historial_Acceso] int NOT NULL IDENTITY,
        [Id_Acceso] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_HistorialAccesos] PRIMARY KEY ([Id_Historial_Acceso]),
        CONSTRAINT [FK_HistorialAccesos_Accesos_Id_Acceso] FOREIGN KEY ([Id_Acceso]) REFERENCES [Accesos] ([Id_Acceso]) ON DELETE CASCADE
    );
    PRINT 'Tabla HistorialAccesos creada.';
END
GO

-- Tabla Participantes
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Participantes')
BEGIN
    CREATE TABLE [Participantes] (
        [Id_Participante] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Id_Estado] int NOT NULL,
        [Activo] bit NOT NULL,
        [Orden] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Participantes] PRIMARY KEY ([Id_Participante]),
        CONSTRAINT [FK_Participantes_Estados_Id_Estado] FOREIGN KEY ([Id_Estado]) REFERENCES [Estados] ([Id_Estado]) ON DELETE NO ACTION
    );
    PRINT 'Tabla Participantes creada.';
END
GO

-- Tabla Evaluaciones
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Evaluaciones')
BEGIN
    CREATE TABLE [Evaluaciones] (
        [Id_Evaluacion] int NOT NULL IDENTITY,
        [Id_Participante] int NOT NULL,
        [Id_Acceso] int NOT NULL,
        [Atuendo] int NOT NULL,
        [Maquillaje] int NOT NULL,
        [Tradiciones] int NOT NULL,
        [Pasarela] int NOT NULL,
        [Interaccion] int NOT NULL,
        [Total] decimal(5,2) NOT NULL,
        [Activo] bit NOT NULL,
        [FechaEvaluacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Evaluaciones] PRIMARY KEY ([Id_Evaluacion]),
        CONSTRAINT [FK_Evaluaciones_Accesos_Id_Acceso] FOREIGN KEY ([Id_Acceso]) REFERENCES [Accesos] ([Id_Acceso]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Evaluaciones_Participantes_Id_Participante] FOREIGN KEY ([Id_Participante]) REFERENCES [Participantes] ([Id_Participante]) ON DELETE NO ACTION
    );
    PRINT 'Tabla Evaluaciones creada.';
END
GO

-- Tabla Rankings
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Rankings')
BEGIN
    CREATE TABLE [Rankings] (
        [Id_Ranking] int NOT NULL IDENTITY,
        [Id_Participante] int NOT NULL,
        [Puntos] decimal(10,2) NOT NULL,
        [FechaActualizacion] datetime2 NOT NULL,
        [Observaciones] nvarchar(500) NULL,
        [TotalAtuendo] int NOT NULL DEFAULT 0,
        [TotalInteraccion] int NOT NULL DEFAULT 0,
        [TotalMaquillaje] int NOT NULL DEFAULT 0,
        [TotalPasarela] int NOT NULL DEFAULT 0,
        [TotalTradiciones] int NOT NULL DEFAULT 0,
        [PuntosDesempate] decimal(5,2) NOT NULL DEFAULT 0.0,
        CONSTRAINT [PK_Rankings] PRIMARY KEY ([Id_Ranking]),
        CONSTRAINT [FK_Rankings_Participantes_Id_Participante] FOREIGN KEY ([Id_Participante]) REFERENCES [Participantes] ([Id_Participante]) ON DELETE CASCADE
    );
    PRINT 'Tabla Rankings creada.';
END
GO

-- Tabla UsuariosAdmin
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UsuariosAdmin')
BEGIN
    CREATE TABLE [UsuariosAdmin] (
        [Id_Usuario] int NOT NULL IDENTITY,
        [Usuario] nvarchar(50) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [Email] nvarchar(100) NULL,
        [NombreCompleto] nvarchar(100) NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [Activo] bit NOT NULL,
        [Rol] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_UsuariosAdmin] PRIMARY KEY ([Id_Usuario])
    );
    PRINT 'Tabla UsuariosAdmin creada.';
END
GO

-- ========================================
-- CREAR ÍNDICES
-- ========================================

-- Índices para Accesos
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Accesos_Acceso')
BEGIN
    CREATE UNIQUE INDEX [IX_Accesos_Acceso] ON [Accesos] ([Acceso]);
    PRINT 'Índice IX_Accesos_Acceso creado.';
END
GO

-- Índices para Estados
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Estados_Estado')
BEGIN
    CREATE UNIQUE INDEX [IX_Estados_Estado] ON [Estados] ([Estado]);
    PRINT 'Índice IX_Estados_Estado creado.';
END
GO

-- Índices para Evaluaciones
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Evaluaciones_Id_Acceso')
BEGIN
    CREATE INDEX [IX_Evaluaciones_Id_Acceso] ON [Evaluaciones] ([Id_Acceso]);
    PRINT 'Índice IX_Evaluaciones_Id_Acceso creado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Evaluaciones_Id_Participante_Id_Acceso')
BEGIN
    CREATE UNIQUE INDEX [IX_Evaluaciones_Id_Participante_Id_Acceso] ON [Evaluaciones] ([Id_Participante], [Id_Acceso]);
    PRINT 'Índice IX_Evaluaciones_Id_Participante_Id_Acceso creado.';
END
GO

-- Índices para HistorialAccesos
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistorialAccesos_Id_Acceso')
BEGIN
    CREATE INDEX [IX_HistorialAccesos_Id_Acceso] ON [HistorialAccesos] ([Id_Acceso]);
    PRINT 'Índice IX_HistorialAccesos_Id_Acceso creado.';
END
GO

-- Índices para Participantes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Participantes_Id_Estado')
BEGIN
    CREATE INDEX [IX_Participantes_Id_Estado] ON [Participantes] ([Id_Estado]);
    PRINT 'Índice IX_Participantes_Id_Estado creado.';
END
GO

-- Índices para Rankings
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Rankings_Id_Participante')
BEGIN
    CREATE UNIQUE INDEX [IX_Rankings_Id_Participante] ON [Rankings] ([Id_Participante]);
    PRINT 'Índice IX_Rankings_Id_Participante creado.';
END
GO

-- Índices para UsuariosAdmin
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UsuariosAdmin_Email')
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosAdmin_Email] ON [UsuariosAdmin] ([Email]) WHERE [Email] IS NOT NULL;
    PRINT 'Índice IX_UsuariosAdmin_Email creado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UsuariosAdmin_Usuario')
BEGIN
    CREATE UNIQUE INDEX [IX_UsuariosAdmin_Usuario] ON [UsuariosAdmin] ([Usuario]);
    PRINT 'Índice IX_UsuariosAdmin_Usuario creado.';
END
GO

-- ========================================
-- INSERTAR DATOS INICIALES
-- ========================================

-- Insertar datos en Accesos
IF NOT EXISTS (SELECT * FROM [Accesos])
BEGIN
    SET IDENTITY_INSERT [Accesos] ON;
    INSERT INTO [Accesos] ([Id_Acceso], [Acceso])
    VALUES 
        (1, N'ADM001'),
        (2, N'JUE001'),
        (3, N'JUE002'),
        (4, N'JUE003');
    SET IDENTITY_INSERT [Accesos] OFF;
    PRINT 'Datos iniciales insertados en Accesos.';
END
GO

-- Insertar datos en Ajustes
IF NOT EXISTS (SELECT * FROM [Ajustes])
BEGIN
    SET IDENTITY_INSERT [Ajustes] ON;
    INSERT INTO [Ajustes] ([Id_Ajuste], [Activo], [Fecha], [Publicacion_Resultados], [Tiempo_de_Votacion])
    VALUES (1, CAST(1 AS bit), '2025-10-23T12:00:00.0000000', CAST(0 AS bit), 30);
    SET IDENTITY_INSERT [Ajustes] OFF;
    PRINT 'Datos iniciales insertados en Ajustes.';
END
GO

-- Insertar datos en Estados
IF NOT EXISTS (SELECT * FROM [Estados])
BEGIN
    SET IDENTITY_INSERT [Estados] ON;
    INSERT INTO [Estados] ([Id_Estado], [Estado], [Descripcion])
    VALUES 
        (1, N'Registrado', N'Participante registrado en el concurso'),
        (2, N'En Espera', N'Participante en espera de evaluación'),
        (3, N'En Votación', N'Participante siendo evaluado por jueces'),
        (4, N'Calificado', N'Participante ya calificado por todos los jueces'),
        (5, N'Descalificado', N'Participante descalificado del concurso'),
        (6, N'Finalista', N'Participante clasificado como finalista');
    SET IDENTITY_INSERT [Estados] OFF;
    PRINT 'Datos iniciales insertados en Estados.';
END
GO

-- Insertar usuario administrador
IF NOT EXISTS (SELECT * FROM [UsuariosAdmin])
BEGIN
    SET IDENTITY_INSERT [UsuariosAdmin] ON;
    INSERT INTO [UsuariosAdmin] ([Id_Usuario], [Activo], [Email], [FechaCreacion], [NombreCompleto], [Password], [Rol], [Usuario])
    VALUES (1, CAST(1 AS bit), N'admin@catrinas.com', '2025-10-23T00:00:00.0000000', N'Administrador del Sistema', N'admin123', N'Administrador', N'admin');
    SET IDENTITY_INSERT [UsuariosAdmin] OFF;
    PRINT 'Usuario administrador creado.';
END
GO

-- Insertar participantes de ejemplo (opcional)
IF NOT EXISTS (SELECT * FROM [Participantes])
BEGIN
    SET IDENTITY_INSERT [Participantes] ON;
    INSERT INTO [Participantes] ([Id_Participante], [Activo], [Id_Estado], [Nombre], [Orden])
    VALUES 
        (1, CAST(1 AS bit), 2, N'Ana García Martínez', 1),
        (2, CAST(1 AS bit), 2, N'Luis Rodríguez López', 2),
        (3, CAST(1 AS bit), 2, N'Carmen Flores Sánchez', 3),
        (4, CAST(1 AS bit), 2, N'Jorge Hernández Vega', 4),
        (5, CAST(1 AS bit), 2, N'María Isabel Jiménez', 5),
        (6, CAST(1 AS bit), 2, N'Carlos Eduardo Morales', 6),
        (7, CAST(1 AS bit), 2, N'Sofia Alejandra Ruiz', 7),
        (8, CAST(1 AS bit), 2, N'Ricardo Daniel Torres', 8),
        (9, CAST(1 AS bit), 1, N'Alejandra Beatriz Luna', 9),
        (10, CAST(1 AS bit), 1, N'Fernando Javier Castro', 10);
    SET IDENTITY_INSERT [Participantes] OFF;
    PRINT 'Participantes de ejemplo insertados.';
END
GO

-- ========================================
-- REGISTRAR MIGRACIONES APLICADAS
-- ========================================

-- Registrar todas las migraciones como aplicadas
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory])
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES 
        (N'20251023170343_InitialCreate', N'9.0.10'),
        (N'20251023171009_FixedSeedData', N'9.0.10'),
        (N'20251023193359_AgregarUsuariosAdminYRankingTablas', N'9.0.10'),
        (N'20251023195029_EliminarTablaUsuarioRedundante', N'9.0.10'),
        (N'20251024190025_AgregarParticipantesDeEjemplo', N'9.0.10'),
        (N'20251024192358_CambiarRangoPuntuacionA1a5', N'9.0.10'),
        (N'20251024192418_ActualizarSistemaPuntuacion', N'9.0.10'),
        (N'20251026200332_AgregarColumnaOrdenParticipantes', N'9.0.10'),
        (N'20251027151159_AgregarTotalesPorCategoria', N'9.0.10'),
        (N'20251028175358_AgregarPuntosDesempateRanking', N'9.0.10');
    PRINT 'Historial de migraciones registrado.';
END
GO

-- ========================================
-- VERIFICACIÓN FINAL
-- ========================================

PRINT '========================================';
PRINT 'RESUMEN DE LA INSTALACIÓN:';
PRINT '========================================';
PRINT 'Base de datos: CATRINAS';
PRINT 'Tablas creadas: ' + CAST((SELECT COUNT(*) FROM sys.tables WHERE name NOT LIKE 'sys%') AS VARCHAR(10));
PRINT 'Índices creados: ' + CAST((SELECT COUNT(*) FROM sys.indexes WHERE name LIKE 'IX_%') AS VARCHAR(10));
PRINT 'Registros en Accesos: ' + CAST((SELECT COUNT(*) FROM [Accesos]) AS VARCHAR(10));
PRINT 'Registros en Estados: ' + CAST((SELECT COUNT(*) FROM [Estados]) AS VARCHAR(10));
PRINT 'Registros en Participantes: ' + CAST((SELECT COUNT(*) FROM [Participantes]) AS VARCHAR(10));
PRINT 'Registros en UsuariosAdmin: ' + CAST((SELECT COUNT(*) FROM [UsuariosAdmin]) AS VARCHAR(10));
PRINT '========================================';
PRINT 'INSTALACIÓN COMPLETADA EXITOSAMENTE';
PRINT '========================================';

-- Mostrar credenciales de acceso
PRINT 'CREDENCIALES POR DEFECTO:';
PRINT 'Usuario Admin: admin';
PRINT 'Password Admin: admin123';
PRINT 'Códigos de acceso: ADM001, JUE001, JUE002, JUE003';
PRINT '========================================';

GO