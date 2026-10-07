# Chaski-Ruta · Sistema de Gestión de Viajes Interprovinciales

Sistema web para gestionar la venta de pasajes de una empresa de transporte interprovincial: programación de viajes, reservas con elección de asiento, cobros, cancelaciones con devolución, reportes y comprobantes.

Proyecto del curso **Diseño y Arquitectura de Software**.

---

## Contenido

1. [Qué hace el sistema](#qué-hace-el-sistema)
2. [Tecnologías](#tecnologías)
3. [Cómo ejecutarlo](#cómo-ejecutarlo)
4. [Cuentas de prueba](#cuentas-de-prueba)
5. [Reglas de negocio](#reglas-de-negocio)
6. [Estructura del proyecto](#estructura-del-proyecto)
7. [Base de datos](#base-de-datos)
8. [Problemas frecuentes](#problemas-frecuentes)
9. [Trabajo en equipo con Git](#trabajo-en-equipo-con-git)

---

## Qué hace el sistema

### Para el Vendedor
- **Reservas**: busca viajes por origen, destino y fecha, elige el asiento en un mapa y registra al pasajero. La tarifa se calcula por la edad (Niño, Tercera Edad o General).
- **Cobrar** una reserva (Efectivo, Tarjeta, Yape, Plin o Transferencia) y **cancelarla** con devolución según la política.
- **Ventas** y **Cancelaciones**: listados con filtros, detalle, **comprobante en PDF** y exportación a **Excel y PDF**.
- **Configuración**: editar su perfil y cambiar su contraseña.

### Para el Administrador (todo lo anterior, más)
- **Viajes**: programar, editar, cancelar y finalizar viajes. Las 4 tarifas se generan solas a partir del precio base, y el sistema evita que un bus tenga dos viajes a la vez.
- **Buses**: registrar la flota; los asientos se generan según la capacidad y los pisos.
- **Rutas** y **Ciudades**: catálogos con sus validaciones.
- **Usuarios**: crear cuentas, editar rol y datos, desactivar o reactivar, desbloquear y restablecer contraseñas.
- **Reportes**: ingresos, ocupación y pasajeros por ruta, con exportación a Excel y PDF.

### Seguridad
- Inicio de sesión con ID de usuario y contraseña (cifrada). Bloqueo de 15 minutos tras 5 intentos fallidos, con aviso de intentos restantes.
- Permisos por rol. Al desactivar una cuenta, cambiarle el rol o restablecer su contraseña, **sus sesiones abiertas se cierran**.
- Todos los formularios llevan protección antifalsificación (CSRF) y se validan en el navegador y en el servidor.
- El rol **Cliente** existe pero está **en espera**: aún no puede entrar al panel.

---

## Tecnologías

| Parte | Tecnología |
|---|---|
| Aplicación | ASP.NET Core MVC (.NET 10) |
| Datos | Entity Framework Core 10 + SQL Server (LocalDB) |
| Interfaz | Bootstrap 5, Font Awesome y Chart.js (cargados desde internet) |
| PDF | [QuestPDF](https://www.questpdf.com) (licencia Community) |
| Excel | [ClosedXML](https://github.com/ClosedXML/ClosedXML) |

---

## Cómo ejecutarlo

### Requisitos (una sola vez)
- **Visual Studio** reciente (2022 versión 17.13 o superior, o 2026) con las cargas de trabajo:
  - **ASP.NET y desarrollo web**
  - **Almacenamiento y procesamiento de datos** (incluye SQL Server LocalDB)
- **.NET 10** (lo instala Visual Studio, o el SDK desde <https://dotnet.microsoft.com>)
- **Conexión a internet** en la primera compilación (se descargan los paquetes NuGet) y al usar el sistema (estilos e iconos).

### 1. Descargar el proyecto
En Visual Studio: **Clonar un repositorio** y pegar la dirección del repositorio. O desde una terminal:

```bash
git clone https://github.com/EduardoZA03/Proyecto-Sistemas-de-Gesti-n-de-Viajes-Interprovinciales
cd Proyecto-Sistemas-de-Gesti-n-de-Viajes-Interprovinciales
```

### 2. Crear la base de datos
Desde la carpeta del repositorio:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -f 65001 -i Database\ChaskiRuta.sql
```

Si LocalDB no está iniciado: `sqllocaldb start MSSQLLocalDB`.
También puede abrirse `Database/ChaskiRuta.sql` en SQL Server Management Studio, conectarse a `(localdb)\MSSQLLocalDB` y ejecutarlo.

El script **se puede volver a ejecutar sin riesgo**: no duplica datos y agrega las columnas que falten.

### 3. Ejecutar
Abrir `WebApplication1\WebApplication1.slnx` en Visual Studio y pulsar **F5**.
Se abre el navegador en la pantalla de inicio de sesión.

> La cadena de conexión está en `WebApplication1/WebApplication1/appsettings.json`
> (`Server=(localdb)\MSSQLLocalDB;Database=ChaskiRuta;...`). Si usas otro SQL Server, cámbiala ahí.

### 4. Recibir cambios del equipo
```bash
git pull origin main
```
Después de un `pull`, vuelve a ejecutar el script de la base de datos **solo si** la actualización cambió las tablas.

---

## Cuentas de prueba

Al crear la base se generan tres cuentas. Se entra con el **ID de usuario** (no con el correo):

| ID de usuario | Rol | Acceso |
|---|---|---|
| `admin` | Administrador | Todo el sistema |
| `vendedor` | Vendedor | Reservas, ventas, cancelaciones y configuración |
| `cliente` | Cliente | Sin acceso (rol en espera) |

Las contraseñas iniciales se asignan **solo en modo desarrollo** al arrancar la aplicación; están definidas en `Data/SembrarUsuarios.cs`.

> **Importante**: son contraseñas de prueba para desarrollo. Si el sistema se publica en un servidor real, deben cambiarse y no se asignarán solas (ver *Reglas de negocio → Producción*).

El Administrador puede crear más cuentas desde **Usuarios → Nueva cuenta**.

---

## Reglas de negocio

**Tarifas.** Cada viaje tiene 4 tarifas calculadas sobre el precio base: General (100 %), Estudiante (80 %), Tercera Edad (70 %) y Niño (50 %). Al reservar se elige por edad: menor de 12 años = Niño, 60 o más = Tercera Edad, el resto General.

**Estados de una reserva.** `Pendiente` → (cobrar) → `Confirmada` → (cancelar) → `Cancelada`. Un asiento reservado, aunque esté pendiente de pago, cuenta como ocupado. Al cancelar queda libre otra vez.

**Política de cancelación.** Se devuelve un porcentaje de lo pagado según el tiempo que falta para la salida:

| Tiempo antes del viaje | Devolución |
|---|---|
| 24 horas o más | 100 % |
| Entre 12 y 24 horas | 80 % |
| Menos de 12 horas | 0 % |

**Restricciones de integridad.**
- Un viaje con reservas activas no se edita ni se cancela hasta cancelar esas reservas.
- Un bus con viajes no cambia su cantidad de asientos ni pasa a mantenimiento si tiene viajes por salir.
- Una ruta o ciudad con viajes no se elimina (se desactiva).
- La misma persona no puede tener dos reservas activas en el mismo viaje.
- Las cuentas no se eliminan, solo se desactivan, para conservar el historial.
- Siempre debe quedar al menos un administrador activo.

**Producción.** La aplicación asigna contraseñas iniciales solo cuando corre en modo `Development`. Para un servidor real hay que crear el administrador inicial de forma segura, ajustar la zona horaria a la de Perú (el sistema usa la hora del servidor) y adaptar el script de base de datos a ese servidor.

---

## Estructura del proyecto

```
.
├── Database/
│   └── ChaskiRuta.sql              Script de la base de datos (tablas, datos de ejemplo)
├── WebApplication1/
│   ├── WebApplication1.slnx        Solución de Visual Studio
│   └── WebApplication1/            Proyecto ASP.NET Core MVC
│       ├── Controllers/            Lógica de cada pantalla
│       ├── Models/                 Entidades y ViewModels
│       ├── Views/                  Pantallas (Razor)
│       ├── Data/                   Contexto de base de datos, seguridad y reglas compartidas
│       │   └── Exportacion/        Generación de PDF, Excel y comprobantes
│       ├── Components/             Componente del encabezado
│       ├── wwwroot/                Estilos, imágenes y scripts
│       ├── Program.cs              Configuración de la aplicación
│       └── appsettings.json        Cadena de conexión
└── README.md
```

---

## Base de datos

La base `ChaskiRuta` tiene 15 tablas:

| Grupo | Tablas |
|---|---|
| Usuarios | `Rol`, `Usuario` |
| Empresa | `Empresa`, `Terminal`, `Bus`, `Asiento` |
| Rutas y viajes | `Ciudad`, `Ruta`, `Viaje`, `Tarifa` |
| Ventas | `Reserva`, `ReservaAsiento`, `Pasajero`, `Pago`, `Cancelacion` |

El script incluye **datos de ejemplo**: 3 usuarios, 8 ciudades, 8 rutas, 3 buses (120 asientos), viajes, 13 reservas con sus pagos y 3 cancelaciones.

> Las fechas de los viajes de ejemplo se calculan respecto al día en que se crea la base, así que **con los días van quedando en el pasado**. Para seguir probando, el Administrador puede programar viajes nuevos desde **Viajes → Nuevo viaje** (y crear rutas o buses si hacen falta).

---

## Problemas frecuentes

| Síntoma | Qué hacer |
|---|---|
| `Cannot open database "ChaskiRuta"` | La base no existe en tu equipo: ejecuta el script del paso 2. |
| *Error 50* / "Error occurred during LocalDB instance startup" | LocalDB no pudo arrancar. Ejecuta `sqllocaldb stop MSSQLLocalDB -k` y luego `sqllocaldb start MSSQLLocalDB`. Cierra otros programas que usen la misma instancia. |
| No me deja entrar con `admin` | Revisa que ejecutaste el script y que arrancaste la aplicación en modo desarrollo (F5). Las contraseñas iniciales se asignan al arrancar. |
| La página se ve sin estilos | Falta internet: Bootstrap, los iconos y los gráficos se cargan desde internet. |
| Error al compilar por paquetes | Falta internet en la primera compilación (NuGet). Vuelve a compilar con conexión. |
| Aparece "No hay viajes" al reservar | Los viajes de ejemplo ya pasaron. Programa viajes nuevos como Administrador. |
| Al actualizar con `git pull` falla la página | Ejecuta de nuevo `Database/ChaskiRuta.sql`: la actualización puede haber agregado columnas. |

---

## Trabajo en equipo con Git

- La rama `main` tiene la versión estable. Cada integrante trabaja en su propia rama (por ejemplo `feature/programacion-...`).
- Los cambios llegan a `main` mediante **Pull Request**.
- Antes de empezar a trabajar: `git pull origin main`.
- Al terminar: **Confirmar** (commit) con un mensaje claro y luego **Insertar** (push). Sin el commit no se sube nada nuevo.

---

## Licencias de terceros
- **QuestPDF** se usa bajo la licencia *Community*, gratuita para proyectos académicos y pequeños.
- Bootstrap, Font Awesome, Chart.js y ClosedXML son de código abierto.
