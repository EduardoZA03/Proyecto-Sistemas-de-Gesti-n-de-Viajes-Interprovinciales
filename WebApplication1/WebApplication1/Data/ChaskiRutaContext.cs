using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    // Contexto "database first": las tablas se crean con Database/ChaskiRuta.sql
    public class ChaskiRutaContext : DbContext
    {
        public ChaskiRutaContext(DbContextOptions<ChaskiRutaContext> options) : base(options) { }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Ciudad> Ciudades => Set<Ciudad>();
        public DbSet<Empresa> Empresas => Set<Empresa>();
        public DbSet<Terminal> Terminales => Set<Terminal>();
        public DbSet<Bus> Buses => Set<Bus>();
        public DbSet<Asiento> Asientos => Set<Asiento>();
        public DbSet<Ruta> Rutas => Set<Ruta>();
        public DbSet<Viaje> Viajes => Set<Viaje>();
        public DbSet<Tarifa> Tarifas => Set<Tarifa>();
        public DbSet<Reserva> Reservas => Set<Reserva>();
        public DbSet<ReservaAsiento> ReservaAsientos => Set<ReservaAsiento>();
        public DbSet<Pasajero> Pasajeros => Set<Pasajero>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<Cancelacion> Cancelaciones => Set<Cancelacion>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // Las tablas están en singular
            mb.Entity<Rol>().ToTable("Rol");
            mb.Entity<Usuario>().ToTable("Usuario");
            mb.Entity<Ciudad>().ToTable("Ciudad");
            mb.Entity<Empresa>().ToTable("Empresa");
            mb.Entity<Terminal>().ToTable("Terminal");
            mb.Entity<Bus>().ToTable("Bus");
            mb.Entity<Asiento>().ToTable("Asiento");
            mb.Entity<Ruta>().ToTable("Ruta");
            mb.Entity<Viaje>().ToTable("Viaje");
            mb.Entity<Tarifa>().ToTable("Tarifa");
            mb.Entity<Reserva>().ToTable("Reserva");
            mb.Entity<ReservaAsiento>().ToTable("ReservaAsiento");
            mb.Entity<Pasajero>().ToTable("Pasajero");
            mb.Entity<Pago>().ToTable("Pago");
            mb.Entity<Cancelacion>().ToTable("Cancelacion");

            // Claves primarias (se llaman IdXxx, EF no las detecta solo)
            mb.Entity<Rol>().HasKey(e => e.IdRol);
            mb.Entity<Usuario>().HasKey(e => e.IdUsuario);
            mb.Entity<Ciudad>().HasKey(e => e.IdCiudad);
            mb.Entity<Empresa>().HasKey(e => e.IdEmpresa);
            mb.Entity<Terminal>().HasKey(e => e.IdTerminal);
            mb.Entity<Bus>().HasKey(e => e.IdBus);
            mb.Entity<Asiento>().HasKey(e => e.IdAsiento);
            mb.Entity<Ruta>().HasKey(e => e.IdRuta);
            mb.Entity<Viaje>().HasKey(e => e.IdViaje);
            mb.Entity<Tarifa>().HasKey(e => e.IdTarifa);
            mb.Entity<Reserva>().HasKey(e => e.IdReserva);
            mb.Entity<ReservaAsiento>().HasKey(e => e.IdReservaAsiento);
            mb.Entity<Pasajero>().HasKey(e => e.IdPasajero);
            mb.Entity<Pago>().HasKey(e => e.IdPago);
            mb.Entity<Cancelacion>().HasKey(e => e.IdCancelacion);

            // Claves foráneas (mismo ON DELETE que Database/ChaskiRuta.sql)
            mb.Entity<Usuario>().HasOne(e => e.Rol).WithMany(r => r.Usuarios)
                .HasForeignKey(e => e.IdRol).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Terminal>().HasOne(e => e.Empresa).WithMany(r => r.Terminales)
                .HasForeignKey(e => e.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Bus>().HasOne(e => e.Empresa).WithMany(r => r.Buses)
                .HasForeignKey(e => e.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Asiento>().HasOne(e => e.Bus).WithMany(r => r.Asientos)
                .HasForeignKey(e => e.IdBus).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<Viaje>().HasOne(e => e.Ruta).WithMany(r => r.Viajes)
                .HasForeignKey(e => e.IdRuta).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Viaje>().HasOne(e => e.Bus).WithMany(r => r.Viajes)
                .HasForeignKey(e => e.IdBus).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Tarifa>().HasOne(e => e.Viaje).WithMany(r => r.Tarifas)
                .HasForeignKey(e => e.IdViaje).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<Reserva>().HasOne(e => e.Usuario).WithMany(r => r.Reservas)
                .HasForeignKey(e => e.IdUsuario).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Reserva>().HasOne(e => e.Viaje).WithMany(r => r.Reservas)
                .HasForeignKey(e => e.IdViaje).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<ReservaAsiento>().HasOne(e => e.Reserva).WithMany(r => r.ReservaAsientos)
                .HasForeignKey(e => e.IdReserva).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<ReservaAsiento>().HasOne(e => e.Asiento).WithMany()
                .HasForeignKey(e => e.IdAsiento).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<ReservaAsiento>().HasOne(e => e.Tarifa).WithMany()
                .HasForeignKey(e => e.IdTarifa).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Pasajero>().HasOne(e => e.Reserva).WithMany(r => r.Pasajeros)
                .HasForeignKey(e => e.IdReserva).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<Pago>().HasOne(e => e.Reserva).WithMany(r => r.Pagos)
                .HasForeignKey(e => e.IdReserva).OnDelete(DeleteBehavior.Restrict);

            // Ruta -> Ciudad (dos relaciones a la misma tabla)
            mb.Entity<Ruta>().HasOne(r => r.Origen).WithMany()
                .HasForeignKey(r => r.IdOrigen).OnDelete(DeleteBehavior.Restrict);
            mb.Entity<Ruta>().HasOne(r => r.Destino).WithMany()
                .HasForeignKey(r => r.IdDestino).OnDelete(DeleteBehavior.Restrict);

            // Terminal.RutasOrigen no tiene columna en la base de datos
            mb.Entity<Terminal>().Ignore(t => t.RutasOrigen);

            // Reserva 1 - 0..1 Cancelacion
            mb.Entity<Reserva>().HasOne(r => r.Cancelacion).WithOne(c => c.Reserva)
                .HasForeignKey<Cancelacion>(c => c.IdReserva).OnDelete(DeleteBehavior.Restrict);

            // Tipos de columna
            mb.Entity<Viaje>().Property(v => v.FechaSalida).HasColumnType("date");
            mb.Entity<Viaje>().Property(v => v.HoraSalida).HasColumnType("time(0)");
            mb.Entity<Pasajero>().Property(p => p.FechaNacimiento).HasColumnType("date");

            mb.Entity<Viaje>().Property(v => v.PrecioBase).HasPrecision(10, 2);
            mb.Entity<Tarifa>().Property(t => t.Precio).HasPrecision(10, 2);
            mb.Entity<Reserva>().Property(r => r.Total).HasPrecision(10, 2);
            mb.Entity<ReservaAsiento>().Property(r => r.Precio).HasPrecision(10, 2);
            mb.Entity<Pago>().Property(p => p.Monto).HasPrecision(10, 2);
            mb.Entity<Cancelacion>().Property(c => c.MontoReembolso).HasPrecision(10, 2);
        }
    }
}
