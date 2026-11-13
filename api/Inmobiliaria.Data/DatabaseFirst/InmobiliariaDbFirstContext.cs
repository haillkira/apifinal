using System;
using System.Collections.Generic;
using Inmobiliaria.Data.Entities.DatabaseFirst;
using Microsoft.EntityFrameworkCore;

namespace Inmobiliaria.Data.DatabaseFirst;

public partial class InmobiliariaDbFirstContext : DbContext
{
    public InmobiliariaDbFirstContext(DbContextOptions<InmobiliariaDbFirstContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Administradores> Administradores { get; set; }

    public virtual DbSet<BusquedasGuardadas> BusquedasGuardadas { get; set; }

    public virtual DbSet<Caracteristicas> Caracteristicas { get; set; }

    public virtual DbSet<Compradores> Compradores { get; set; }

    public virtual DbSet<Contratos> Contratos { get; set; }

    public virtual DbSet<Conversaciones> Conversaciones { get; set; }

    public virtual DbSet<Favoritos> Favoritos { get; set; }

    public virtual DbSet<HistorialAcciones> HistorialAcciones { get; set; }

    public virtual DbSet<HistorialAprobaciones> HistorialAprobaciones { get; set; }

    public virtual DbSet<ImagenesPropiedades> ImagenesPropiedades { get; set; }

    public virtual DbSet<Mensajes> Mensajes { get; set; }

    public virtual DbSet<Notificaciones> Notificaciones { get; set; }

    public virtual DbSet<Pagos> Pagos { get; set; }

    public virtual DbSet<PropiedadCaracteristicas> PropiedadCaracteristicas { get; set; }

    public virtual DbSet<Propiedades> Propiedades { get; set; }

    public virtual DbSet<Provincias> Provincias { get; set; }

    public virtual DbSet<Publicaciones> Publicaciones { get; set; }

    public virtual DbSet<Reportes> Reportes { get; set; }

    public virtual DbSet<Reservaciones> Reservaciones { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<Solicitudes> Solicitudes { get; set; }

    public virtual DbSet<Suspensiones> Suspensiones { get; set; }

    public virtual DbSet<TiposPropiedad> TiposPropiedad { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    public virtual DbSet<Vendedores> Vendedores { get; set; }

    public virtual DbSet<Visitas> Visitas { get; set; }

    public virtual DbSet<Vista_PublicacionesDisponibles> Vista_PublicacionesDisponibles { get; set; }

    public virtual DbSet<Vista_ReportesPendientes> Vista_ReportesPendientes { get; set; }

    public virtual DbSet<Vista_SolicitudesVendedor> Vista_SolicitudesVendedor { get; set; }

    public virtual DbSet<Vista_VendedoresPendientes> Vista_VendedoresPendientes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administradores>(entity =>
        {
            entity.HasKey(e => e.IdAdministrador);

            entity.HasIndex(e => e.IdUsuario, "IX_Administradores_IdUsuario");

            entity.HasIndex(e => e.IdUsuario, "UX_Administradores_IdUsuario").IsUnique();

            entity.Property(e => e.NivelPermiso).HasMaxLength(20);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Administradores)
                .HasForeignKey<Administradores>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Administradores_IdUsuario");
        });

        modelBuilder.Entity<BusquedasGuardadas>(entity =>
        {
            entity.HasKey(e => e.IdBusqueda);

            entity.HasIndex(e => e.IdComprador, "IX_BusquedasGuardadas_IdComprador");

            entity.Property(e => e.FiltrosJson).HasMaxLength(4000);
            entity.Property(e => e.NombreBusqueda).HasMaxLength(100);

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.BusquedasGuardadas)
                .HasForeignKey(d => d.IdComprador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusquedasGuardadas_IdComprador");
        });

        modelBuilder.Entity<Caracteristicas>(entity =>
        {
            entity.HasKey(e => e.IdCaracteristica);

            entity.HasIndex(e => e.Nombre, "UX_Caracteristicas_Nombre").IsUnique();

            entity.Property(e => e.Categoria).HasMaxLength(50);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Compradores>(entity =>
        {
            entity.HasKey(e => e.IdComprador);

            entity.HasIndex(e => e.IdUsuario, "IX_Compradores_IdUsuario");

            entity.HasIndex(e => e.Cedula, "UX_Compradores_Cedula").IsUnique();

            entity.HasIndex(e => e.IdUsuario, "UX_Compradores_IdUsuario").IsUnique();

            entity.Property(e => e.Cedula).HasMaxLength(20);
            entity.Property(e => e.PreferenciaContacto).HasMaxLength(30);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.Compradores)
                .HasForeignKey<Compradores>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Compradores_IdUsuario");
        });

        modelBuilder.Entity<Contratos>(entity =>
        {
            entity.HasKey(e => e.IdContrato);

            entity.HasIndex(e => e.IdSolicitud, "IX_Contratos_IdSolicitud");

            entity.HasIndex(e => e.NumeroContrato, "UX_Contratos_NumeroContrato").IsUnique();

            entity.Property(e => e.DocumentoUrl).HasMaxLength(1000);
            entity.Property(e => e.EstadoContrato).HasMaxLength(20);
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.MontoAcordado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NumeroContrato).HasMaxLength(100);
            entity.Property(e => e.TipoContrato).HasMaxLength(20);

            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.Contratos)
                .HasForeignKey(d => d.IdSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contratos_IdSolicitud");
        });

        modelBuilder.Entity<Conversaciones>(entity =>
        {
            entity.HasKey(e => e.IdConversacion);

            entity.HasIndex(e => e.IdComprador, "IX_Conversaciones_IdComprador");

            entity.HasIndex(e => e.IdPublicacion, "IX_Conversaciones_IdPublicacion");

            entity.HasIndex(e => e.IdVendedor, "IX_Conversaciones_IdVendedor");

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.Conversaciones)
                .HasForeignKey(d => d.IdComprador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversaciones_IdComprador");

            entity.HasOne(d => d.IdPublicacionNavigation).WithMany(p => p.Conversaciones)
                .HasForeignKey(d => d.IdPublicacion)
                .HasConstraintName("FK_Conversaciones_IdPublicacion");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.Conversaciones)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversaciones_IdVendedor");
        });

        modelBuilder.Entity<Favoritos>(entity =>
        {
            entity.HasKey(e => new { e.IdComprador, e.IdPublicacion });

            entity.HasIndex(e => e.IdComprador, "IX_Favoritos_IdComprador");

            entity.HasIndex(e => e.IdPublicacion, "IX_Favoritos_IdPublicacion");

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdComprador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favoritos_IdComprador");

            entity.HasOne(d => d.IdPublicacionNavigation).WithMany(p => p.Favoritos)
                .HasForeignKey(d => d.IdPublicacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favoritos_IdPublicacion");
        });

        modelBuilder.Entity<HistorialAcciones>(entity =>
        {
            entity.HasKey(e => e.IdAccion);

            entity.HasIndex(e => e.IdUsuario, "IX_HistorialAcciones_IdUsuario");

            entity.Property(e => e.Accion).HasMaxLength(100);
            entity.Property(e => e.Detalles).HasMaxLength(4000);
            entity.Property(e => e.DireccionIP).HasMaxLength(64);
            entity.Property(e => e.TablaAfectada).HasMaxLength(100);

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.HistorialAcciones)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_HistorialAcciones_IdUsuario");
        });

        modelBuilder.Entity<HistorialAprobaciones>(entity =>
        {
            entity.HasKey(e => e.IdHistorial);

            entity.HasIndex(e => e.IdAdministrador, "IX_HistorialAprobaciones_IdAdministrador");

            entity.HasIndex(e => e.IdVendedor, "IX_HistorialAprobaciones_IdVendedor");

            entity.Property(e => e.Accion).HasMaxLength(20);
            entity.Property(e => e.Comentario).HasMaxLength(1000);

            entity.HasOne(d => d.IdAdministradorNavigation).WithMany(p => p.HistorialAprobaciones)
                .HasForeignKey(d => d.IdAdministrador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialAprobaciones_IdAdministrador");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.HistorialAprobaciones)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistorialAprobaciones_IdVendedor");
        });

        modelBuilder.Entity<ImagenesPropiedades>(entity =>
        {
            entity.HasKey(e => e.IdImagen);

            entity.HasIndex(e => e.IdPropiedad, "IX_ImagenesPropiedades_IdPropiedad");

            entity.HasIndex(e => e.IdPropiedad, "UX_ImagenPrincipal")
                .IsUnique()
                .HasFilter("([EsPrincipal]=(1))");

            entity.Property(e => e.TextoAlternativo).HasMaxLength(300);
            entity.Property(e => e.UrlImagen).HasMaxLength(1000);

            entity.HasOne(d => d.IdPropiedadNavigation).WithMany(p => p.ImagenesPropiedades)
                .HasForeignKey(d => d.IdPropiedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ImagenesPropiedades_IdPropiedad");
        });

        modelBuilder.Entity<Mensajes>(entity =>
        {
            entity.HasKey(e => e.IdMensaje);

            entity.HasIndex(e => e.IdConversacion, "IX_Mensajes_IdConversacion");

            entity.HasIndex(e => e.IdRemitente, "IX_Mensajes_IdRemitente");

            entity.Property(e => e.Contenido).HasMaxLength(4000);

            entity.HasOne(d => d.IdConversacionNavigation).WithMany(p => p.Mensajes)
                .HasForeignKey(d => d.IdConversacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mensajes_IdConversacion");

            entity.HasOne(d => d.IdRemitenteNavigation).WithMany(p => p.Mensajes)
                .HasForeignKey(d => d.IdRemitente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mensajes_IdRemitente");
        });

        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion);

            entity.HasIndex(e => e.IdUsuarioDestino, "IX_Notificaciones_IdUsuarioDestino");

            entity.Property(e => e.Mensaje).HasMaxLength(2000);
            entity.Property(e => e.TipoNotificacion).HasMaxLength(40);
            entity.Property(e => e.Titulo).HasMaxLength(150);
            entity.Property(e => e.UrlDestino).HasMaxLength(1000);

            entity.HasOne(d => d.IdUsuarioDestinoNavigation).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.IdUsuarioDestino)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notificaciones_IdUsuarioDestino");
        });

        modelBuilder.Entity<Pagos>(entity =>
        {
            entity.HasKey(e => e.IdPago);

            entity.HasIndex(e => e.IdComprador, "IX_Pagos_IdComprador");

            entity.HasIndex(e => e.IdReservacion, "IX_Pagos_IdReservacion");

            entity.HasIndex(e => e.IdSolicitud, "IX_Pagos_IdSolicitud");

            entity.Property(e => e.Concepto).HasMaxLength(200);
            entity.Property(e => e.EstadoPago).HasMaxLength(20);
            entity.Property(e => e.MetodoPago).HasMaxLength(30);
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenciaExterna).HasMaxLength(200);

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdComprador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_IdComprador");

            entity.HasOne(d => d.IdReservacionNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdReservacion)
                .HasConstraintName("FK_Pagos_IdReservacion");

            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdSolicitud)
                .HasConstraintName("FK_Pagos_IdSolicitud");
        });

        modelBuilder.Entity<PropiedadCaracteristicas>(entity =>
        {
            entity.HasKey(e => new { e.IdPropiedad, e.IdCaracteristica });

            entity.HasIndex(e => e.IdCaracteristica, "IX_PropiedadCaracteristicas_IdCaracteristica");

            entity.HasIndex(e => e.IdPropiedad, "IX_PropiedadCaracteristicas_IdPropiedad");

            entity.Property(e => e.Valor).HasMaxLength(200);

            entity.HasOne(d => d.IdCaracteristicaNavigation).WithMany(p => p.PropiedadCaracteristicas)
                .HasForeignKey(d => d.IdCaracteristica)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropiedadCaracteristicas_IdCaracteristica");

            entity.HasOne(d => d.IdPropiedadNavigation).WithMany(p => p.PropiedadCaracteristicas)
                .HasForeignKey(d => d.IdPropiedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PropiedadCaracteristicas_IdPropiedad");
        });

        modelBuilder.Entity<Propiedades>(entity =>
        {
            entity.HasKey(e => e.IdPropiedad);

            entity.HasIndex(e => e.IdProvincia, "IX_Propiedades_IdProvincia");

            entity.HasIndex(e => e.IdTipoPropiedad, "IX_Propiedades_IdTipoPropiedad");

            entity.HasIndex(e => e.IdVendedor, "IX_Propiedades_IdVendedor");

            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(4000);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.EstadoPropiedad).HasMaxLength(20);
            entity.Property(e => e.Latitud).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.MetrosCuadrados).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenciaDireccion).HasMaxLength(500);
            entity.Property(e => e.Sector).HasMaxLength(100);
            entity.Property(e => e.Titulo).HasMaxLength(150);

            entity.HasOne(d => d.IdProvinciaNavigation).WithMany(p => p.Propiedades)
                .HasForeignKey(d => d.IdProvincia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Propiedades_IdProvincia");

            entity.HasOne(d => d.IdTipoPropiedadNavigation).WithMany(p => p.Propiedades)
                .HasForeignKey(d => d.IdTipoPropiedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Propiedades_IdTipoPropiedad");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.Propiedades)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Propiedades_IdVendedor");
        });

        modelBuilder.Entity<Provincias>(entity =>
        {
            entity.HasKey(e => e.IdProvincia);

            entity.HasIndex(e => e.Nombre, "UX_Provincias_Nombre").IsUnique();

            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Publicaciones>(entity =>
        {
            entity.HasKey(e => e.IdPublicacion);

            entity.HasIndex(e => e.IdPropiedad, "IX_Publicaciones_IdPropiedad");

            entity.Property(e => e.Deposito).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EstadoPublicacion).HasMaxLength(20);
            entity.Property(e => e.MantenimientoMensual).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(1000);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoOperacion).HasMaxLength(20);

            entity.HasOne(d => d.IdPropiedadNavigation).WithMany(p => p.Publicaciones)
                .HasForeignKey(d => d.IdPropiedad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Publicaciones_IdPropiedad");
        });

        modelBuilder.Entity<Reportes>(entity =>
        {
            entity.HasKey(e => e.IdReporte);

            entity.HasIndex(e => e.IdPublicacion, "IX_Reportes_IdPublicacion");

            entity.HasIndex(e => e.IdUsuarioReporta, "IX_Reportes_IdUsuarioReporta");

            entity.HasIndex(e => e.IdUsuarioReportado, "IX_Reportes_IdUsuarioReportado");

            entity.HasIndex(e => e.ResueltoPor, "IX_Reportes_ResueltoPor");

            entity.Property(e => e.Categoria).HasMaxLength(40);
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.EstadoReporte).HasMaxLength(20);
            entity.Property(e => e.Resolucion).HasMaxLength(2000);

            entity.HasOne(d => d.IdPublicacionNavigation).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.IdPublicacion)
                .HasConstraintName("FK_Reportes_IdPublicacion");

            entity.HasOne(d => d.IdUsuarioReportaNavigation).WithMany(p => p.ReportesIdUsuarioReportaNavigation)
                .HasForeignKey(d => d.IdUsuarioReporta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reportes_IdUsuarioReporta");

            entity.HasOne(d => d.IdUsuarioReportadoNavigation).WithMany(p => p.ReportesIdUsuarioReportadoNavigation)
                .HasForeignKey(d => d.IdUsuarioReportado)
                .HasConstraintName("FK_Reportes_IdUsuarioReportado");

            entity.HasOne(d => d.ResueltoPorNavigation).WithMany(p => p.ReportesResueltoPorNavigation)
                .HasForeignKey(d => d.ResueltoPor)
                .HasConstraintName("FK_Reportes_ResueltoPor");
        });

        modelBuilder.Entity<Reservaciones>(entity =>
        {
            entity.HasKey(e => e.IdReservacion);

            entity.HasIndex(e => e.IdSolicitud, "IX_Reservaciones_IdSolicitud");

            entity.HasIndex(e => e.IdSolicitud, "UX_Reservaciones_IdSolicitud").IsUnique();

            entity.Property(e => e.EstadoReservacion).HasMaxLength(20);
            entity.Property(e => e.MontoReserva).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdSolicitudNavigation).WithOne(p => p.Reservaciones)
                .HasForeignKey<Reservaciones>(d => d.IdSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservaciones_IdSolicitud");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.IdRol);

            entity.HasIndex(e => e.Nombre, "UX_Roles_Nombre").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Solicitudes>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud);

            entity.HasIndex(e => e.IdComprador, "IX_Solicitudes_IdComprador");

            entity.HasIndex(e => e.IdPublicacion, "IX_Solicitudes_IdPublicacion");

            entity.Property(e => e.EstadoSolicitud).HasMaxLength(20);
            entity.Property(e => e.Mensaje).HasMaxLength(2000);
            entity.Property(e => e.TipoSolicitud).HasMaxLength(20);

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.Solicitudes)
                .HasForeignKey(d => d.IdComprador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Solicitudes_IdComprador");

            entity.HasOne(d => d.IdPublicacionNavigation).WithMany(p => p.Solicitudes)
                .HasForeignKey(d => d.IdPublicacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Solicitudes_IdPublicacion");
        });

        modelBuilder.Entity<Suspensiones>(entity =>
        {
            entity.HasKey(e => e.IdSuspension);

            entity.HasIndex(e => e.IdAdministrador, "IX_Suspensiones_IdAdministrador");

            entity.HasIndex(e => e.IdUsuario, "IX_Suspensiones_IdUsuario");

            entity.Property(e => e.Motivo).HasMaxLength(1000);

            entity.HasOne(d => d.IdAdministradorNavigation).WithMany(p => p.Suspensiones)
                .HasForeignKey(d => d.IdAdministrador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suspensiones_IdAdministrador");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Suspensiones)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Suspensiones_IdUsuario");
        });

        modelBuilder.Entity<TiposPropiedad>(entity =>
        {
            entity.HasKey(e => e.IdTipoPropiedad);

            entity.HasIndex(e => e.Nombre, "UX_TiposPropiedad_Nombre").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.HasIndex(e => e.IdRol, "IX_Usuarios_IdRol");

            entity.HasIndex(e => e.Correo, "UX_Usuarios_Correo").IsUnique();

            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.EstadoCuenta).HasMaxLength(20);
            entity.Property(e => e.FotoPerfilUrl).HasMaxLength(1000);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(1000);
            entity.Property(e => e.Telefono).HasMaxLength(30);

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_IdRol");
        });

        modelBuilder.Entity<Vendedores>(entity =>
        {
            entity.HasKey(e => e.IdVendedor);

            entity.HasIndex(e => e.IdUsuario, "IX_Vendedores_IdUsuario");

            entity.HasIndex(e => e.VerificadoPor, "IX_Vendedores_VerificadoPor");

            entity.HasIndex(e => e.Cedula, "UX_Vendedores_Cedula").IsUnique();

            entity.HasIndex(e => e.IdUsuario, "UX_Vendedores_IdUsuario").IsUnique();

            entity.Property(e => e.Cedula).HasMaxLength(20);
            entity.Property(e => e.EstadoAprobacion).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(1000);
            entity.Property(e => e.NombreComercial).HasMaxLength(150);
            entity.Property(e => e.NumeroLicencia).HasMaxLength(100);
            entity.Property(e => e.TipoVendedor).HasMaxLength(30);

            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.VendedoresIdUsuarioNavigation)
                .HasForeignKey<Vendedores>(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vendedores_IdUsuario");

            entity.HasOne(d => d.VerificadoPorNavigation).WithMany(p => p.VendedoresVerificadoPorNavigation)
                .HasForeignKey(d => d.VerificadoPor)
                .HasConstraintName("FK_Vendedores_VerificadoPor");
        });

        modelBuilder.Entity<Visitas>(entity =>
        {
            entity.HasKey(e => e.IdVisita);

            entity.HasIndex(e => e.IdSolicitud, "IX_Visitas_IdSolicitud");

            entity.Property(e => e.EstadoVisita).HasMaxLength(20);
            entity.Property(e => e.Notas).HasMaxLength(1000);

            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.Visitas)
                .HasForeignKey(d => d.IdSolicitud)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Visitas_IdSolicitud");
        });

        modelBuilder.Entity<Vista_PublicacionesDisponibles>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vista_PublicacionesDisponibles");

            entity.Property(e => e.Deposito).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EstadoPublicacion).HasMaxLength(20);
            entity.Property(e => e.MantenimientoMensual).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(1000);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoOperacion).HasMaxLength(20);
        });

        modelBuilder.Entity<Vista_ReportesPendientes>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vista_ReportesPendientes");

            entity.Property(e => e.Categoria).HasMaxLength(40);
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.EstadoReporte).HasMaxLength(20);
            entity.Property(e => e.IdReporte).ValueGeneratedOnAdd();
            entity.Property(e => e.Resolucion).HasMaxLength(2000);
        });

        modelBuilder.Entity<Vista_SolicitudesVendedor>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vista_SolicitudesVendedor");

            entity.Property(e => e.EstadoSolicitud).HasMaxLength(20);
            entity.Property(e => e.Mensaje).HasMaxLength(2000);
            entity.Property(e => e.TipoSolicitud).HasMaxLength(20);
        });

        modelBuilder.Entity<Vista_VendedoresPendientes>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Vista_VendedoresPendientes");

            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Cedula).HasMaxLength(20);
            entity.Property(e => e.Correo).HasMaxLength(254);
            entity.Property(e => e.EstadoAprobacion).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(1000);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.NombreComercial).HasMaxLength(150);
            entity.Property(e => e.NumeroLicencia).HasMaxLength(100);
            entity.Property(e => e.TipoVendedor).HasMaxLength(30);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
