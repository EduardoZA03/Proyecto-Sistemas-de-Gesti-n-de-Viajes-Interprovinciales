/* Chaski Ruta - Base de datos ChaskiRuta (SQL Server / LocalDB).
   Se puede ejecutar varias veces sin duplicar datos. */

IF DB_ID(N'ChaskiRuta') IS NULL
    CREATE DATABASE ChaskiRuta;
GO

USE ChaskiRuta;
GO

-- Tablas

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

-- Índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Viaje_Busqueda')
    CREATE INDEX IX_Viaje_Busqueda ON dbo.Viaje (IdOrigen, IdDestino, FechaSalida);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Usuario')
    CREATE INDEX IX_Reserva_Usuario ON dbo.Reserva (IdUsuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reserva_Viaje')
    CREATE INDEX IX_Reserva_Viaje ON dbo.Reserva (IdViaje);
GO

-- Datos de prueba

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

-- Datos de ejemplo: viajes pasados, reservas y pagos
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

-- Control de viajes: agrega a cada viaje el chofer y las horas reales de salida y llegada
IF COL_LENGTH(N'dbo.Viaje', N'IdChofer') IS NULL
    ALTER TABLE dbo.Viaje ADD IdChofer INT NULL CONSTRAINT FK_Viaje_Chofer REFERENCES dbo.Usuario(IdUsuario);
IF COL_LENGTH(N'dbo.Viaje', N'FechaHoraSalidaReal') IS NULL
    ALTER TABLE dbo.Viaje ADD FechaHoraSalidaReal DATETIME2 NULL;
IF COL_LENGTH(N'dbo.Viaje', N'FechaHoraLlegadaReal') IS NULL
    ALTER TABLE dbo.Viaje ADD FechaHoraLlegadaReal DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Viaje_Chofer')
    CREATE INDEX IX_Viaje_Chofer ON dbo.Viaje (IdChofer);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Viaje_Estado_Fecha')
    CREATE INDEX IX_Viaje_Estado_Fecha ON dbo.Viaje (Estado, FechaSalida);
GO

/* Rol del chofer: solo ve sus viajes y marca la partida y la llegada */
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE NombreRol = N'Chofer')
    INSERT dbo.Rol (NombreRol, Descripcion) VALUES (N'Chofer', N'Conduce los viajes y actualiza su estado');

/* Dos choferes de ejemplo (la app les da contraseña inicial solo en desarrollo) */
INSERT dbo.Usuario (Nombres, Apellidos, Dni, Correo, Telefono, IdRol, NombreUsuario)
SELECT u.Nombres, u.Apellidos, u.Dni, u.Correo, u.Telefono, r.IdRol, u.NombreUsuario
FROM (VALUES
  (N'Julio', N'Paredes', N'10000004', N'chofer1@chaskiruta.pe', N'999000004', N'chofer1'),
  (N'Marco', N'Villanueva', N'10000005', N'chofer2@chaskiruta.pe', N'999000005', N'chofer2')
) AS u(Nombres, Apellidos, Dni, Correo, Telefono, NombreUsuario)
CROSS JOIN (SELECT IdRol FROM dbo.Rol WHERE NombreRol = N'Chofer') r
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuario x
                  WHERE x.NombreUsuario = u.NombreUsuario OR x.Dni = u.Dni OR x.Correo = u.Correo);
GO

/* Los viajes de ejemplo sin chofer se reparten entre los dos choferes según el bus */
UPDATE v SET IdChofer = c.IdUsuario
FROM dbo.Viaje v
JOIN dbo.Bus b     ON b.IdBus = v.IdBus
JOIN dbo.Usuario c ON c.NombreUsuario = CASE b.Placa WHEN N'DEF-456' THEN N'chofer2' ELSE N'chofer1' END
WHERE v.IdChofer IS NULL AND b.Placa IN (N'ABC-123', N'DEF-456', N'GHI-789');
GO

/* Viajes de ejemplo ya finalizados: horas reales con pequeñas demoras, para que el reporte de retrasos tenga datos */
UPDATE v SET
    FechaHoraSalidaReal  = DATEADD(MINUTE, (v.IdViaje * 7) % 32, s.Salida),
    FechaHoraLlegadaReal = DATEADD(MINUTE, d.Minutos + (v.IdViaje * 7) % 32 + (v.IdViaje * 13) % 46 - 8, s.Salida)
FROM dbo.Viaje v
JOIN dbo.Ruta r ON r.IdRuta = v.IdRuta
CROSS APPLY (SELECT DATEADD(SECOND, DATEDIFF(SECOND, 0, v.HoraSalida), CAST(v.FechaSalida AS DATETIME2)) AS Salida) s
CROSS APPLY (SELECT CHARINDEX(N'h', r.DuracionEstimada) AS H, CHARINDEX(N'min', r.DuracionEstimada) AS M) p
CROSS APPLY (SELECT
      CASE WHEN p.H > 0 THEN ISNULL(TRY_CAST(LEFT(r.DuracionEstimada, p.H - 1) AS INT), 0) ELSE 0 END * 60
    + CASE WHEN p.M > 0 THEN ISNULL(TRY_CAST(LTRIM(RTRIM(SUBSTRING(r.DuracionEstimada, p.H + 1, p.M - p.H - 1))) AS INT), 0) ELSE 0 END AS Minutos) d
WHERE v.Estado = N'Finalizado' AND v.IdChofer IS NOT NULL
  AND v.FechaHoraSalidaReal IS NULL AND v.FechaHoraLlegadaReal IS NULL;
GO

/* Datos adicionales (ciudades, flota, personal, viajes y ventas). Se carga una sola vez (se reconoce por el bus
   JKL-012). Cada bus hace un viaje al día en su horario fijo con su chofer fijo, así que nunca se cruzan. */
IF NOT EXISTS (SELECT 1 FROM dbo.Bus WHERE Placa = N'JKL-012')
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRAN;

    DECLARE @ahora DATETIME2(0) = GETDATE();
    DECLARE @hoy DATE = CAST(@ahora AS DATE);

    /* ---- Ciudades ---- */
    INSERT dbo.Ciudad (NombreCiudad, Departamento)
    SELECT c.Nombre, c.Departamento
    FROM (VALUES
      (N'Tacna', N'Tacna'), (N'Puno', N'Puno'), (N'Ayacucho', N'Ayacucho'), (N'Cajamarca', N'Cajamarca'),
      (N'Chimbote', N'Áncash'), (N'Huaraz', N'Áncash'), (N'Tumbes', N'Tumbes')
    ) AS c(Nombre, Departamento)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Ciudad x WHERE x.NombreCiudad = c.Nombre AND x.Departamento = c.Departamento);

    /* ---- Terminales ---- */
    INSERT dbo.Terminal (IdEmpresa, NombreTerminal, Direccion, Ciudad)
    SELECT e.IdEmpresa, t.Nombre, t.Direccion, t.Ciudad
    FROM dbo.Empresa e
    CROSS JOIN (VALUES
      (N'Terminal Trujillo', N'Av. América Sur 1250',      N'Trujillo'),
      (N'Terminal Chiclayo', N'Av. Bolognesi 536',         N'Chiclayo'),
      (N'Terminal Piura',    N'Av. Sánchez Cerro 1100',    N'Piura'),
      (N'Terminal Huancayo', N'Av. Ferrocarril 1043',      N'Huancayo'),
      (N'Terminal Ica',      N'Av. Lima 220',              N'Ica'),
      (N'Terminal Tacna',    N'Av. Hipólito Unanue 500',   N'Tacna'),
      (N'Terminal Puno',     N'Jr. Primero de Mayo 703',   N'Puno')
    ) AS t(Nombre, Direccion, Ciudad)
    WHERE e.Ruc = N'20123456789'
      AND NOT EXISTS (SELECT 1 FROM dbo.Terminal x WHERE x.NombreTerminal = t.Nombre);

    /* ---- Rutas (en ambos sentidos) ---- */
    INSERT dbo.Ruta (IdOrigen, IdDestino, DistanciaKm, DuracionEstimada)
    SELECT o.IdCiudad, d.IdCiudad, r.Km, r.Duracion
    FROM (VALUES
      (N'Trujillo', N'Lima',      560, N'8 h'),
      (N'Huancayo', N'Lima',      300, N'7 h'),
      (N'Ica',      N'Lima',      300, N'4 h'),
      (N'Cusco',    N'Arequipa',  520, N'10 h'),
      (N'Lima',     N'Chiclayo',  770, N'12 h'),
      (N'Chiclayo', N'Lima',      770, N'12 h'),
      (N'Lima',     N'Piura',     980, N'16 h'),
      (N'Piura',    N'Lima',      980, N'16 h'),
      (N'Lima',     N'Tacna',    1290, N'19 h'),
      (N'Tacna',    N'Lima',     1290, N'19 h'),
      (N'Lima',     N'Ayacucho',  560, N'9 h'),
      (N'Ayacucho', N'Lima',      560, N'9 h'),
      (N'Lima',     N'Huaraz',    400, N'8 h'),
      (N'Huaraz',   N'Lima',      400, N'8 h'),
      (N'Lima',     N'Cajamarca', 860, N'14 h'),
      (N'Cajamarca',N'Lima',      860, N'14 h'),
      (N'Lima',     N'Chimbote',  420, N'6 h 30 min'),
      (N'Chimbote', N'Lima',      420, N'6 h 30 min'),
      (N'Arequipa', N'Tacna',     320, N'6 h 30 min'),
      (N'Tacna',    N'Arequipa',  320, N'6 h 30 min'),
      (N'Arequipa', N'Puno',      300, N'5 h 30 min'),
      (N'Puno',     N'Arequipa',  300, N'5 h 30 min'),
      (N'Cusco',    N'Puno',      390, N'6 h 30 min'),
      (N'Puno',     N'Cusco',     390, N'6 h 30 min'),
      (N'Trujillo', N'Chiclayo',  210, N'3 h 30 min'),
      (N'Chiclayo', N'Trujillo',  210, N'3 h 30 min'),
      (N'Chiclayo', N'Piura',     220, N'3 h 30 min'),
      (N'Piura',    N'Chiclayo',  220, N'3 h 30 min'),
      (N'Piura',    N'Tumbes',    280, N'4 h'),
      (N'Tumbes',   N'Piura',     280, N'4 h'),
      (N'Trujillo', N'Cajamarca', 300, N'5 h'),
      (N'Cajamarca',N'Trujillo',  300, N'5 h')
    ) AS r(Origen, Destino, Km, Duracion)
    JOIN dbo.Ciudad o ON o.NombreCiudad = r.Origen
    JOIN dbo.Ciudad d ON d.NombreCiudad = r.Destino
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Ruta x WHERE x.IdOrigen = o.IdCiudad AND x.IdDestino = d.IdCiudad);

    /* ---- Personal: 2 vendedores más y 8 choferes más ---- */
    INSERT dbo.Usuario (Nombres, Apellidos, Dni, Correo, Telefono, IdRol, NombreUsuario)
    SELECT u.Nombres, u.Apellidos, u.Dni, u.Correo, u.Telefono, r.IdRol, u.NombreUsuario
    FROM (VALUES
      (N'Rosa',    N'Quispe Mamani',     N'10000006', N'vendedor2@chaskiruta.pe', N'999000006', N'Vendedor', N'vendedor2'),
      (N'Luis',    N'Cárdenas Ríos',     N'10000007', N'vendedor3@chaskiruta.pe', N'999000007', N'Vendedor', N'vendedor3'),
      (N'Raúl',    N'Mendoza Salas',     N'10000008', N'chofer3@chaskiruta.pe',   N'999000008', N'Chofer',   N'chofer3'),
      (N'Víctor',  N'Huamán Cruz',       N'10000009', N'chofer4@chaskiruta.pe',   N'999000009', N'Chofer',   N'chofer4'),
      (N'Óscar',   N'Salinas Torres',    N'10000010', N'chofer5@chaskiruta.pe',   N'999000010', N'Chofer',   N'chofer5'),
      (N'Pablo',   N'Cáceres Luna',      N'10000011', N'chofer6@chaskiruta.pe',   N'999000011', N'Chofer',   N'chofer6'),
      (N'Ernesto', N'Rivas Campos',      N'10000012', N'chofer7@chaskiruta.pe',   N'999000012', N'Chofer',   N'chofer7'),
      (N'Hernán',  N'Zavala Soto',       N'10000013', N'chofer8@chaskiruta.pe',   N'999000013', N'Chofer',   N'chofer8'),
      (N'Walter',  N'Cornejo Vega',      N'10000014', N'chofer9@chaskiruta.pe',   N'999000014', N'Chofer',   N'chofer9'),
      (N'Gustavo', N'Ibarra Paredes',    N'10000015', N'chofer10@chaskiruta.pe',  N'999000015', N'Chofer',   N'chofer10')
    ) AS u(Nombres, Apellidos, Dni, Correo, Telefono, Rol, NombreUsuario)
    JOIN dbo.Rol r ON r.NombreRol = u.Rol
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuario x
                      WHERE x.NombreUsuario = u.NombreUsuario OR x.Dni = u.Dni OR x.Correo = u.Correo);

    /* ---- Flota: 8 buses operativos más y 1 en mantenimiento ---- */
    INSERT dbo.Bus (IdEmpresa, Placa, Modelo, Marca, Anio, CapacidadAsientos, Estado, Servicio)
    SELECT e.IdEmpresa, b.Placa, b.Modelo, b.Marca, b.Anio, b.Capacidad, b.Estado, b.Servicio
    FROM dbo.Empresa e
    CROSS JOIN (VALUES
      (N'JKL-012', N'9800 DD',        N'Volvo',    2023, 40, N'Operativo',     N'Ejecutivo'),
      (N'MNO-345', N'K360',           N'Scania',   2022, 44, N'Operativo',     N'Semi Cama'),
      (N'PQR-678', N'O500 RSD',       N'Mercedes', 2021, 36, N'Operativo',     N'Cama Suite'),
      (N'STU-901', N'Paradiso G8',    N'Marcopolo',2023, 40, N'Operativo',     N'Ejecutivo'),
      (N'VWX-234', N'i8',             N'Irizar',   2022, 40, N'Operativo',     N'Semi Cama'),
      (N'YZA-567', N'9700',           N'Volvo',    2020, 44, N'Operativo',     N'Cama Suite'),
      (N'BCD-890', N'K410',           N'Scania',   2024, 48, N'Operativo',     N'Ejecutivo'),
      (N'EFG-123', N'O500 RS',        N'Mercedes', 2021, 44, N'Operativo',     N'Semi Cama'),
      (N'HIJ-456', N'Torino',         N'MAN',      2019, 40, N'Mantenimiento', N'Ejecutivo')
    ) AS b(Placa, Modelo, Marca, Anio, Capacidad, Estado, Servicio)
    WHERE e.Ruc = N'20123456789';

    ;WITH Numeros AS (
        SELECT TOP (80) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N FROM sys.all_objects
    )
    INSERT dbo.Asiento (IdBus, NumeroAsiento, Piso)
    SELECT b.IdBus, RIGHT(N'0' + CAST(n.N AS NVARCHAR(2)), 2),
           CASE WHEN n.N <= ROUND(b.CapacidadAsientos * 0.3, 0) THEN 1 ELSE 2 END
    FROM dbo.Bus b
    JOIN Numeros n ON n.N <= b.CapacidadAsientos
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Asiento a WHERE a.IdBus = b.IdBus);

    /* ---- Viajes: cada bus recorre su línea (ida, vuelta, descanso) a su hora fija ---- */
    CREATE TABLE #Linea (
        Idx INT PRIMARY KEY, Placa NVARCHAR(10), Chofer NVARCHAR(30),
        Origen NVARCHAR(100), Destino NVARCHAR(100), Hora TIME(0), Precio DECIMAL(10,2)
    );
    INSERT #Linea (Idx, Placa, Chofer, Origen, Destino, Hora, Precio) VALUES
      (0, N'JKL-012', N'chofer3',  N'Lima',     N'Trujillo', '21:00',  60.00),
      (1, N'MNO-345', N'chofer4',  N'Lima',     N'Arequipa', '19:30',  90.00),
      (2, N'PQR-678', N'chofer5',  N'Lima',     N'Ica',      '07:00',  35.00),
      (3, N'STU-901', N'chofer6',  N'Lima',     N'Huancayo', '22:00',  50.00),
      (4, N'VWX-234', N'chofer7',  N'Arequipa', N'Cusco',    '21:30',  70.00),
      (5, N'YZA-567', N'chofer8',  N'Lima',     N'Chiclayo', '20:00',  80.00),
      (6, N'BCD-890', N'chofer9',  N'Lima',     N'Piura',    '16:00', 110.00),
      (7, N'EFG-123', N'chofer10', N'Lima',     N'Tacna',    '15:00', 130.00);

    CREATE TABLE #NV (IdViaje INT PRIMARY KEY);

    ;WITH Dias AS (
        SELECT TOP (33) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 19 AS D FROM sys.all_objects
    ), Base AS (
        SELECT l.Placa, l.Chofer, l.Precio, l.Idx, x.D,
               ((x.D + l.Idx) % 3 + 3) % 3 AS Ciclo,
               DATEADD(SECOND, DATEDIFF(SECOND, 0, l.Hora), CAST(DATEADD(DAY, x.D, @hoy) AS DATETIME2(0))) AS Salida,
               hh.H,
               CASE WHEN ((x.D + l.Idx) % 3 + 3) % 3 = 1 THEN l.Origen  ELSE l.Destino END AS Desde,
               CASE WHEN ((x.D + l.Idx) % 3 + 3) % 3 = 1 THEN l.Destino ELSE l.Origen  END AS Hasta
        FROM #Linea l CROSS JOIN Dias x
        CROSS APPLY (SELECT (CAST((l.Idx + 1) * 1000 + x.D + 100 AS BIGINT) * 1103515245 + 12345) % 2147483647 AS S1) a1
        CROSS APPLY (SELECT (a1.S1 * 1103515245 + 12345) % 2147483647 / 4096 AS H) hh
    )
    INSERT dbo.Viaje (IdRuta, IdBus, IdOrigen, IdDestino, FechaSalida, HoraSalida, PrecioBase, Estado,
                      IdChofer, FechaHoraSalidaReal, FechaHoraLlegadaReal)
    OUTPUT inserted.IdViaje INTO #NV (IdViaje)
    SELECT ru.IdRuta, bu.IdBus, ru.IdOrigen, ru.IdDestino,
           CAST(b.Salida AS DATE), CAST(b.Salida AS TIME(0)),
           ROUND(b.Precio * CASE WHEN DATEDIFF(DAY, 0, CAST(b.Salida AS DATE)) % 7 >= 4 THEN 1.10 ELSE 1.00 END, 0),
           e.Estado, ch.IdUsuario,
           CASE e.Estado
                WHEN N'Finalizado' THEN DATEADD(MINUTE, t.DemoraSalida, b.Salida)
                WHEN N'En ruta'    THEN CASE WHEN DATEADD(MINUTE, t.DemoraSalida, b.Salida) <= @ahora
                                             THEN DATEADD(MINUTE, t.DemoraSalida, b.Salida) ELSE b.Salida END
           END,
           CASE WHEN e.Estado = N'Finalizado' THEN DATEADD(MINUTE, t.DemoraSalida + t.DemoraRuta, t.LlegadaEst) END
    FROM Base b
    JOIN dbo.Ciudad co ON co.NombreCiudad = b.Desde
    JOIN dbo.Ciudad cd ON cd.NombreCiudad = b.Hasta
    JOIN dbo.Ruta ru   ON ru.IdOrigen = co.IdCiudad AND ru.IdDestino = cd.IdCiudad
    JOIN dbo.Bus bu    ON bu.Placa = b.Placa
    JOIN dbo.Usuario ch ON ch.NombreUsuario = b.Chofer
    CROSS APPLY (SELECT CHARINDEX(N'h', ru.DuracionEstimada) AS H, CHARINDEX(N'min', ru.DuracionEstimada) AS M) p
    CROSS APPLY (SELECT
          CASE WHEN p.H > 0 THEN ISNULL(TRY_CAST(LEFT(ru.DuracionEstimada, p.H - 1) AS INT), 0) ELSE 0 END * 60
        + CASE WHEN p.M > 0 THEN ISNULL(TRY_CAST(LTRIM(RTRIM(SUBSTRING(ru.DuracionEstimada, p.H + 1, p.M - p.H - 1))) AS INT), 0) ELSE 0 END AS Minutos) d
    CROSS APPLY (SELECT DATEADD(MINUTE, d.Minutos, b.Salida) AS LlegadaEst,
                        b.H % 16 + CASE WHEN b.H % 9 = 0 THEN 20 ELSE 0 END AS DemoraSalida,   -- minutos tarde al salir
                        (b.H / 7) % 55 - 25 AS DemoraRuta) t                                   -- minutos de más o de menos en el camino
    CROSS APPLY (SELECT CASE WHEN b.H % 23 = 0 THEN N'Cancelado'
                             WHEN b.Salida > @ahora THEN N'Programado'
                             WHEN DATEADD(HOUR, 3, t.LlegadaEst) <= @ahora THEN N'Finalizado'
                             ELSE N'En ruta' END AS Estado) e
    WHERE b.Ciclo <> 0;

    INSERT dbo.Tarifa (IdViaje, TipoTarifa, Precio)
    SELECT v.IdViaje, t.Tipo, ROUND(v.PrecioBase * t.Factor, 2)
    FROM dbo.Viaje v
    CROSS JOIN (VALUES
      (N'General', 1.00), (N'Estudiante', 0.80), (N'Tercera Edad', 0.70), (N'Niño', 0.50)
    ) AS t(Tipo, Factor)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Tarifa x WHERE x.IdViaje = v.IdViaje);

    /* ---- Ventas: reservas, pasajeros, pagos y cancelaciones de los viajes nuevos ---- */
    CREATE TABLE #Nombres (Idx INT PRIMARY KEY, Nombre NVARCHAR(50), Ascii NVARCHAR(50));
    INSERT #Nombres (Idx, Nombre, Ascii) VALUES
      (0, N'Juan', N'juan'), (1, N'María', N'maria'), (2, N'José', N'jose'), (3, N'Rosa', N'rosa'),
      (4, N'Luis', N'luis'), (5, N'Carmen', N'carmen'), (6, N'Carlos', N'carlos'), (7, N'Ana', N'ana'),
      (8, N'Jorge', N'jorge'), (9, N'Lucía', N'lucia'), (10, N'Miguel', N'miguel'), (11, N'Elena', N'elena'),
      (12, N'Pedro', N'pedro'), (13, N'Sofía', N'sofia'), (14, N'Víctor', N'victor'), (15, N'Patricia', N'patricia'),
      (16, N'Manuel', N'manuel'), (17, N'Julia', N'julia'), (18, N'Fernando', N'fernando'), (19, N'Diana', N'diana'),
      (20, N'Ricardo', N'ricardo'), (21, N'Gabriela', N'gabriela'), (22, N'Andrés', N'andres'), (23, N'Paola', N'paola'),
      (24, N'Hugo', N'hugo'), (25, N'Milagros', N'milagros'), (26, N'César', N'cesar'), (27, N'Valeria', N'valeria'),
      (28, N'Álvaro', N'alvaro'), (29, N'Isabel', N'isabel');

    CREATE TABLE #Apellidos (Idx INT PRIMARY KEY, Apellido NVARCHAR(50));
    INSERT #Apellidos (Idx, Apellido) VALUES
      (0, N'Quispe'), (1, N'Huamán'), (2, N'Flores'), (3, N'Rojas'), (4, N'Mamani'), (5, N'Condori'),
      (6, N'Vargas'), (7, N'Castillo'), (8, N'Chávez'), (9, N'Torres'), (10, N'Ramírez'), (11, N'Gutiérrez'),
      (12, N'Díaz'), (13, N'Sánchez'), (14, N'Rodríguez'), (15, N'García'), (16, N'López'), (17, N'Mendoza'),
      (18, N'Silva'), (19, N'Cruz'), (20, N'Rivera'), (21, N'Paredes'), (22, N'Salazar'), (23, N'Medina'),
      (24, N'Vega'), (25, N'Luna'), (26, N'Soto'), (27, N'Ríos'), (28, N'Campos'), (29, N'Espinoza');

    CREATE TABLE #Vend (Idx INT PRIMARY KEY, IdUsuario INT);
    INSERT #Vend (Idx, IdUsuario)
    SELECT ROW_NUMBER() OVER (ORDER BY NombreUsuario) - 1, IdUsuario
    FROM dbo.Usuario WHERE NombreUsuario IN (N'vendedor', N'vendedor2', N'vendedor3');
    DECLARE @nVend INT = (SELECT COUNT(*) FROM #Vend);

    CREATE TABLE #Num (K INT PRIMARY KEY);
    INSERT #Num (K) SELECT TOP (80) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 FROM sys.all_objects;

    CREATE TABLE #Gen (
        RowId INT IDENTITY(1,1) PRIMARY KEY, IdViaje INT, H INT, IdAsiento INT, IdTarifa INT, Precio DECIMAL(10,2),
        Tipo NVARCHAR(30), Nombres NVARCHAR(100), Apellidos NVARCHAR(100), Dni NVARCHAR(8), Nacimiento DATE,
        CorreoPas NVARCHAR(100), Estado NVARCHAR(20), FechaReserva DATETIME2(0), FechaCancel DATETIME2(0) NULL,
        Salida DATETIME2(0), Metodo NVARCHAR(30), IdVendedor INT, IdReserva INT NULL
    );

    ;WITH Viajes AS (
        SELECT v.IdViaje, v.Estado, bu.IdBus, bu.CapacidadAsientos AS Cap,
               DATEADD(SECOND, DATEDIFF(SECOND, 0, v.HoraSalida), CAST(v.FechaSalida AS DATETIME2(0))) AS Salida,
               DATEDIFF(DAY, @hoy, v.FechaSalida) AS D,
               hv.H AS H2
        FROM dbo.Viaje v
        JOIN #NV n ON n.IdViaje = v.IdViaje
        JOIN dbo.Bus bu ON bu.IdBus = v.IdBus
        CROSS APPLY (SELECT (CAST(v.IdViaje AS BIGINT) * 37 * 1103515245 + 12345) % 2147483647 AS S1) q1
        CROSS APPLY (SELECT (q1.S1 * 1103515245 + 12345) % 2147483647 / 4096 AS H) hv
        WHERE v.Estado <> N'Cancelado'
    ), Ocupacion AS (
        SELECT Viajes.*,
               CASE WHEN Estado = N'Programado'
                    THEN CASE WHEN 48 - 2 * D + H2 % 15 < 8 THEN 8 ELSE 48 - 2 * D + H2 % 15 END
                    ELSE 15 + H2 % 51 END AS Pct
        FROM Viajes
    )
    INSERT #Gen (IdViaje, H, IdAsiento, IdTarifa, Precio, Tipo, Nombres, Apellidos, Dni, Nacimiento, CorreoPas,
                 Estado, FechaReserva, FechaCancel, Salida, Metodo, IdVendedor)
    SELECT o.IdViaje, CAST(h7.S / 4096 AS INT), a.IdAsiento, t.IdTarifa, t.Precio, x.Tipo, nm.Nombre,
           a1.Apellido + N' ' + a2.Apellido, p.Dni,
           DATEADD(DAY, -(x.Edad * 365 + p.Num % 300), @hoy),
           nm.Ascii + CAST(p.Num % 1000 AS NVARCHAR(4)) + N'@example.com',
           e.Estado,
           CASE WHEN e.Estado = N'Cancelada' THEN DATEADD(HOUR, -(2 + (h4.S / 4096) % 72), c.Fecha) ELSE r.Fecha END,
           CASE WHEN e.Estado = N'Cancelada' THEN c.Fecha END,
           o.Salida, m.Metodo, vd.IdUsuario
    FROM Ocupacion o
    JOIN #Num k ON k.K < CEILING(o.Cap * o.Pct / 100.0)
    CROSS APPLY (SELECT (CAST(o.IdViaje AS BIGINT) * 1000 + k.K + 1) AS Semilla) sm
    CROSS APPLY (SELECT (sm.Semilla * 1103515245 + 12345) % 2147483647 AS S) h1
    CROSS APPLY (SELECT (h1.S * 1103515245 + 12345) % 2147483647 AS S) h2
    CROSS APPLY (SELECT (h2.S * 1103515245 + 12345) % 2147483647 AS S) h3
    CROSS APPLY (SELECT (h3.S * 1103515245 + 12345) % 2147483647 AS S) h4
    CROSS APPLY (SELECT (h4.S * 1103515245 + 12345) % 2147483647 AS S) h5
    CROSS APPLY (SELECT (h5.S * 1103515245 + 12345) % 2147483647 AS S) h6
    CROSS APPLY (SELECT (h6.S * 1103515245 + 12345) % 2147483647 AS S) h7
    CROSS APPLY (SELECT 30000000 + (o.IdViaje * 131 + k.K * 977) % 60000000 AS Num) pn
    CROSS APPLY (SELECT pn.Num, CAST(pn.Num AS NVARCHAR(8)) AS Dni) p
    CROSS APPLY (SELECT (h1.S / 4096) % 100 AS R) rt
    CROSS APPLY (SELECT CASE WHEN rt.R < 60 THEN N'General'
                             WHEN rt.R < 75 THEN N'Estudiante'
                             WHEN rt.R < 87 THEN N'Tercera Edad'
                             ELSE N'Niño' END AS Tipo,
                        CASE WHEN rt.R < 60 THEN 22 + p.Num % 34
                             WHEN rt.R < 75 THEN 17 + p.Num % 8
                             WHEN rt.R < 87 THEN 60 + p.Num % 22
                             ELSE 2 + p.Num % 10 END AS Edad) x
    CROSS APPLY (SELECT (h2.S / 4096) % 100 AS R) re
    CROSS APPLY (SELECT CASE WHEN o.Estado = N'Programado'
                             THEN CASE WHEN re.R < 78 THEN N'Confirmada'
                                       WHEN re.R < 96 THEN N'Pendiente' ELSE N'Cancelada' END
                             ELSE CASE WHEN re.R < 92 THEN N'Confirmada' ELSE N'Cancelada' END
                        END AS Estado) e
    CROSS APPLY (SELECT CASE WHEN o.Estado = N'Programado'
                             THEN DATEADD(HOUR, -(1 + (h3.S / 4096) % 140), @ahora)
                             ELSE DATEADD(HOUR, -CASE (h3.S / 4096) % 6 WHEN 0 THEN 6 WHEN 1 THEN 15 WHEN 2 THEN 20
                                                                      WHEN 3 THEN 30 WHEN 4 THEN 48 ELSE 72 END, o.Salida)
                        END AS Fecha) c
    CROSS APPLY (SELECT CASE WHEN o.Estado = N'Programado'
                             THEN DATEADD(HOUR, -(1 + (h5.S / 4096) % 144), @ahora)
                             ELSE DATEADD(HOUR, -(2 + (h5.S / 4096) % 216), o.Salida)
                        END AS Fecha) r
    CROSS APPLY (SELECT (h6.S / 4096) % 100 AS R) rm
    CROSS APPLY (SELECT CASE WHEN rm.R < 35 THEN N'Efectivo'
                             WHEN rm.R < 60 THEN N'Yape'
                             WHEN rm.R < 80 THEN N'Tarjeta'
                             WHEN rm.R < 90 THEN N'Plin'
                             ELSE N'Transferencia' END AS Metodo) m
    JOIN #Nombres nm ON nm.Idx = p.Num % 30
    JOIN #Apellidos a1 ON a1.Idx = (p.Num / 30) % 30
    JOIN #Apellidos a2 ON a2.Idx = (p.Num / 900) % 30
    JOIN #Vend vd ON vd.Idx = (h7.S / 4096) % @nVend
    JOIN dbo.Asiento a ON a.IdBus = o.IdBus
                      AND a.NumeroAsiento = RIGHT(N'0' + CAST((k.K * 7 + o.IdViaje) % o.Cap + 1 AS NVARCHAR(3)), 2)
    JOIN dbo.Tarifa t ON t.IdViaje = o.IdViaje AND t.TipoTarifa = x.Tipo;

    INSERT dbo.Reserva (CodigoReserva, IdUsuario, IdViaje, FechaReserva, Estado, Total)
    SELECT N'TMP-' + CAST(g.RowId AS NVARCHAR(10)), g.IdVendedor, g.IdViaje, g.FechaReserva, g.Estado, g.Precio
    FROM #Gen g;

    UPDATE g SET IdReserva = r.IdReserva
    FROM #Gen g JOIN dbo.Reserva r ON r.CodigoReserva = N'TMP-' + CAST(g.RowId AS NVARCHAR(10));

    UPDATE dbo.Reserva
    SET CodigoReserva = N'RES-' + RIGHT(N'000000' + CAST(IdReserva AS NVARCHAR(10)), 6)
    WHERE CodigoReserva LIKE N'TMP-%';

    INSERT dbo.Pasajero (IdReserva, Nombres, Apellidos, TipoDocumento, NroDocumento, FechaNacimiento, Telefono, Correo)
    SELECT g.IdReserva, g.Nombres, g.Apellidos, N'DNI', g.Dni, g.Nacimiento, N'9' + RIGHT(g.Dni, 8), g.CorreoPas
    FROM #Gen g;

    INSERT dbo.ReservaAsiento (IdReserva, IdAsiento, IdTarifa, Precio, Estado)
    SELECT g.IdReserva, g.IdAsiento, g.IdTarifa, g.Precio,
           CASE g.Estado WHEN N'Cancelada' THEN N'Cancelado' ELSE N'Reservado' END
    FROM #Gen g;

    INSERT dbo.Pago (IdReserva, FechaPago, Monto, MetodoPago, Referencia, Estado)
    SELECT g.IdReserva, DATEADD(MINUTE, 5, g.FechaReserva), g.Precio, g.Metodo,
           N'OP-' + RIGHT(N'000000' + CAST(g.RowId AS NVARCHAR(10)), 6),
           CASE g.Estado WHEN N'Pendiente' THEN N'Pendiente' ELSE N'Pagado' END
    FROM #Gen g;

    /* Cancelaciones: devolución según el tiempo que faltaba para la salida (24 h o más 100 %, 12 a 24 h 80 %, menos 0 %) */
    INSERT dbo.Cancelacion (IdReserva, FechaCancelacion, Motivo, MontoReembolso, Estado)
    SELECT g.IdReserva, g.FechaCancel,
           CASE WHEN g.H % 10 < 5 THEN N'Cambio de planes' WHEN g.H % 10 < 8 THEN N'Emergencia' ELSE N'Error en reserva' END,
           ROUND(g.Precio * x.Pct / 100.0, 2),
           CASE WHEN x.Pct > 0 THEN N'Reembolsado' ELSE N'Sin devolución' END
    FROM #Gen g
    CROSS APPLY (SELECT CASE WHEN DATEDIFF(HOUR, g.FechaCancel, g.Salida) >= 24 THEN 100
                             WHEN DATEDIFF(HOUR, g.FechaCancel, g.Salida) >= 12 THEN 80
                             ELSE 0 END AS Pct) x
    WHERE g.Estado = N'Cancelada';

    DROP TABLE #Gen, #Num, #Vend, #Apellidos, #Nombres, #NV, #Linea;
    COMMIT;
END;
GO

-- ---------------------------------------------------------------------------
-- Integridad, rendimiento y auditoría (se puede ejecutar varias veces)
-- Va al final para no estorbar la carga de los datos de ejemplo. Si una base
-- existente ya tiene datos que incumplen una regla, la regla se agrega solo para
-- los datos nuevos (WITH NOCHECK) y se muestra un aviso.
-- ---------------------------------------------------------------------------

-- 1. Valores permitidos en los estados y catálogos (antes eran texto libre)
DECLARE @reglas TABLE (Id INT IDENTITY(1,1), Tabla SYSNAME, Nombre SYSNAME, Condicion NVARCHAR(MAX));
INSERT @reglas (Tabla, Nombre, Condicion) VALUES
 (N'Rol',            N'CK_Rol_Estado',             N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Usuario',        N'CK_Usuario_Estado',         N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Usuario',        N'CK_Usuario_Intentos',       N'IntentosFallidos >= 0'),
 (N'Ciudad',         N'CK_Ciudad_Estado',          N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Empresa',        N'CK_Empresa_Estado',         N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Terminal',       N'CK_Terminal_Estado',        N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Ruta',           N'CK_Ruta_Estado',            N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Bus',            N'CK_Bus_Estado',             N'Estado IN (N''Operativo'', N''Mantenimiento'', N''Baja'')'),
 (N'Bus',            N'CK_Bus_Servicio',           N'Servicio IN (N''Ejecutivo'', N''Semi Cama'', N''Cama Suite'')'),
 (N'Asiento',        N'CK_Asiento_Estado',         N'Estado IN (N''Disponible'', N''Ocupado'')'),
 (N'Viaje',          N'CK_Viaje_Estado',           N'Estado IN (N''Programado'', N''En ruta'', N''Finalizado'', N''Cancelado'')'),
 (N'Viaje',          N'CK_Viaje_HorasReales',      N'FechaHoraSalidaReal IS NULL OR FechaHoraLlegadaReal IS NULL OR FechaHoraLlegadaReal > FechaHoraSalidaReal'),
 (N'Tarifa',         N'CK_Tarifa_Tipo',            N'TipoTarifa IN (N''General'', N''Estudiante'', N''Tercera Edad'', N''Niño'')'),
 (N'Tarifa',         N'CK_Tarifa_Estado',          N'Estado IN (N''Activo'', N''Inactivo'')'),
 (N'Reserva',        N'CK_Reserva_Estado',         N'Estado IN (N''Pendiente'', N''Confirmada'', N''Cancelada'')'),
 (N'Reserva',        N'CK_Reserva_Total',          N'Total >= 0'),
 (N'ReservaAsiento', N'CK_RA_Estado',              N'Estado IN (N''Reservado'', N''Cancelado'')'),
 (N'ReservaAsiento', N'CK_RA_Precio',              N'Precio >= 0'),
 (N'Pasajero',       N'CK_Pasajero_TipoDocumento', N'TipoDocumento IN (N''DNI'', N''Carné Extranjería'', N''Pasaporte'')'),
 (N'Pago',           N'CK_Pago_Estado',            N'Estado IN (N''Pendiente'', N''Pagado'', N''Anulado'')'),
 (N'Pago',           N'CK_Pago_Metodo',            N'MetodoPago IN (N''Efectivo'', N''Tarjeta'', N''Yape'', N''Plin'', N''Transferencia'')'),
 (N'Cancelacion',    N'CK_Cancelacion_Estado',     N'Estado IN (N''Procesada'', N''Reembolsado'', N''Sin devolución'')'),
 (N'Cancelacion',    N'CK_Cancelacion_Monto',      N'MontoReembolso >= 0');

DECLARE @i INT = 1, @n INT = (SELECT COUNT(*) FROM @reglas), @t SYSNAME, @nom SYSNAME, @cond NVARCHAR(MAX), @sql NVARCHAR(MAX), @malos INT;
WHILE @i <= @n
BEGIN
    SELECT @t = Tabla, @nom = Nombre, @cond = Condicion FROM @reglas WHERE Id = @i;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = @nom)
    BEGIN
        SET @sql = N'SELECT @m = COUNT(*) FROM dbo.' + QUOTENAME(@t) + N' WHERE NOT (' + @cond + N')';
        EXEC sp_executesql @sql, N'@m INT OUTPUT', @m = @malos OUTPUT;
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@t) + CASE WHEN @malos = 0 THEN N' WITH CHECK' ELSE N' WITH NOCHECK' END
                 + N' ADD CONSTRAINT ' + QUOTENAME(@nom) + N' CHECK (' + @cond + N')';
        EXEC (@sql);
        IF @malos > 0
            PRINT N'AVISO: ' + CAST(@malos AS NVARCHAR(10)) + N' fila(s) de ' + @t + N' incumplen ' + @nom + N'; la regla solo vale para los datos nuevos.';
    END;
    SET @i += 1;
END;
GO

-- 2. El origen y el destino de un viaje deben ser los de su ruta (antes podían quedar distintos)
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_Ruta_Id_Extremos')
    ALTER TABLE dbo.Ruta ADD CONSTRAINT UQ_Ruta_Id_Extremos UNIQUE (IdRuta, IdOrigen, IdDestino);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Viaje_Ruta_Extremos')
BEGIN
    DECLARE @incoherentes INT = (SELECT COUNT(*) FROM dbo.Viaje v JOIN dbo.Ruta r ON r.IdRuta = v.IdRuta
                                 WHERE v.IdOrigen <> r.IdOrigen OR v.IdDestino <> r.IdDestino);
    IF @incoherentes = 0
        ALTER TABLE dbo.Viaje WITH CHECK ADD CONSTRAINT FK_Viaje_Ruta_Extremos
            FOREIGN KEY (IdRuta, IdOrigen, IdDestino) REFERENCES dbo.Ruta (IdRuta, IdOrigen, IdDestino);
    ELSE
    BEGIN
        ALTER TABLE dbo.Viaje WITH NOCHECK ADD CONSTRAINT FK_Viaje_Ruta_Extremos
            FOREIGN KEY (IdRuta, IdOrigen, IdDestino) REFERENCES dbo.Ruta (IdRuta, IdOrigen, IdDestino);
        PRINT N'AVISO: ' + CAST(@incoherentes AS NVARCHAR(10)) + N' viaje(s) tienen un origen o destino distinto al de su ruta.';
    END;
END;
GO

-- 3. Índices en las llaves foráneas y en las columnas que más se filtran
DECLARE @idx TABLE (Nombre SYSNAME, Tabla SYSNAME, Columnas NVARCHAR(200));
INSERT @idx (Nombre, Tabla, Columnas) VALUES
 (N'IX_Terminal_Empresa',        N'Terminal',       N'IdEmpresa'),
 (N'IX_Bus_Empresa',             N'Bus',            N'IdEmpresa'),
 (N'IX_Usuario_Rol',             N'Usuario',        N'IdRol'),
 (N'IX_Ruta_Destino',            N'Ruta',           N'IdDestino'),
 (N'IX_Viaje_Ruta',              N'Viaje',          N'IdRuta'),
 (N'IX_Viaje_Bus_Fecha',         N'Viaje',          N'IdBus, FechaSalida'),
 (N'IX_Viaje_Destino',           N'Viaje',          N'IdDestino'),
 (N'IX_ReservaAsiento_Asiento',  N'ReservaAsiento', N'IdAsiento'),
 (N'IX_ReservaAsiento_Tarifa',   N'ReservaAsiento', N'IdTarifa'),
 (N'IX_Pasajero_Reserva',        N'Pasajero',       N'IdReserva'),
 (N'IX_Pasajero_Documento',      N'Pasajero',       N'NroDocumento, TipoDocumento'),
 (N'IX_Pago_Reserva',            N'Pago',           N'IdReserva'),
 (N'IX_Pago_Fecha',              N'Pago',           N'FechaPago'),
 (N'IX_Reserva_Estado_Fecha',    N'Reserva',        N'Estado, FechaReserva'),
 (N'IX_Cancelacion_Fecha',       N'Cancelacion',    N'FechaCancelacion');
DECLARE @j INT = 1, @m INT = (SELECT COUNT(*) FROM @idx), @s NVARCHAR(MAX);
DECLARE @lista TABLE (K INT IDENTITY(1,1), Nombre SYSNAME, Tabla SYSNAME, Columnas NVARCHAR(200));
INSERT @lista (Nombre, Tabla, Columnas) SELECT Nombre, Tabla, Columnas FROM @idx;
WHILE @j <= @m
BEGIN
    SELECT @s = N'CREATE INDEX ' + QUOTENAME(Nombre) + N' ON dbo.' + QUOTENAME(Tabla) + N' (' + Columnas + N')'
    FROM @lista WHERE K = @j AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = Nombre);
    IF @s IS NOT NULL EXEC (@s);
    SET @s = NULL; SET @j += 1;
END;
GO

-- 4. Un asiento no puede estar en dos reservas activas del mismo viaje, y lo garantiza la base de datos
--    (además de la transacción serializable de la aplicación): vista indexada con índice único.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON;
GO
IF OBJECT_ID(N'dbo.vw_AsientosOcupados') IS NULL
    EXEC (N'CREATE VIEW dbo.vw_AsientosOcupados WITH SCHEMABINDING AS
            SELECT r.IdViaje, ra.IdAsiento, ra.IdReservaAsiento
            FROM dbo.ReservaAsiento ra
            JOIN dbo.Reserva r ON r.IdReserva = ra.IdReserva
            WHERE ra.Estado <> N''Cancelado'' AND r.Estado <> N''Cancelada''');
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AsientosOcupados' AND object_id = OBJECT_ID(N'dbo.vw_AsientosOcupados'))
BEGIN TRY
    CREATE UNIQUE CLUSTERED INDEX UX_AsientosOcupados ON dbo.vw_AsientosOcupados (IdViaje, IdAsiento);
END TRY
BEGIN CATCH
    PRINT N'AVISO: no se pudo crear el índice único de asientos (hay asientos repetidos en reservas activas): ' + ERROR_MESSAGE();
END CATCH;
GO

-- 5. Auditoría: historial de cambios en las tablas sensibles
IF OBJECT_ID(N'dbo.Auditoria') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria (
        IdAuditoria BIGINT IDENTITY(1,1) CONSTRAINT PK_Auditoria PRIMARY KEY,
        Tabla       NVARCHAR(50)  NOT NULL,
        IdRegistro  INT           NOT NULL,
        Accion      NVARCHAR(10)  NOT NULL CONSTRAINT CK_Auditoria_Accion CHECK (Accion IN (N'INSERT', N'UPDATE', N'DELETE')),
        Detalle     NVARCHAR(500) NOT NULL CONSTRAINT DF_Auditoria_Detalle DEFAULT N'',
        UsuarioBD   NVARCHAR(128) NOT NULL CONSTRAINT DF_Auditoria_Usuario DEFAULT SUSER_SNAME(),
        Aplicacion  NVARCHAR(128) NOT NULL CONSTRAINT DF_Auditoria_App DEFAULT APP_NAME(),
        FechaHora   DATETIME2     NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_Auditoria_Registro ON dbo.Auditoria (Tabla, IdRegistro);
    CREATE INDEX IX_Auditoria_Fecha ON dbo.Auditoria (FechaHora);
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_Reserva_Auditoria ON dbo.Reserva AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria (Tabla, IdRegistro, Accion, Detalle)
    SELECT N'Reserva', i.IdReserva, N'INSERT', CONCAT(N'Código ', i.CodigoReserva, N'; estado ', i.Estado, N'; total ', i.Total)
    FROM inserted i WHERE NOT EXISTS (SELECT 1 FROM deleted d WHERE d.IdReserva = i.IdReserva)
    UNION ALL
    SELECT N'Reserva', i.IdReserva, N'UPDATE', CONCAT(N'Estado: ', d.Estado, N' -> ', i.Estado, N'; total: ', d.Total, N' -> ', i.Total)
    FROM inserted i JOIN deleted d ON d.IdReserva = i.IdReserva
    WHERE EXISTS (SELECT i.Estado, i.Total EXCEPT SELECT d.Estado, d.Total)
    UNION ALL
    SELECT N'Reserva', d.IdReserva, N'DELETE', CONCAT(N'Código ', d.CodigoReserva, N'; estado ', d.Estado)
    FROM deleted d WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.IdReserva = d.IdReserva);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Pago_Auditoria ON dbo.Pago AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria (Tabla, IdRegistro, Accion, Detalle)
    SELECT N'Pago', i.IdPago, N'INSERT', CONCAT(N'Reserva ', i.IdReserva, N'; ', i.MetodoPago, N'; monto ', i.Monto, N'; estado ', i.Estado)
    FROM inserted i WHERE NOT EXISTS (SELECT 1 FROM deleted d WHERE d.IdPago = i.IdPago)
    UNION ALL
    SELECT N'Pago', i.IdPago, N'UPDATE', CONCAT(N'Estado: ', d.Estado, N' -> ', i.Estado, N'; monto: ', d.Monto, N' -> ', i.Monto)
    FROM inserted i JOIN deleted d ON d.IdPago = i.IdPago
    WHERE EXISTS (SELECT i.Estado, i.Monto EXCEPT SELECT d.Estado, d.Monto)
    UNION ALL
    SELECT N'Pago', d.IdPago, N'DELETE', CONCAT(N'Reserva ', d.IdReserva, N'; monto ', d.Monto)
    FROM deleted d WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.IdPago = d.IdPago);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Cancelacion_Auditoria ON dbo.Cancelacion AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria (Tabla, IdRegistro, Accion, Detalle)
    SELECT N'Cancelacion', i.IdCancelacion, N'INSERT', CONCAT(N'Reserva ', i.IdReserva, N'; motivo ', i.Motivo, N'; reembolso ', i.MontoReembolso, N'; ', i.Estado)
    FROM inserted i WHERE NOT EXISTS (SELECT 1 FROM deleted d WHERE d.IdCancelacion = i.IdCancelacion)
    UNION ALL
    SELECT N'Cancelacion', i.IdCancelacion, N'UPDATE', CONCAT(N'Reembolso: ', d.MontoReembolso, N' -> ', i.MontoReembolso, N'; estado: ', d.Estado, N' -> ', i.Estado)
    FROM inserted i JOIN deleted d ON d.IdCancelacion = i.IdCancelacion
    WHERE EXISTS (SELECT i.MontoReembolso, i.Estado EXCEPT SELECT d.MontoReembolso, d.Estado)
    UNION ALL
    SELECT N'Cancelacion', d.IdCancelacion, N'DELETE', CONCAT(N'Reserva ', d.IdReserva)
    FROM deleted d WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.IdCancelacion = d.IdCancelacion);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Viaje_Auditoria ON dbo.Viaje AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria (Tabla, IdRegistro, Accion, Detalle)
    SELECT N'Viaje', i.IdViaje, N'INSERT', CONCAT(N'Ruta ', i.IdRuta, N'; bus ', i.IdBus, N'; chofer ', i.IdChofer, N'; salida ', i.FechaSalida, N' ', i.HoraSalida, N'; ', i.Estado)
    FROM inserted i WHERE NOT EXISTS (SELECT 1 FROM deleted d WHERE d.IdViaje = i.IdViaje)
    UNION ALL
    SELECT N'Viaje', i.IdViaje, N'UPDATE',
           CONCAT(CASE WHEN i.Estado <> d.Estado THEN CONCAT(N'Estado: ', d.Estado, N' -> ', i.Estado, N'. ') ELSE N'' END,
                  CASE WHEN i.IdBus <> d.IdBus THEN CONCAT(N'Bus: ', d.IdBus, N' -> ', i.IdBus, N'. ') ELSE N'' END,
                  CASE WHEN EXISTS (SELECT i.IdChofer EXCEPT SELECT d.IdChofer) THEN CONCAT(N'Chofer: ', d.IdChofer, N' -> ', i.IdChofer, N'. ') ELSE N'' END,
                  CASE WHEN i.FechaSalida <> d.FechaSalida OR i.HoraSalida <> d.HoraSalida THEN CONCAT(N'Salida: ', d.FechaSalida, N' ', d.HoraSalida, N' -> ', i.FechaSalida, N' ', i.HoraSalida, N'. ') ELSE N'' END,
                  CASE WHEN i.PrecioBase <> d.PrecioBase THEN CONCAT(N'Precio: ', d.PrecioBase, N' -> ', i.PrecioBase, N'. ') ELSE N'' END,
                  CASE WHEN EXISTS (SELECT i.FechaHoraSalidaReal EXCEPT SELECT d.FechaHoraSalidaReal) THEN N'Hora real de salida registrada. ' ELSE N'' END,
                  CASE WHEN EXISTS (SELECT i.FechaHoraLlegadaReal EXCEPT SELECT d.FechaHoraLlegadaReal) THEN N'Hora real de llegada registrada. ' ELSE N'' END)
    FROM inserted i JOIN deleted d ON d.IdViaje = i.IdViaje
    WHERE EXISTS (SELECT i.Estado, i.IdBus, i.IdChofer, i.FechaSalida, i.HoraSalida, i.PrecioBase, i.FechaHoraSalidaReal, i.FechaHoraLlegadaReal
                  EXCEPT SELECT d.Estado, d.IdBus, d.IdChofer, d.FechaSalida, d.HoraSalida, d.PrecioBase, d.FechaHoraSalidaReal, d.FechaHoraLlegadaReal)
    UNION ALL
    SELECT N'Viaje', d.IdViaje, N'DELETE', CONCAT(N'Ruta ', d.IdRuta, N'; salida ', d.FechaSalida, N' ', d.HoraSalida, N'; ', d.Estado)
    FROM deleted d WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.IdViaje = d.IdViaje);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_Usuario_Auditoria ON dbo.Usuario AFTER INSERT, UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Auditoria (Tabla, IdRegistro, Accion, Detalle)
    SELECT N'Usuario', i.IdUsuario, N'INSERT', CONCAT(N'Usuario ', i.NombreUsuario, N'; rol ', i.IdRol, N'; estado ', i.Estado)
    FROM inserted i WHERE NOT EXISTS (SELECT 1 FROM deleted d WHERE d.IdUsuario = i.IdUsuario)
    UNION ALL
    SELECT N'Usuario', i.IdUsuario, N'UPDATE',
           CONCAT(CASE WHEN i.Estado <> d.Estado THEN CONCAT(N'Estado: ', d.Estado, N' -> ', i.Estado, N'. ') ELSE N'' END,
                  CASE WHEN i.IdRol <> d.IdRol THEN CONCAT(N'Rol: ', d.IdRol, N' -> ', i.IdRol, N'. ') ELSE N'' END,
                  CASE WHEN EXISTS (SELECT i.ContrasenaHash EXCEPT SELECT d.ContrasenaHash) THEN N'Contraseña modificada. ' ELSE N'' END)
    FROM inserted i JOIN deleted d ON d.IdUsuario = i.IdUsuario
    WHERE EXISTS (SELECT i.Estado, i.IdRol, i.ContrasenaHash EXCEPT SELECT d.Estado, d.IdRol, d.ContrasenaHash)
    UNION ALL
    SELECT N'Usuario', d.IdUsuario, N'DELETE', CONCAT(N'Usuario ', d.NombreUsuario)
    FROM deleted d WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.IdUsuario = d.IdUsuario);
END;
GO

-- Comprobación
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
