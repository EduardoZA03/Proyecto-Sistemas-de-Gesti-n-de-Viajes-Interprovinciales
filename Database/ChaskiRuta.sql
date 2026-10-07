/* =====================================================================
   Chaski Ruta - Sistema de Gestión de Viajes Interprovinciales
   Base de datos: ChaskiRuta (SQL Server / LocalDB)
   Coincide con las clases de WebApplication1/Models
   El script se puede ejecutar varias veces sin duplicar datos.
   ===================================================================== */

IF DB_ID(N'ChaskiRuta') IS NULL
    CREATE DATABASE ChaskiRuta;
GO

USE ChaskiRuta;
GO

/* ------------------------------- TABLAS ------------------------------ */

IF OBJECT_ID(N'dbo.Rol') IS NULL
CREATE TABLE dbo.Rol (
    IdRol        INT IDENTITY(1,1) CONSTRAINT PK_Rol PRIMARY KEY,
    NombreRol    NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Rol_Nombre UNIQUE,
    Descripcion  NVARCHAR(200) NOT NULL DEFAULT N'',
    Estado       NVARCHAR(20)  NOT NULL DEFAULT N'Activo'
);

IF OBJECT_ID(N'dbo.Usuario') IS NULL
CREATE TABLE dbo.Usuario (
    IdUsuario     INT IDENTITY(1,1) CONSTRAINT PK_Usuario PRIMARY KEY,
    Nombres       NVARCHAR(100) NOT NULL,
    Apellidos     NVARCHAR(100) NOT NULL,
    Dni           NVARCHAR(8)   NOT NULL CONSTRAINT UQ_Usuario_Dni UNIQUE,
    Correo        NVARCHAR(100) NOT NULL CONSTRAINT UQ_Usuario_Correo UNIQUE,
    Telefono      NVARCHAR(15)  NOT NULL DEFAULT N'',
    IdRol         INT NOT NULL CONSTRAINT FK_Usuario_Rol REFERENCES dbo.Rol(IdRol),
    Estado        NVARCHAR(20)  NOT NULL DEFAULT N'Activo',
    FechaCreacion DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    -- Inicio de sesión: se entra con NombreUsuario (ID) y contraseña
    NombreUsuario    NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE,
    ContrasenaHash   NVARCHAR(200) NULL,       -- la app la rellena; nunca se guarda en texto
    IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuario_Intentos DEFAULT 0,
    BloqueadoHasta   DATETIME2 NULL,
    UltimoAcceso     DATETIME2 NULL
);

IF OBJECT_ID(N'dbo.Ciudad') IS NULL
CREATE TABLE dbo.Ciudad (
    IdCiudad     INT IDENTITY(1,1) CONSTRAINT PK_Ciudad PRIMARY KEY,
    NombreCiudad NVARCHAR(100) NOT NULL,
    Departamento NVARCHAR(100) NOT NULL,
    Estado       NVARCHAR(20)  NOT NULL DEFAULT N'Activo',
    CONSTRAINT UQ_Ciudad UNIQUE (NombreCiudad, Departamento)
);

IF OBJECT_ID(N'dbo.Empresa') IS NULL
CREATE TABLE dbo.Empresa (
    IdEmpresa   INT IDENTITY(1,1) CONSTRAINT PK_Empresa PRIMARY KEY,
    RazonSocial NVARCHAR(150) NOT NULL,
    Ruc         NVARCHAR(11)  NOT NULL CONSTRAINT UQ_Empresa_Ruc UNIQUE,
    Telefono    NVARCHAR(15)  NOT NULL DEFAULT N'',
    Correo      NVARCHAR(100) NOT NULL DEFAULT N'',
    Direccion   NVARCHAR(200) NOT NULL DEFAULT N'',
    Estado      NVARCHAR(20)  NOT NULL DEFAULT N'Activo'
);

IF OBJECT_ID(N'dbo.Terminal') IS NULL
CREATE TABLE dbo.Terminal (
    IdTerminal     INT IDENTITY(1,1) CONSTRAINT PK_Terminal PRIMARY KEY,
    IdEmpresa      INT NOT NULL CONSTRAINT FK_Terminal_Empresa REFERENCES dbo.Empresa(IdEmpresa),
    NombreTerminal NVARCHAR(100) NOT NULL,
    Direccion      NVARCHAR(200) NOT NULL DEFAULT N'',
    Ciudad         NVARCHAR(100) NOT NULL,
    Estado         NVARCHAR(20)  NOT NULL DEFAULT N'Activo'
);

IF OBJECT_ID(N'dbo.Bus') IS NULL
CREATE TABLE dbo.Bus (
    IdBus             INT IDENTITY(1,1) CONSTRAINT PK_Bus PRIMARY KEY,
    IdEmpresa         INT NOT NULL CONSTRAINT FK_Bus_Empresa REFERENCES dbo.Empresa(IdEmpresa),
    Placa             NVARCHAR(10) NOT NULL CONSTRAINT UQ_Bus_Placa UNIQUE,
    Modelo            NVARCHAR(50) NOT NULL,
    Marca             NVARCHAR(50) NOT NULL,
    Anio              INT NOT NULL CONSTRAINT CK_Bus_Anio CHECK (Anio BETWEEN 1990 AND 2100),
    CapacidadAsientos INT NOT NULL CONSTRAINT CK_Bus_Capacidad CHECK (CapacidadAsientos > 0),
    Estado            NVARCHAR(20) NOT NULL DEFAULT N'Operativo',  -- Operativo, Mantenimiento
    Servicio          NVARCHAR(30) NOT NULL CONSTRAINT DF_Bus_Servicio DEFAULT N'Ejecutivo'  -- Ejecutivo, Semi Cama, Cama Suite
);

IF OBJECT_ID(N'dbo.Asiento') IS NULL
CREATE TABLE dbo.Asiento (
    IdAsiento     INT IDENTITY(1,1) CONSTRAINT PK_Asiento PRIMARY KEY,
    IdBus         INT NOT NULL CONSTRAINT FK_Asiento_Bus REFERENCES dbo.Bus(IdBus) ON DELETE CASCADE,
    NumeroAsiento NVARCHAR(10) NOT NULL,
    Piso          INT NOT NULL DEFAULT 1 CONSTRAINT CK_Asiento_Piso CHECK (Piso IN (1,2)),
    Estado        NVARCHAR(20) NOT NULL DEFAULT N'Disponible',
    CONSTRAINT UQ_Asiento_Bus_Numero UNIQUE (IdBus, NumeroAsiento)
);

IF OBJECT_ID(N'dbo.Ruta') IS NULL
CREATE TABLE dbo.Ruta (
    IdRuta           INT IDENTITY(1,1) CONSTRAINT PK_Ruta PRIMARY KEY,
    IdOrigen         INT NOT NULL CONSTRAINT FK_Ruta_Origen  REFERENCES dbo.Ciudad(IdCiudad),
    IdDestino        INT NOT NULL CONSTRAINT FK_Ruta_Destino REFERENCES dbo.Ciudad(IdCiudad),
    DistanciaKm      FLOAT NOT NULL CONSTRAINT CK_Ruta_Distancia CHECK (DistanciaKm > 0),
    DuracionEstimada NVARCHAR(20) NOT NULL,
    Estado           NVARCHAR(20) NOT NULL DEFAULT N'Activo',
    CONSTRAINT CK_Ruta_Distinta CHECK (IdOrigen <> IdDestino),
    CONSTRAINT UQ_Ruta UNIQUE (IdOrigen, IdDestino)
);

IF OBJECT_ID(N'dbo.Viaje') IS NULL
CREATE TABLE dbo.Viaje (
    IdViaje     INT IDENTITY(1,1) CONSTRAINT PK_Viaje PRIMARY KEY,
    IdRuta      INT NOT NULL CONSTRAINT FK_Viaje_Ruta    REFERENCES dbo.Ruta(IdRuta),
    IdBus       INT NOT NULL CONSTRAINT FK_Viaje_Bus     REFERENCES dbo.Bus(IdBus),
    IdOrigen    INT NOT NULL CONSTRAINT FK_Viaje_Origen  REFERENCES dbo.Ciudad(IdCiudad),
    IdDestino   INT NOT NULL CONSTRAINT FK_Viaje_Destino REFERENCES dbo.Ciudad(IdCiudad),
    FechaSalida DATE NOT NULL,
    HoraSalida  TIME(0) NOT NULL,
    PrecioBase  DECIMAL(10,2) NOT NULL CONSTRAINT CK_Viaje_Precio CHECK (PrecioBase >= 0),
    Estado      NVARCHAR(20) NOT NULL DEFAULT N'Programado'
);

IF OBJECT_ID(N'dbo.Tarifa') IS NULL
CREATE TABLE dbo.Tarifa (
    IdTarifa   INT IDENTITY(1,1) CONSTRAINT PK_Tarifa PRIMARY KEY,
    IdViaje    INT NOT NULL CONSTRAINT FK_Tarifa_Viaje REFERENCES dbo.Viaje(IdViaje) ON DELETE CASCADE,
    TipoTarifa NVARCHAR(30) NOT NULL,   -- General, Niño, Estudiante, Tercera Edad
    Precio     DECIMAL(10,2) NOT NULL CONSTRAINT CK_Tarifa_Precio CHECK (Precio >= 0),
    Estado     NVARCHAR(20) NOT NULL DEFAULT N'Activo',
    CONSTRAINT UQ_Tarifa UNIQUE (IdViaje, TipoTarifa)
);

IF OBJECT_ID(N'dbo.Reserva') IS NULL
CREATE TABLE dbo.Reserva (
    IdReserva     INT IDENTITY(1,1) CONSTRAINT PK_Reserva PRIMARY KEY,
    CodigoReserva NVARCHAR(20) NOT NULL CONSTRAINT UQ_Reserva_Codigo UNIQUE,
    IdUsuario     INT NOT NULL CONSTRAINT FK_Reserva_Usuario REFERENCES dbo.Usuario(IdUsuario),
    IdViaje       INT NOT NULL CONSTRAINT FK_Reserva_Viaje   REFERENCES dbo.Viaje(IdViaje),
    FechaReserva  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Estado        NVARCHAR(20) NOT NULL DEFAULT N'Pendiente',   -- Pendiente, Confirmada, Cancelada
    Total         DECIMAL(10,2) NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.ReservaAsiento') IS NULL
CREATE TABLE dbo.ReservaAsiento (
    IdReservaAsiento INT IDENTITY(1,1) CONSTRAINT PK_ReservaAsiento PRIMARY KEY,
    IdReserva        INT NOT NULL CONSTRAINT FK_RA_Reserva REFERENCES dbo.Reserva(IdReserva) ON DELETE CASCADE,
    IdAsiento        INT NOT NULL CONSTRAINT FK_RA_Asiento REFERENCES dbo.Asiento(IdAsiento),
    IdTarifa         INT NOT NULL CONSTRAINT FK_RA_Tarifa  REFERENCES dbo.Tarifa(IdTarifa),
    Precio           DECIMAL(10,2) NOT NULL,
    Estado           NVARCHAR(20) NOT NULL DEFAULT N'Reservado',
    CONSTRAINT UQ_RA UNIQUE (IdReserva, IdAsiento)
);

IF OBJECT_ID(N'dbo.Pasajero') IS NULL
CREATE TABLE dbo.Pasajero (
    IdPasajero      INT IDENTITY(1,1) CONSTRAINT PK_Pasajero PRIMARY KEY,
    IdReserva       INT NOT NULL CONSTRAINT FK_Pasajero_Reserva REFERENCES dbo.Reserva(IdReserva) ON DELETE CASCADE,
    Nombres         NVARCHAR(100) NOT NULL,
    Apellidos       NVARCHAR(100) NOT NULL,
    TipoDocumento   NVARCHAR(20)  NOT NULL DEFAULT N'DNI',
    NroDocumento    NVARCHAR(20)  NOT NULL,
    FechaNacimiento DATE NOT NULL,
    Telefono        NVARCHAR(15)  NOT NULL DEFAULT N'',
    Correo          NVARCHAR(100) NOT NULL DEFAULT N''
);

IF OBJECT_ID(N'dbo.Pago') IS NULL
CREATE TABLE dbo.Pago (
    IdPago     INT IDENTITY(1,1) CONSTRAINT PK_Pago PRIMARY KEY,
    IdReserva  INT NOT NULL CONSTRAINT FK_Pago_Reserva REFERENCES dbo.Reserva(IdReserva),
    FechaPago  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Monto      DECIMAL(10,2) NOT NULL CONSTRAINT CK_Pago_Monto CHECK (Monto >= 0),
    MetodoPago NVARCHAR(30) NOT NULL,   -- Efectivo, Tarjeta, Yape, Plin
    Referencia NVARCHAR(100) NOT NULL DEFAULT N'',
    Estado     NVARCHAR(20) NOT NULL DEFAULT N'Pagado'
);

IF OBJECT_ID(N'dbo.Cancelacion') IS NULL
CREATE TABLE dbo.Cancelacion (
    IdCancelacion     INT IDENTITY(1,1) CONSTRAINT PK_Cancelacion PRIMARY KEY,
    IdReserva         INT NOT NULL CONSTRAINT UQ_Cancelacion_Reserva UNIQUE   -- 1 cancelación por reserva
                          CONSTRAINT FK_Cancelacion_Reserva REFERENCES dbo.Reserva(IdReserva),
    FechaCancelacion  DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Motivo            NVARCHAR(300) NOT NULL DEFAULT N'',
    MontoReembolso    DECIMAL(10,2) NOT NULL DEFAULT 0,
    Estado            NVARCHAR(20) NOT NULL DEFAULT N'Procesada'
);
GO

/* Bases creadas con una versión anterior del script: agrega Bus.Servicio */
IF COL_LENGTH(N'dbo.Bus', N'Servicio') IS NULL
BEGIN
    ALTER TABLE dbo.Bus ADD Servicio NVARCHAR(30) NOT NULL CONSTRAINT DF_Bus_Servicio DEFAULT N'Ejecutivo';
    EXEC(N'UPDATE dbo.Bus SET Servicio = N''Semi Cama''  WHERE Placa = N''DEF-456'';
           UPDATE dbo.Bus SET Servicio = N''Cama Suite'' WHERE Placa = N''GHI-789'';');
END
GO

/* Bases creadas con una versión anterior: agrega los campos de inicio de sesión */
IF COL_LENGTH(N'dbo.Usuario', N'NombreUsuario') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario ADD
        NombreUsuario    NVARCHAR(30)  NULL,
        ContrasenaHash   NVARCHAR(200) NULL,
        IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuario_Intentos DEFAULT 0,
        BloqueadoHasta   DATETIME2 NULL,
        UltimoAcceso     DATETIME2 NULL;
    EXEC(N'
        UPDATE dbo.Usuario SET NombreUsuario = CASE Correo
            WHEN N''admin@chaskiruta.pe''    THEN N''admin''
            WHEN N''vendedor@chaskiruta.pe'' THEN N''vendedor''
            WHEN N''lucia@example.com''      THEN N''cliente'' END
        WHERE Correo IN (N''admin@chaskiruta.pe'', N''vendedor@chaskiruta.pe'', N''lucia@example.com'');
        UPDATE dbo.Usuario SET NombreUsuario = N''usuario'' + CAST(IdUsuario AS NVARCHAR(10)) WHERE NombreUsuario IS NULL;
        ALTER TABLE dbo.Usuario ALTER COLUMN NombreUsuario NVARCHAR(30) NOT NULL;
        ALTER TABLE dbo.Usuario ADD CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario);');
END
GO

/* ------------------------------ ÍNDICES ------------------------------ */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Viaje_Busqueda')
    CREATE INDEX IX_Viaje_Busqueda ON dbo.Viaje (IdOrigen, IdDestino, FechaSalida);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Usuario')
    CREATE INDEX IX_Reserva_Usuario ON dbo.Reserva (IdUsuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Viaje')
    CREATE INDEX IX_Reserva_Viaje ON dbo.Reserva (IdViaje);
GO

/* --------------------------- DATOS DE PRUEBA ------------------------- */

IF NOT EXISTS (SELECT 1 FROM dbo.Rol)
INSERT dbo.Rol (NombreRol, Descripcion) VALUES
 (N'Administrador', N'Acceso total al sistema'),
 (N'Vendedor',      N'Registra ventas y reservas en terminal'),
 (N'Cliente',       N'Pasajero que reserva en línea');

IF NOT EXISTS (SELECT 1 FROM dbo.Ciudad)
INSERT dbo.Ciudad (NombreCiudad, Departamento) VALUES
 (N'Lima',      N'Lima'),
 (N'Arequipa',  N'Arequipa'),
 (N'Cusco',     N'Cusco'),
 (N'Trujillo',  N'La Libertad'),
 (N'Chiclayo',  N'Lambayeque'),
 (N'Piura',     N'Piura'),
 (N'Huancayo',  N'Junín'),
 (N'Ica',       N'Ica');

IF NOT EXISTS (SELECT 1 FROM dbo.Empresa)
INSERT dbo.Empresa (RazonSocial, Ruc, Telefono, Correo, Direccion) VALUES
 (N'Chaski Ruta S.A.C.', N'20123456789', N'014000000', N'contacto@chaskiruta.pe', N'Av. Javier Prado 1234, Lima');

IF NOT EXISTS (SELECT 1 FROM dbo.Terminal)
INSERT dbo.Terminal (IdEmpresa, NombreTerminal, Direccion, Ciudad)
SELECT e.IdEmpresa, t.Nombre, t.Direccion, t.Ciudad
FROM dbo.Empresa e
CROSS JOIN (VALUES
  (N'Terminal Lima Sur',  N'Av. Paseo de la República 100', N'Lima'),
  (N'Terminal Arequipa',  N'Av. Arturo Ibáñez 200',         N'Arequipa'),
  (N'Terminal Cusco',     N'Av. Vía de Evitamiento 300',    N'Cusco')
) AS t(Nombre, Direccion, Ciudad)
WHERE e.Ruc = N'20123456789';

IF NOT EXISTS (SELECT 1 FROM dbo.Bus)
INSERT dbo.Bus (IdEmpresa, Placa, Modelo, Marca, Anio, CapacidadAsientos, Estado, Servicio)
SELECT e.IdEmpresa, b.Placa, b.Modelo, b.Marca, b.Anio, 40, b.Estado, b.Servicio
FROM dbo.Empresa e
CROSS JOIN (VALUES
  (N'ABC-123', N'Marcopolo G7', N'Volvo',    2022, N'Operativo',     N'Ejecutivo'),
  (N'DEF-456', N'Paradiso 1800', N'Scania',  2021, N'Operativo',     N'Semi Cama'),
  (N'GHI-789', N'Irizar i6',     N'Mercedes', 2020, N'Mantenimiento', N'Cama Suite')
) AS b(Placa, Modelo, Marca, Anio, Estado, Servicio)
WHERE e.Ruc = N'20123456789';

/* 40 asientos por bus: 1-12 en piso 1, 13-40 en piso 2 */
IF NOT EXISTS (SELECT 1 FROM dbo.Asiento)
BEGIN
    ;WITH Numeros AS (
        SELECT TOP (40) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
        FROM sys.all_objects
    )
    INSERT dbo.Asiento (IdBus, NumeroAsiento, Piso)
    SELECT b.IdBus, RIGHT(N'0' + CAST(n.N AS NVARCHAR(2)), 2),
           CASE WHEN n.N <= 12 THEN 1 ELSE 2 END
    FROM dbo.Bus b CROSS JOIN Numeros n;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Ruta)
INSERT dbo.Ruta (IdOrigen, IdDestino, DistanciaKm, DuracionEstimada)
SELECT o.IdCiudad, d.IdCiudad, r.Km, r.Duracion
FROM (VALUES
  (N'Lima',     N'Arequipa', 1010, N'15 h'),
  (N'Arequipa', N'Lima',     1010, N'15 h'),
  (N'Lima',     N'Cusco',    1100, N'21 h'),
  (N'Cusco',    N'Lima',     1100, N'21 h'),
  (N'Lima',     N'Trujillo',  560, N'8 h'),
  (N'Lima',     N'Huancayo',  300, N'7 h'),
  (N'Lima',     N'Ica',       300, N'4 h'),
  (N'Arequipa', N'Cusco',     520, N'10 h')
) AS r(Origen, Destino, Km, Duracion)
JOIN dbo.Ciudad o ON o.NombreCiudad = r.Origen
JOIN dbo.Ciudad d ON d.NombreCiudad = r.Destino;

IF NOT EXISTS (SELECT 1 FROM dbo.Viaje)
BEGIN
    DECLARE @bus1 INT = (SELECT IdBus FROM dbo.Bus WHERE Placa = N'ABC-123');
    DECLARE @bus2 INT = (SELECT IdBus FROM dbo.Bus WHERE Placa = N'DEF-456');

    INSERT dbo.Viaje (IdRuta, IdBus, IdOrigen, IdDestino, FechaSalida, HoraSalida, PrecioBase)
    SELECT ru.IdRuta, v.IdBus, ru.IdOrigen, ru.IdDestino,
           DATEADD(DAY, v.Dias, CAST(GETDATE() AS DATE)), v.Hora, v.Precio
    FROM (VALUES
      (N'Lima',     N'Arequipa', @bus1, 1, CAST('20:00' AS TIME(0)), 90.00),
      (N'Lima',     N'Cusco',    @bus2, 2, CAST('17:30' AS TIME(0)), 120.00),
      (N'Lima',     N'Trujillo', @bus1, 3, CAST('21:00' AS TIME(0)), 60.00),
      (N'Arequipa', N'Cusco',    @bus2, 4, CAST('22:00' AS TIME(0)), 70.00),
      (N'Lima',     N'Ica',      @bus1, 5, CAST('08:00' AS TIME(0)), 35.00)
    ) AS v(Origen, Destino, IdBus, Dias, Hora, Precio)
    JOIN dbo.Ciudad co ON co.NombreCiudad = v.Origen
    JOIN dbo.Ciudad cd ON cd.NombreCiudad = v.Destino
    JOIN dbo.Ruta ru   ON ru.IdOrigen = co.IdCiudad AND ru.IdDestino = cd.IdCiudad;
END;

/* Tarifas por viaje: General 100%, Estudiante 80%, Tercera Edad 70%, Niño 50% */
IF NOT EXISTS (SELECT 1 FROM dbo.Tarifa)
INSERT dbo.Tarifa (IdViaje, TipoTarifa, Precio)
SELECT v.IdViaje, t.Tipo, ROUND(v.PrecioBase * t.Factor, 2)
FROM dbo.Viaje v
CROSS JOIN (VALUES
  (N'General', 1.00), (N'Estudiante', 0.80), (N'Tercera Edad', 0.70), (N'Niño', 0.50)
) AS t(Tipo, Factor);

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario)
INSERT dbo.Usuario (Nombres, Apellidos, Dni, Correo, Telefono, IdRol, NombreUsuario)
SELECT u.Nombres, u.Apellidos, u.Dni, u.Correo, u.Telefono, r.IdRol, u.NombreUsuario
FROM (VALUES
  (N'Admin',   N'Chaski',  N'10000001', N'admin@chaskiruta.pe',    N'999000001', N'Administrador', N'admin'),
  (N'Carlos',  N'Ramos',   N'10000002', N'vendedor@chaskiruta.pe', N'999000002', N'Vendedor',      N'vendedor'),
  (N'Lucía',   N'Torres',  N'10000003', N'lucia@example.com',      N'999000003', N'Cliente',       N'cliente')
) AS u(Nombres, Apellidos, Dni, Correo, Telefono, Rol, NombreUsuario)
JOIN dbo.Rol r ON r.NombreRol = u.Rol;
GO

/* ------------- DATOS DE EJEMPLO: viajes pasados, reservas, pagos ------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Reserva)
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRAN;

    -- Viajes ya realizados (para que los reportes tengan historial)
    INSERT dbo.Viaje (IdRuta, IdBus, IdOrigen, IdDestino, FechaSalida, HoraSalida, PrecioBase, Estado)
    SELECT ru.IdRuta, b.IdBus, ru.IdOrigen, ru.IdDestino,
           DATEADD(DAY, v.Dias, CAST(GETDATE() AS DATE)), v.Hora, v.Precio, N'Finalizado'
    FROM (VALUES
      (N'Lima', N'Arequipa', N'ABC-123', -6, CAST('20:00' AS TIME(0)), 90.00),
      (N'Lima', N'Cusco',    N'DEF-456', -4, CAST('17:30' AS TIME(0)), 120.00),
      (N'Lima', N'Trujillo', N'ABC-123', -2, CAST('21:00' AS TIME(0)), 60.00),
      (N'Lima', N'Ica',      N'DEF-456', -1, CAST('08:00' AS TIME(0)), 35.00)
    ) AS v(Origen, Destino, Placa, Dias, Hora, Precio)
    JOIN dbo.Ciudad co ON co.NombreCiudad = v.Origen
    JOIN dbo.Ciudad cd ON cd.NombreCiudad = v.Destino
    JOIN dbo.Ruta ru   ON ru.IdOrigen = co.IdCiudad AND ru.IdDestino = cd.IdCiudad
    JOIN dbo.Bus b     ON b.Placa = v.Placa;

    -- Tarifas de los viajes que aún no tienen
    INSERT dbo.Tarifa (IdViaje, TipoTarifa, Precio)
    SELECT v.IdViaje, t.Tipo, ROUND(v.PrecioBase * t.Factor, 2)
    FROM dbo.Viaje v
    CROSS JOIN (VALUES
      (N'General', 1.00), (N'Estudiante', 0.80), (N'Tercera Edad', 0.70), (N'Niño', 0.50)
    ) AS t(Tipo, Factor)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Tarifa x WHERE x.IdViaje = v.IdViaje);

    -- Reservas de ejemplo (una por pasajero, un asiento cada una)
    CREATE TABLE #Demo (
        Idx INT PRIMARY KEY, Origen NVARCHAR(100), Destino NVARCHAR(100), DiasViaje INT, DiasReserva INT,
        EstadoReserva NVARCHAR(20), Nombres NVARCHAR(100), Apellidos NVARCHAR(100), Dni NVARCHAR(20),
        Asiento NVARCHAR(10), Tarifa NVARCHAR(30), Metodo NVARCHAR(30), EstadoPago NVARCHAR(20),
        Motivo NVARCHAR(300) NULL, PctReembolso INT NULL,
        IdViaje INT NULL, IdReserva INT NULL, IdAsiento INT NULL, IdTarifa INT NULL, Precio DECIMAL(10,2) NULL
    );

    INSERT #Demo (Idx, Origen, Destino, DiasViaje, DiasReserva, EstadoReserva, Nombres, Apellidos, Dni, Asiento, Tarifa, Metodo, EstadoPago, Motivo, PctReembolso) VALUES
     (1,  N'Lima',     N'Arequipa', -6, -9, N'Confirmada', N'Juan Carlos', N'Pérez Quispe',   N'12345678', N'05', N'General',      N'Yape',          N'Pagado',    NULL, NULL),
     (2,  N'Lima',     N'Arequipa', -6, -9, N'Confirmada', N'María',       N'López García',   N'87654321', N'12', N'Estudiante',   N'Tarjeta',       N'Pagado',    NULL, NULL),
     (3,  N'Lima',     N'Cusco',    -4, -7, N'Confirmada', N'Pedro',       N'Ramírez Soto',   N'11223344', N'08', N'General',      N'Efectivo',      N'Pagado',    NULL, NULL),
     (4,  N'Lima',     N'Cusco',    -4, -6, N'Confirmada', N'Ana',         N'Torres Medina',  N'22334455', N'03', N'Tercera Edad', N'Yape',          N'Pagado',    NULL, NULL),
     (5,  N'Lima',     N'Trujillo', -2, -5, N'Confirmada', N'Luis',        N'Mendoza Vargas', N'33445566', N'07', N'General',      N'Transferencia', N'Pagado',    NULL, NULL),
     (6,  N'Lima',     N'Ica',      -1, -3, N'Confirmada', N'Rosa',        N'Huamán Díaz',    N'44556677', N'15', N'Niño',         N'Efectivo',      N'Pagado',    NULL, NULL),
     (7,  N'Lima',     N'Arequipa',  1, -2, N'Confirmada', N'Carlos',      N'Salazar Rojas',  N'55667788', N'05', N'General',      N'Yape',          N'Pagado',    NULL, NULL),
     (8,  N'Lima',     N'Cusco',     2,  0, N'Confirmada', N'Diana',       N'Flores Paredes', N'66778899', N'10', N'General',      N'Tarjeta',       N'Pagado',    NULL, NULL),
     (9,  N'Lima',     N'Trujillo',  3,  0, N'Confirmada', N'Jorge',       N'Vega Luna',      N'77889900', N'20', N'Estudiante',   N'Yape',          N'Pagado',    NULL, NULL),
     (10, N'Arequipa', N'Cusco',     4,  0, N'Pendiente',  N'Sofía',       N'Mamani Condori', N'88990011', N'14', N'General',      N'Efectivo',      N'Pendiente', NULL, NULL),
     (11, N'Lima',     N'Ica',       5, -1, N'Cancelada',  N'Miguel',      N'Castro Ruiz',    N'99001122', N'22', N'General',      N'Yape',          N'Pagado',    N'Cambio de planes',  100),
     (12, N'Lima',     N'Arequipa',  1,  0, N'Cancelada',  N'Elena',       N'Paz Rojas',      N'10101010', N'07', N'General',      N'Tarjeta',       N'Pagado',    N'Emergencia',         80),
     (13, N'Lima',     N'Cusco',     2, -1, N'Cancelada',  N'Hugo',        N'Medina Torres',  N'20202020', N'18', N'General',      N'Efectivo',      N'Pagado',    N'Error en reserva',    0);

    UPDATE d SET
        IdViaje  = v.IdViaje,
        IdAsiento = a.IdAsiento,
        IdTarifa = t.IdTarifa,
        Precio   = t.Precio
    FROM #Demo d
    JOIN dbo.Ciudad co ON co.NombreCiudad = d.Origen
    JOIN dbo.Ciudad cd ON cd.NombreCiudad = d.Destino
    JOIN dbo.Viaje v   ON v.IdOrigen = co.IdCiudad AND v.IdDestino = cd.IdCiudad
                      AND v.FechaSalida = DATEADD(DAY, d.DiasViaje, CAST(GETDATE() AS DATE))
    JOIN dbo.Asiento a ON a.IdBus = v.IdBus AND a.NumeroAsiento = d.Asiento
    JOIN dbo.Tarifa t  ON t.IdViaje = v.IdViaje AND t.TipoTarifa = d.Tarifa;

    DECLARE @vendedor INT = (SELECT IdUsuario FROM dbo.Usuario WHERE Correo = N'vendedor@chaskiruta.pe');

    INSERT dbo.Reserva (CodigoReserva, IdUsuario, IdViaje, FechaReserva, Estado, Total)
    SELECT N'TMP-' + CAST(d.Idx AS NVARCHAR(10)), @vendedor, d.IdViaje,
           DATEADD(HOUR, 9 + d.Idx % 8, CAST(DATEADD(DAY, d.DiasReserva, CAST(GETDATE() AS DATE)) AS DATETIME2)),
           d.EstadoReserva, d.Precio
    FROM #Demo d;

    UPDATE d SET IdReserva = r.IdReserva
    FROM #Demo d JOIN dbo.Reserva r ON r.CodigoReserva = N'TMP-' + CAST(d.Idx AS NVARCHAR(10));

    UPDATE dbo.Reserva
    SET CodigoReserva = N'RES-' + RIGHT(N'000000' + CAST(IdReserva AS NVARCHAR(10)), 6)
    WHERE CodigoReserva LIKE N'TMP-%';

    INSERT dbo.Pasajero (IdReserva, Nombres, Apellidos, TipoDocumento, NroDocumento, FechaNacimiento, Telefono, Correo)
    SELECT d.IdReserva, d.Nombres, d.Apellidos, N'DNI', d.Dni,
           DATEADD(YEAR, CASE d.Tarifa WHEN N'Niño' THEN -8 WHEN N'Tercera Edad' THEN -65 WHEN N'Estudiante' THEN -20 ELSE -35 END,
                   CAST(GETDATE() AS DATE)),
           N'9' + RIGHT(d.Dni, 8), LOWER(REPLACE(d.Nombres, N' ', N'')) + N'@example.com'
    FROM #Demo d;

    INSERT dbo.ReservaAsiento (IdReserva, IdAsiento, IdTarifa, Precio, Estado)
    SELECT d.IdReserva, d.IdAsiento, d.IdTarifa, d.Precio,
           CASE d.EstadoReserva WHEN N'Cancelada' THEN N'Cancelado' ELSE N'Reservado' END
    FROM #Demo d;

    INSERT dbo.Pago (IdReserva, FechaPago, Monto, MetodoPago, Referencia, Estado)
    SELECT d.IdReserva, DATEADD(MINUTE, 5, r.FechaReserva), d.Precio, d.Metodo,
           N'OP-' + RIGHT(N'0000' + CAST(d.Idx AS NVARCHAR(10)), 4), d.EstadoPago
    FROM #Demo d JOIN dbo.Reserva r ON r.IdReserva = d.IdReserva;

    INSERT dbo.Cancelacion (IdReserva, FechaCancelacion, Motivo, MontoReembolso, Estado)
    SELECT d.IdReserva, DATEADD(HOUR, 2, r.FechaReserva), d.Motivo,
           ROUND(d.Precio * d.PctReembolso / 100.0, 2),
           CASE WHEN d.PctReembolso > 0 THEN N'Reembolsado' ELSE N'Sin devolución' END
    FROM #Demo d JOIN dbo.Reserva r ON r.IdReserva = d.IdReserva
    WHERE d.EstadoReserva = N'Cancelada';

    DROP TABLE #Demo;
    COMMIT;
END;
GO

/* ------------------------------ COMPROBACIÓN ------------------------- */
SELECT N'Rol' AS Tabla, COUNT(*) AS Filas FROM dbo.Rol UNION ALL
SELECT N'Usuario',  COUNT(*) FROM dbo.Usuario  UNION ALL
SELECT N'Ciudad',   COUNT(*) FROM dbo.Ciudad   UNION ALL
SELECT N'Empresa',  COUNT(*) FROM dbo.Empresa  UNION ALL
SELECT N'Terminal', COUNT(*) FROM dbo.Terminal UNION ALL
SELECT N'Bus',      COUNT(*) FROM dbo.Bus      UNION ALL
SELECT N'Asiento',  COUNT(*) FROM dbo.Asiento  UNION ALL
SELECT N'Ruta',     COUNT(*) FROM dbo.Ruta     UNION ALL
SELECT N'Viaje',    COUNT(*) FROM dbo.Viaje    UNION ALL
SELECT N'Tarifa',   COUNT(*) FROM dbo.Tarifa   UNION ALL
SELECT N'Reserva',  COUNT(*) FROM dbo.Reserva  UNION ALL
SELECT N'Pasajero', COUNT(*) FROM dbo.Pasajero UNION ALL
SELECT N'ReservaAsiento', COUNT(*) FROM dbo.ReservaAsiento UNION ALL
SELECT N'Pago',     COUNT(*) FROM dbo.Pago     UNION ALL
SELECT N'Cancelacion', COUNT(*) FROM dbo.Cancelacion;
GO
