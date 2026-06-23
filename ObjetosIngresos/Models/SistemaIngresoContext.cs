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
            entity.HasKey(e => e.IdCentro).HasName("PK_CentrosFormacion");
            entity.ToTable("CentrosFormacion");
            entity.Property(e => e.IdCentro).HasColumnName("id_centro");
            entity.Property(e => e.IdRegional).HasColumnName("id_regional");
            entity.Property(e => e.NombreCentro).HasMaxLength(150).HasColumnName("nombre_centro");
            entity.HasOne(d => d.IdRegionalNavigation).WithMany(p => p.CentrosFormacions)
                .HasForeignKey(d => d.IdRegional).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DetalleElemento>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK_DetalleElemento");
            entity.ToTable("DetalleElemento");
            entity.HasIndex(e => new { e.IdElemento, e.IdTipoDetalle }, "UQ_Elemento_Tipo").IsUnique();
            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");
            entity.HasOne(d => d.IdElementoNavigation).WithMany(p => p.DetalleElementos).HasForeignKey(d => d.IdElemento);
            entity.HasOne(d => d.IdTipoDetalleNavigation).WithMany(p => p.DetalleElementos).HasForeignKey(d => d.IdTipoDetalle);
        });

        modelBuilder.Entity<Elemento>(entity =>
        {
            entity.HasKey(e => e.IdElemento).HasName("PK_Elementos");
            entity.HasIndex(e => e.Serial, "UQ_Elementos_Serial").IsUnique();
            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.FotoArchivo).HasColumnName("foto_archivo");
            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Serial).HasMaxLength(50).HasColumnName("serial");
            entity.Property(e => e.TipoElemento).HasMaxLength(50).HasColumnName("tipo_elemento");
            entity.HasOne(d => d.IdMarcaNavigation).WithMany(p => p.Elementos).HasForeignKey(d => d.IdMarca);
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Elementos).HasForeignKey(d => d.IdUsuario);
        });

        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(e => e.IdMarca).HasName("PK_Marcas");
            entity.HasIndex(e => e.NombreMarca, "UQ_Marcas_Nombre").IsUnique();
            entity.Property(e => e.IdMarca).HasColumnName("id_marca");
            entity.Property(e => e.NombreMarca).HasMaxLength(100).HasColumnName("nombre_marca");
        });

        modelBuilder.Entity<MovimientoDetalle>(entity =>
        {
            entity.HasKey(e => e.IdMovimientoDetalle).HasName("PK_MovimientoDetalle");
            entity.ToTable("MovimientoDetalle");
            entity.HasIndex(e => new { e.IdMovimiento, e.IdTipoDetalle }, "UQ_Movimiento_Tipo").IsUnique();
            entity.Property(e => e.IdMovimientoDetalle).HasColumnName("id_movimiento_detalle");
            entity.Property(e => e.IdMovimiento).HasColumnName("id_movimiento");
            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");
            entity.Property(e => e.Presente).HasColumnName("presente");
            entity.HasOne(d => d.IdMovimientoNavigation).WithMany(p => p.MovimientoDetalles).HasForeignKey(d => d.IdMovimiento);
            entity.HasOne(d => d.IdTipoDetalleNavigation).WithMany(p => p.MovimientoDetalles).HasForeignKey(d => d.IdTipoDetalle);
        });

        modelBuilder.Entity<Regionale>(entity =>
        {
            entity.HasKey(e => e.IdRegional).HasName("PK_Regionales");
            entity.HasIndex(e => e.NombreRegional, "UQ_Regionales_Nombre").IsUnique();
            entity.Property(e => e.IdRegional).HasColumnName("id_regional");
            entity.Property(e => e.NombreRegional).HasMaxLength(100).HasColumnName("nombre_regional");
        });

        modelBuilder.Entity<RegistrosMovimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK_RegistrosMovimiento");
            entity.ToTable("RegistrosMovimiento");
            entity.Property(e => e.IdMovimiento).HasColumnName("id_movimiento");
             
            entity.Property(e => e.FechaEntrada)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("fecha_entrada");

            entity.Property(e => e.FechaSalida).HasColumnName("fecha_salida");
            entity.Property(e => e.IdElemento).HasColumnName("id_elemento");
            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.HasOne(d => d.IdElementoNavigation).WithMany(p => p.RegistrosMovimientos).HasForeignKey(d => d.IdElemento);
            entity.HasOne(d => d.IdSedeNavigation).WithMany(p => p.RegistrosMovimientos).HasForeignKey(d => d.IdSede);
        });

        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.IdSede).HasName("PK_Sedes");
            entity.Property(e => e.IdSede).HasColumnName("id_sede");
            entity.Property(e => e.Ciudad).HasMaxLength(100).HasColumnName("ciudad");
            entity.Property(e => e.IdCentro).HasColumnName("id_centro");
            entity.Property(e => e.NombreSede).HasMaxLength(150).HasColumnName("nombre_sede");
            entity.HasOne(d => d.IdCentroNavigation).WithMany(p => p.Sedes).HasForeignKey(d => d.IdCentro);
        });

        modelBuilder.Entity<TiposDetalle>(entity =>
        {
            entity.HasKey(e => e.IdTipoDetalle).HasName("PK_TiposDetalle");
            entity.ToTable("TiposDetalle");
            entity.HasIndex(e => e.Nombre, "UQ_TiposDetalle_Nombre").IsUnique();
            entity.Property(e => e.IdTipoDetalle).HasColumnName("id_tipo_detalle");
            entity.Property(e => e.Nombre).HasMaxLength(50).HasColumnName("nombre");
        });

        modelBuilder.Entity<TiposUsuario>(entity =>
        {
            entity.HasKey(e => e.IdTipoUsuario).HasName("PK_TiposUsuario");
            entity.ToTable("TiposUsuario");
            entity.Property(e => e.IdTipoUsuario).HasColumnName("id_tipo_usuario");
            entity.Property(e => e.Descripcion).HasMaxLength(50).HasColumnName("descripcion");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK_Usuarios");

            // 🛠️ ADAPTACIÓN POSTGRES: Filtro simplificado de índice parcial para Postgres
            entity.HasIndex(e => e.FirebaseUid, "UQ_Usuarios_FirebaseUid").HasFilter("firebase_uid IS NOT NULL");
            entity.HasIndex(e => e.Documento, "UQ_Usuarios_Documento").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Apellidos).HasMaxLength(100).HasColumnName("apellidos");
            entity.Property(e => e.Correo).HasMaxLength(150).HasColumnName("correo");
            entity.Property(e => e.Documento).HasMaxLength(20).HasColumnName("documento");
            entity.Property(e => e.Ficha).HasMaxLength(20).HasColumnName("ficha");
            entity.Property(e => e.FirebaseUid).HasMaxLength(128).HasColumnName("firebase_uid").IsRequired(false);
            entity.Property(e => e.IdSedePrincipal).HasColumnName("id_sede_principal");
            entity.Property(e => e.IdTipoUsuario).HasColumnName("id_tipo_usuario");
            entity.Property(e => e.Nombres).HasMaxLength(100).HasColumnName("nombres");
            entity.HasOne(d => d.IdSedePrincipalNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdSedePrincipal);
            entity.HasOne(d => d.IdTipoUsuarioNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdTipoUsuario);
        }); 
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        { 
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                entity.SetTableName(ConvertPascalToSnakeCase(tableName));
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }
     
    private string ConvertPascalToSnakeCase(string text)
    {
        if (text == "Regionale") return "regionales"; // Excepción rápida por pluralización
        if (text == "CentrosFormacion") return "centros_formacion";

        return string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}