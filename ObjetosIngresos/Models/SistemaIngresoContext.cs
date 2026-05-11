using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public partial class SistemaIngresoContext : DbContext
{
    public SistemaIngresoContext()
    {
    }

    public SistemaIngresoContext(DbContextOptions<SistemaIngresoContext> options)
        : base(options)
    {
    
    }
     

    public virtual DbSet<CentrosFormacion> CentrosFormacions { get; set; }

    public virtual DbSet<DetalleElemento> DetalleElementos { get; set; }

    public virtual DbSet<Elemento> Elementos { get; set; }

    public virtual DbSet<Marca> Marcas { get; set; }

    public virtual DbSet<MovimientoDetalle> MovimientoDetalles { get; set; }

    public virtual DbSet<Regionale> Regionales { get; set; }

    public virtual DbSet<RegistrosMovimiento> RegistrosMovimientos { get; set; }

    public virtual DbSet<Sede> Sedes { get; set; }

    public virtual DbSet<TiposDetalle> TiposDetalles { get; set; }

    public virtual DbSet<TiposUsuario> TiposUsuarios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CentrosFormacion>(entity =>
        {
            entity.HasKey(e => e.IdCentro).HasName("PK__CentrosF__7197C04FEAA19ABD");

            entity.ToTable("CentrosFormacion");

            entity.Property(e => e.IdCentro).HasColumnName("id_centro");
            entity.Property(e => e.IdRegional).HasColumnName("id_regional");
            entity.Property(e => e.NombreCentro)
                .HasMaxLength(150)
                .HasColumnName("nombre_centro");

            entity.HasOne(d => d.IdRegionalNavigation).WithMany(p => p.CentrosFormacions)
                .HasForeignKey(d => d.IdRegional)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Centro_Regional");
        });

        modelBuilder.Entity<DetalleElemento>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK__DetalleE__4F1332DE4873C1AD");

            entity.ToTable("DetalleElemento");

            entity.HasIndex(e => new { e.IdElemento, e.IdTipoDetalle }, "UQ_Elemento_Tipo").IsUnique();

            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");

            entity.HasOne(d => d.IdElementoNavigation).WithMany(p => p.DetalleElementos)
                .HasForeignKey(d => d.IdElemento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Detalle_Elemento");

            entity.HasOne(d => d.IdTipoDetalleNavigation).WithMany(p => p.DetalleElementos)
                .HasForeignKey(d => d.IdTipoDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Detalle_Tipo");
        });

        modelBuilder.Entity<Elemento>(entity =>
        {
            entity.HasKey(e => e.IdElemento).HasName("PK__Elemento__36C77A2811269813");

            entity.HasIndex(e => e.Serial, "UQ__Elemento__61787229956BD492").IsUnique();

            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.FotoArchivo).HasColumnName("foto_archivo");
            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Serial)
                .HasMaxLength(50)
                .HasColumnName("serial");
            entity.Property(e => e.TipoElemento)
                .HasMaxLength(50)
                .HasColumnName("tipo_elemento");

            entity.HasOne(d => d.IdMarcaNavigation).WithMany(p => p.Elementos)
                .HasForeignKey(d => d.IdMarca)
                .HasConstraintName("FK_Elemento_Marca");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Elementos)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_Elemento_Usuario");
        });

        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(e => e.IdMarca).HasName("PK__Marcas__7E43E99EF05ECDB5");

            entity.HasIndex(e => e.NombreMarca, "UQ__Marcas__6059F5729F8E4926").IsUnique();

            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.NombreMarca)
                .HasMaxLength(100)
                .HasColumnName("nombre_marca");
        });

        modelBuilder.Entity<MovimientoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdMovimientoDetalle).HasName("PK__Movimien__228C6E5CC965AC2F");

            entity.ToTable("MovimientoDetalle");

            entity.HasIndex(e => new { e.IdMovimiento, e.IdTipoDetalle }, "UQ_Movimiento_Tipo").IsUnique();

            entity.Property(e => e.IdMovimientoDetalle).HasColumnName("id_movimiento_detalle");
            entity.Property(e => e.IdMovimiento).HasColumnName("id_movimiento");
            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");
            entity.Property(e => e.Presente).HasColumnName("presente");

            entity.HasOne(d => d.IdMovimientoNavigation).WithMany(p => p.MovimientoDetalles)
                .HasForeignKey(d => d.IdMovimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovDetalle_Mov");

            entity.HasOne(d => d.IdTipoDetalleNavigation).WithMany(p => p.MovimientoDetalles)
                .HasForeignKey(d => d.IdTipoDetalle)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovDetalle_Tipo");
        });

        modelBuilder.Entity<Regionale>(entity =>
        {
            entity.HasKey(e => e.IdRegional).HasName("PK__Regional__0C17901A1852F23D");

            entity.HasIndex(e => e.NombreRegional, "UQ__Regional__B02D94277AEFD096").IsUnique();

            entity.Property(e => e.IdRegional).HasColumnName("id_regional");
            entity.Property(e => e.NombreRegional)
                .HasMaxLength(100)
                .HasColumnName("nombre_regional");
        });

        modelBuilder.Entity<RegistrosMovimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK__Registro__2A071C24C697EF5D");

            entity.ToTable("RegistrosMovimiento");

            entity.Property(e => e.IdMovimiento).HasColumnName("id_movimiento");
            entity.Property(e => e.FechaEntrada)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("fecha_entrada");
            entity.Property(e => e.FechaSalida).HasColumnName("fecha_salida");
            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");

            entity.HasOne(d => d.IdElementoNavigation).WithMany(p => p.RegistrosMovimientos)
                .HasForeignKey(d => d.IdElemento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimiento_Elemento");

            entity.HasOne(d => d.IdSedeNavigation).WithMany(p => p.RegistrosMovimientos)
                .HasForeignKey(d => d.IdSede)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimiento_Sede");
        });

        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.IdSede).HasName("PK__Sedes__D693504B2EBFE182");

            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.Ciudad)
                .HasMaxLength(100)
                .HasColumnName("ciudad");
            entity.Property(e => e.IdCentro).HasColumnName("id_centro");
            entity.Property(e => e.NombreSede)
                .HasMaxLength(150)
                .HasColumnName("nombre_sede");

            entity.HasOne(d => d.IdCentroNavigation).WithMany(p => p.Sedes)
                .HasForeignKey(d => d.IdCentro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sede_Centro");
        });

        modelBuilder.Entity<TiposDetalle>(entity =>
        {
            entity.HasKey(e => e.IdTipoDetalle).HasName("PK__TiposDet__6EFCC7FB23F4F199");

            entity.ToTable("TiposDetalle");

            entity.HasIndex(e => e.Nombre, "UQ__TiposDet__72AFBCC67B1F45BA").IsUnique();

            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposUsuario>(entity =>
        {
            entity.HasKey(e => e.IdTipoUsuario).HasName("PK__TiposUsu__B17D78C8D0436A52");

            entity.ToTable("TiposUsuario");

            entity.Property(e => e.IdTipoUsuario).HasColumnName("id_tipo_usuario");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(50)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuarios__4E3E04ADCDE9DC08");

            entity.HasIndex(e => e.FirebaseUid, "UQ__Usuarios__1E65B7F8DA562CF4").IsUnique();

            entity.HasIndex(e => e.Documento, "UQ__Usuarios__A25B3E614D270099").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");
            entity.Property(e => e.Documento)
                .HasMaxLength(20)
                .HasColumnName("documento");
            entity.Property(e => e.Ficha)
                .HasMaxLength(20)
                .HasColumnName("ficha");
            entity.Property(e => e.FirebaseUid)
                .HasMaxLength(128)
                .HasColumnName("firebase_uid");
            entity.Property(e => e.IdSedePrincipal).HasColumnName("id_sede_principal");
            entity.Property(e => e.IdTipoUsuario).HasColumnName("id_tipo_usuario");
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");

            entity.HasOne(d => d.IdSedePrincipalNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdSedePrincipal)
                .HasConstraintName("FK_Usuario_Sede");

            entity.HasOne(d => d.IdTipoUsuarioNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdTipoUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Tipo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
