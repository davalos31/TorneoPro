using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TorneoPro.API.Models;

namespace TorneoPro.API.Data;

public partial class TorneoProContext : DbContext
{
    public TorneoProContext(DbContextOptions<TorneoProContext> options)
        : base(options)
    {
    }

    public virtual DbSet<actas_partido> actas_partidos { get; set; }

    public virtual DbSet<auditoria_acceso> auditoria_accesos { get; set; }

    public virtual DbSet<cancha> canchas { get; set; }

    public virtual DbSet<canchas_disponibilidad> canchas_disponibilidads { get; set; }

    public virtual DbSet<credenciales_jugadore> credenciales_jugadores { get; set; }

    public virtual DbSet<deporte> deportes { get; set; }

    public virtual DbSet<enlaces_compartido> enlaces_compartidos { get; set; }

    public virtual DbSet<enlaces_historial_uso> enlaces_historial_usos { get; set; }

    public virtual DbSet<equipo> equipos { get; set; }

    public virtual DbSet<equipos_torneo> equipos_torneos { get; set; }

    public virtual DbSet<estadisticas_equipos_torneo> estadisticas_equipos_torneos { get; set; }

    public virtual DbSet<estadisticas_jugadore> estadisticas_jugadores { get; set; }

    public virtual DbSet<fases_torneo_catalogo> fases_torneo_catalogos { get; set; }

    public virtual DbSet<fechas_bloqueada> fechas_bloqueadas { get; set; }

    public virtual DbSet<ia_recomendacione> ia_recomendaciones { get; set; }

    public virtual DbSet<jornadas_imagenes_sociale> jornadas_imagenes_sociales { get; set; }

    public virtual DbSet<jugadores_equipo> jugadores_equipos { get; set; }

    public virtual DbSet<jugadores_suspensione> jugadores_suspensiones { get; set; }

    public virtual DbSet<metodos_pago> metodos_pagos { get; set; }

    public virtual DbSet<motivos_reprogramacion> motivos_reprogramacions { get; set; }

    public virtual DbSet<multa> multas { get; set; }

    public virtual DbSet<multas_historial_pago> multas_historial_pagos { get; set; }

    public virtual DbSet<notificacione> notificaciones { get; set; }

    public virtual DbSet<partido> partidos { get; set; }

    public virtual DbSet<partidos_asistencium> partidos_asistencia { get; set; }

    public virtual DbSet<partidos_evento> partidos_eventos { get; set; }

    public virtual DbSet<partidos_observacione> partidos_observaciones { get; set; }

    public virtual DbSet<partidos_reprogramacione> partidos_reprogramaciones { get; set; }

    public virtual DbSet<penales_detalle> penales_detalles { get; set; }

    public virtual DbSet<penales_tandum> penales_tanda { get; set; }

    public virtual DbSet<preferencias_notificacione> preferencias_notificaciones { get; set; }

    public virtual DbSet<solicitudes_equipo> solicitudes_equipos { get; set; }

    public virtual DbSet<tipos_documento> tipos_documentos { get; set; }

    public virtual DbSet<tipos_enlace> tipos_enlaces { get; set; }

    public virtual DbSet<tipos_formato_torneo> tipos_formato_torneos { get; set; }

    public virtual DbSet<tipos_multum> tipos_multa { get; set; }

    public virtual DbSet<tipos_notificacion> tipos_notificacions { get; set; }

    public virtual DbSet<tipos_rol> tipos_rols { get; set; }

    public virtual DbSet<tipos_superficie> tipos_superficies { get; set; }

    public virtual DbSet<tipos_usuario> tipos_usuarios { get; set; }

    public virtual DbSet<tokens_recuperacion> tokens_recuperacions { get; set; }

    public virtual DbSet<torneo> torneos { get; set; }

    public virtual DbSet<torneos_config_eliminatorium> torneos_config_eliminatoria { get; set; }

    public virtual DbSet<torneos_config_grupos_eliminatorium> torneos_config_grupos_eliminatoria { get; set; }

    public virtual DbSet<torneos_config_horarios_liga> torneos_config_horarios_ligas { get; set; }

    public virtual DbSet<torneos_config_liga> torneos_config_ligas { get; set; }

    public virtual DbSet<torneos_config_multa> torneos_config_multas { get; set; }

    public virtual DbSet<torneos_fase> torneos_fases { get; set; }

    public virtual DbSet<torneos_historial_clonacion> torneos_historial_clonacions { get; set; }

    public virtual DbSet<torneos_llafe> torneos_llaves { get; set; }

    public virtual DbSet<torneos_sembrado> torneos_sembrados { get; set; }

    public virtual DbSet<usuario> usuarios { get; set; }

    public virtual DbSet<usuarios_role> usuarios_roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<actas_partido>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__actas_pa__3213E83F85870FA9");

            entity.HasIndex(e => e.codigo, "UQ__actas_pa__40F9A2068B8208F5").IsUnique();

            entity.HasIndex(e => e.id_partido, "UQ__actas_pa__42D83E450DA2DEB5").IsUnique();

            entity.HasIndex(e => e.estado, "idx_actas_estado");

            entity.HasIndex(e => e.id_partido, "idx_actas_partido");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("BORRADOR");
            entity.Property(e => e.fecha_generacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.numero_acta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.pdf_generado).HasDefaultValue(false);

            entity.HasOne(d => d.id_arbitro_firmanteNavigation).WithMany(p => p.actas_partidoid_arbitro_firmanteNavigations)
                .HasForeignKey(d => d.id_arbitro_firmante)
                .HasConstraintName("FK__actas_par__id_ar__351DDF8C");

            entity.HasOne(d => d.id_partidoNavigation).WithOne(p => p.actas_partido)
                .HasForeignKey<actas_partido>(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__actas_par__id_pa__3335971A");

            entity.HasOne(d => d.id_usuario_creacionNavigation).WithMany(p => p.actas_partidoid_usuario_creacionNavigations)
                .HasForeignKey(d => d.id_usuario_creacion)
                .HasConstraintName("FK__actas_par__id_us__37FA4C37");
        });

        modelBuilder.Entity<auditoria_acceso>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__auditori__3213E83FEC51E019");

            entity.Property(e => e.accion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.exitoso).HasDefaultValue(true);
            entity.Property(e => e.fecha_accion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ip_address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.tabla_afectada)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.auditoria_accesos)
                .HasForeignKey(d => d.id_usuario)
                .HasConstraintName("FK__auditoria__id_us__6A50C1DA");
        });

        modelBuilder.Entity<cancha>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__canchas__3213E83FCD96A2A3");

            entity.HasIndex(e => e.codigo, "UQ__canchas__40F9A20639796977").IsUnique();

            entity.HasIndex(e => e.estado, "idx_canchas_estado");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.ciudad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("DISPONIBLE");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.latitud).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.longitud).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.nombre_corto)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.pais)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.tiene_estacionamiento).HasDefaultValue(false);
            entity.Property(e => e.tiene_iluminacion).HasDefaultValue(false);
            entity.Property(e => e.tiene_vestuarios).HasDefaultValue(false);

            entity.HasOne(d => d.id_tipo_superficieNavigation).WithMany(p => p.canchas)
                .HasForeignKey(d => d.id_tipo_superficie)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__canchas__id_tipo__71D1E811");
        });

        modelBuilder.Entity<canchas_disponibilidad>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__canchas___3213E83FB7A32A18");

            entity.ToTable("canchas_disponibilidad");

            entity.HasIndex(e => e.codigo, "UQ__canchas___40F9A206C20B2872").IsUnique();

            entity.HasIndex(e => new { e.id_cancha, e.fecha_hora_inicio }, "idx_canchas_disp");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.disponible).HasDefaultValue(true);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_canchaNavigation).WithMany(p => p.canchas_disponibilidads)
                .HasForeignKey(d => d.id_cancha)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__canchas_d__id_ca__5B0E7E4A");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.canchas_disponibilidads)
                .HasForeignKey(d => d.id_partido)
                .HasConstraintName("FK__canchas_d__id_pa__5DEAEAF5");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.canchas_disponibilidads)
                .HasForeignKey(d => d.id_torneo)
                .HasConstraintName("FK__canchas_d__id_to__5C02A283");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.canchas_disponibilidads)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__canchas_d__id_us__5EDF0F2E");
        });

        modelBuilder.Entity<credenciales_jugadore>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__credenci__3213E83FA2ED2DD8");

            entity.HasIndex(e => e.codigo, "UQ__credenci__40F9A206FD9BFF36").IsUnique();

            entity.HasIndex(e => new { e.id_jugador, e.id_equipo, e.id_torneo }, "credenciales_unico").IsUnique();

            entity.HasIndex(e => new { e.id_jugador, e.id_torneo }, "idx_credenciales_jug");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVA");
            entity.Property(e => e.fecha_emision).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_generacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.numero_credencial)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.credenciales_jugadores)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__credencia__id_eq__0FB750B3");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.credenciales_jugadoreid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__credencia__id_ju__0EC32C7A");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.credenciales_jugadores)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__credencia__id_to__10AB74EC");

            entity.HasOne(d => d.id_usuario_generacionNavigation).WithMany(p => p.credenciales_jugadoreid_usuario_generacionNavigations)
                .HasForeignKey(d => d.id_usuario_generacion)
                .HasConstraintName("FK__credencia__id_us__1387E197");
        });

        modelBuilder.Entity<deporte>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__deportes__3213E83F527FC922");

            entity.HasIndex(e => e.codigo, "UQ__deportes__40F9A2068E94E127").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.permite_empate).HasDefaultValue(true);
            entity.Property(e => e.permite_penales).HasDefaultValue(false);
            entity.Property(e => e.permite_prorroga).HasDefaultValue(false);
            entity.Property(e => e.tiempo_medio_tiempo_minutos).HasDefaultValue(0);
            entity.Property(e => e.tiempo_prorroga_minutos).HasDefaultValue(0);
        });

        modelBuilder.Entity<enlaces_compartido>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__enlaces___3213E83FB28A6781");

            entity.HasIndex(e => e.codigo, "UQ__enlaces___40F9A2060E7CDA39").IsUnique();

            entity.HasIndex(e => e.codigo_enlace, "UQ__enlaces___5EC76D2677A4BA18").IsUnique();

            entity.HasIndex(e => e.codigo_enlace, "idx_enlaces_codigo");

            entity.HasIndex(e => e.estado, "idx_enlaces_estado");

            entity.HasIndex(e => e.id_tipo_enlace, "idx_enlaces_tipo");

            entity.HasIndex(e => e.id_torneo, "idx_enlaces_torneo");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo_enlace)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVO");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.usos_actuales).HasDefaultValue(0);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.enlaces_compartidos)
                .HasForeignKey(d => d.id_equipo)
                .HasConstraintName("FK__enlaces_c__id_eq__50FB042B");

            entity.HasOne(d => d.id_rol_asignadoNavigation).WithMany(p => p.enlaces_compartidos)
                .HasForeignKey(d => d.id_rol_asignado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_c__id_ro__51EF2864");

            entity.HasOne(d => d.id_tipo_enlaceNavigation).WithMany(p => p.enlaces_compartidos)
                .HasForeignKey(d => d.id_tipo_enlace)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_c__id_ti__4E1E9780");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.enlaces_compartidos)
                .HasForeignKey(d => d.id_torneo)
                .HasConstraintName("FK__enlaces_c__id_to__5006DFF2");

            entity.HasOne(d => d.id_usuario_creadorNavigation).WithMany(p => p.enlaces_compartidos)
                .HasForeignKey(d => d.id_usuario_creador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_c__id_us__4F12BBB9");
        });

        modelBuilder.Entity<enlaces_historial_uso>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__enlaces___3213E83F1AA27089");

            entity.ToTable("enlaces_historial_uso");

            entity.HasIndex(e => e.codigo, "UQ__enlaces___40F9A20660B1D4C5").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_uso).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ip_address)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.uso_exitoso).HasDefaultValue(true);

            entity.HasOne(d => d.id_enlaceNavigation).WithMany(p => p.enlaces_historial_usos)
                .HasForeignKey(d => d.id_enlace)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_h__id_en__5C6CB6D7");

            entity.HasOne(d => d.id_rol_anteriorNavigation).WithMany(p => p.enlaces_historial_usoid_rol_anteriorNavigations)
                .HasForeignKey(d => d.id_rol_anterior)
                .HasConstraintName("FK__enlaces_h__id_ro__5F492382");

            entity.HasOne(d => d.id_rol_nuevoNavigation).WithMany(p => p.enlaces_historial_usoid_rol_nuevoNavigations)
                .HasForeignKey(d => d.id_rol_nuevo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_h__id_ro__603D47BB");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.enlaces_historial_usos)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__enlaces_h__id_us__5D60DB10");
        });

        modelBuilder.Entity<equipo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__equipos__3213E83F33A7530C");

            entity.HasIndex(e => e.codigo, "UQ__equipos__40F9A20688E4F4B4").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.ciudad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.color_primario)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.color_secundario)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.estadio_habitual)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVO");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.nombre_corto)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.pais)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.verificado).HasDefaultValue(false);

            entity.HasOne(d => d.id_capitanNavigation).WithMany(p => p.equipoid_capitanNavigations)
                .HasForeignKey(d => d.id_capitan)
                .HasConstraintName("FK__equipos__id_capi__2B0A656D");

            entity.HasOne(d => d.id_creadorNavigation).WithMany(p => p.equipoid_creadorNavigations)
                .HasForeignKey(d => d.id_creador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__equipos__id_crea__2BFE89A6");
        });

        modelBuilder.Entity<equipos_torneo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__equipos___3213E83F4AB137A3");

            entity.HasIndex(e => e.codigo, "UQ__equipos___40F9A20625410625").IsUnique();

            entity.HasIndex(e => new { e.id_equipo, e.id_torneo }, "equipos_torneos_unico").IsUnique();

            entity.HasIndex(e => e.id_equipo, "idx_et_equipo");

            entity.HasIndex(e => e.estado, "idx_et_estado");

            entity.HasIndex(e => e.id_torneo_fase, "idx_et_fase");

            entity.HasIndex(e => e.id_torneo, "idx_et_torneo");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aprobado).HasDefaultValue(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("INSCRITO");
            entity.Property(e => e.fecha_inscripcion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.tiene_bye).HasDefaultValue(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.equipos_torneos)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__equipos_t__id_eq__28ED12D1");

            entity.HasOne(d => d.id_fase_byeNavigation).WithMany(p => p.equipos_torneoid_fase_byeNavigations)
                .HasForeignKey(d => d.id_fase_bye)
                .HasConstraintName("FK__equipos_t__id_fa__2F9A1060");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.equipos_torneos)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__equipos_t__id_to__29E1370A");

            entity.HasOne(d => d.id_torneo_faseNavigation).WithMany(p => p.equipos_torneoid_torneo_faseNavigations)
                .HasForeignKey(d => d.id_torneo_fase)
                .HasConstraintName("FK__equipos_t__id_to__2DB1C7EE");

            entity.HasOne(d => d.id_usuario_aprobacionNavigation).WithMany(p => p.equipos_torneos)
                .HasForeignKey(d => d.id_usuario_aprobacion)
                .HasConstraintName("FK__equipos_t__id_us__2CBDA3B5");
        });

        modelBuilder.Entity<estadisticas_equipos_torneo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__estadist__3213E83F53CD2B0A");

            entity.HasIndex(e => e.codigo, "UQ__estadist__40F9A206469849B5").IsUnique();

            entity.HasIndex(e => new { e.id_equipo, e.id_torneo, e.id_torneo_fase }, "estadisticas_equipos_unico").IsUnique();

            entity.HasIndex(e => new { e.id_equipo, e.id_torneo }, "idx_est_eq_torneo");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.diferencia_goles).HasComputedColumnSql("([goles_favor]-[goles_contra])", true);
            entity.Property(e => e.fecha_actualizacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.goles_contra).HasDefaultValue(0);
            entity.Property(e => e.goles_favor).HasDefaultValue(0);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.partidos_empatados).HasDefaultValue(0);
            entity.Property(e => e.partidos_ganados).HasDefaultValue(0);
            entity.Property(e => e.partidos_jugados).HasDefaultValue(0);
            entity.Property(e => e.partidos_perdidos).HasDefaultValue(0);
            entity.Property(e => e.partidos_wo_ganados).HasDefaultValue(0);
            entity.Property(e => e.partidos_wo_perdidos).HasDefaultValue(0);
            entity.Property(e => e.puntos).HasDefaultValue(0);
            entity.Property(e => e.racha_actual)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.tarjetas_amarillas).HasDefaultValue(0);
            entity.Property(e => e.tarjetas_rojas).HasDefaultValue(0);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.estadisticas_equipos_torneos)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__estadisti__id_eq__3EA749C6");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.estadisticas_equipos_torneos)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__estadisti__id_to__3F9B6DFF");

            entity.HasOne(d => d.id_torneo_faseNavigation).WithMany(p => p.estadisticas_equipos_torneos)
                .HasForeignKey(d => d.id_torneo_fase)
                .HasConstraintName("FK__estadisti__id_to__408F9238");
        });

        modelBuilder.Entity<estadisticas_jugadore>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__estadist__3213E83F070929BE");

            entity.HasIndex(e => e.codigo, "UQ__estadist__40F9A2066EF541CC").IsUnique();

            entity.HasIndex(e => new { e.id_jugador, e.id_equipo, e.id_torneo }, "estadisticas_jugadores_unico").IsUnique();

            entity.HasIndex(e => new { e.id_jugador, e.id_torneo }, "idx_est_jug_torneo");

            entity.Property(e => e.asistencias).HasDefaultValue(0);
            entity.Property(e => e.autogoles).HasDefaultValue(0);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_actualizacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.goles).HasDefaultValue(0);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.minutos_jugados).HasDefaultValue(0);
            entity.Property(e => e.partidos_jugados).HasDefaultValue(0);
            entity.Property(e => e.partidos_suspendido).HasDefaultValue(0);
            entity.Property(e => e.penales_fallados).HasDefaultValue(0);
            entity.Property(e => e.penales_marcados).HasDefaultValue(0);
            entity.Property(e => e.tarjetas_amarillas).HasDefaultValue(0);
            entity.Property(e => e.tarjetas_rojas).HasDefaultValue(0);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.estadisticas_jugadores)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__estadisti__id_eq__52AE4273");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.estadisticas_jugadores)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__estadisti__id_ju__51BA1E3A");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.estadisticas_jugadores)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__estadisti__id_to__53A266AC");
        });

        modelBuilder.Entity<fases_torneo_catalogo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__fases_to__3213E83F9FF108B8");

            entity.ToTable("fases_torneo_catalogo");

            entity.HasIndex(e => e.codigo, "UQ__fases_to__40F9A2064168BA17").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<fechas_bloqueada>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__fechas_b__3213E83FA162FA67");

            entity.HasIndex(e => e.codigo, "UQ__fechas_b__40F9A20673480CEF").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aplica_a_todas_canchas).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_canchaNavigation).WithMany(p => p.fechas_bloqueada)
                .HasForeignKey(d => d.id_cancha)
                .HasConstraintName("FK__fechas_bl__id_ca__1A34DF26");

            entity.HasOne(d => d.id_motivo_catalogoNavigation).WithMany(p => p.fechas_bloqueada)
                .HasForeignKey(d => d.id_motivo_catalogo)
                .HasConstraintName("FK__fechas_bl__id_mo__1B29035F");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.fechas_bloqueada)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__fechas_bl__id_to__1940BAED");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.fechas_bloqueada)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__fechas_bl__id_us__1D114BD1");
        });

        modelBuilder.Entity<ia_recomendacione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__ia_recom__3213E83FA1785F5C");

            entity.HasIndex(e => e.codigo, "UQ__ia_recom__40F9A206E92AB73D").IsUnique();

            entity.Property(e => e.altura_cm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_generacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.modelo_ia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.nivel_actividad)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.objetivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.peso_kg).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.tipo)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.ia_recomendaciones)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ia_recome__id_us__5555A4F4");
        });

        modelBuilder.Entity<jornadas_imagenes_sociale>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__jornadas__3213E83F89903B24");

            entity.HasIndex(e => e.codigo, "UQ__jornadas__40F9A206A13070E8").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_generacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.jornadas_imagenes_sociales)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jornadas___id_to__6497E884");

            entity.HasOne(d => d.id_usuario_generacionNavigation).WithMany(p => p.jornadas_imagenes_sociales)
                .HasForeignKey(d => d.id_usuario_generacion)
                .HasConstraintName("FK__jornadas___id_us__668030F6");
        });

        modelBuilder.Entity<jugadores_equipo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__jugadore__3213E83F9417B998");

            entity.HasIndex(e => e.codigo, "UQ__jugadore__40F9A20689AA333A").IsUnique();

            entity.HasIndex(e => new { e.id_jugador, e.id_equipo }, "jugadores_equipos_unico").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_capitan).HasDefaultValue(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVO");
            entity.Property(e => e.fecha_inicio).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.posicion)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.jugadores_equipos)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jugadores__id_eq__382F5661");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.jugadores_equipos)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jugadores__id_ju__373B3228");
        });

        modelBuilder.Entity<jugadores_suspensione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__jugadore__3213E83FA24F8808");

            entity.HasIndex(e => e.codigo, "UQ__jugadore__40F9A206D818C016").IsUnique();

            entity.HasIndex(e => e.estado, "idx_susp_estado");

            entity.HasIndex(e => new { e.id_jugador, e.id_torneo }, "idx_susp_jugador");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVA");
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.generada_automaticamente).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.partidos_cumplidos).HasDefaultValue(0);
            entity.Property(e => e.partidos_suspension).HasDefaultValue(1);
            entity.Property(e => e.regla_aplicada)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.jugadores_suspensiones)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jugadores__id_eq__63D8CE75");

            entity.HasOne(d => d.id_evento_origenNavigation).WithMany(p => p.jugadores_suspensiones)
                .HasForeignKey(d => d.id_evento_origen)
                .HasConstraintName("FK__jugadores__id_ev__66B53B20");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.jugadores_suspensioneid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jugadores__id_ju__62E4AA3C");

            entity.HasOne(d => d.id_partido_origenNavigation).WithMany(p => p.jugadores_suspensiones)
                .HasForeignKey(d => d.id_partido_origen)
                .HasConstraintName("FK__jugadores__id_pa__65C116E7");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.jugadores_suspensiones)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__jugadores__id_to__64CCF2AE");

            entity.HasOne(d => d.id_usuario_aprobacionNavigation).WithMany(p => p.jugadores_suspensioneid_usuario_aprobacionNavigations)
                .HasForeignKey(d => d.id_usuario_aprobacion)
                .HasConstraintName("FK__jugadores__id_us__6C6E1476");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.jugadores_suspensioneid_usuario_registroNavigations)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__jugadores__id_us__6B79F03D");
        });

        modelBuilder.Entity<metodos_pago>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__metodos___3213E83F905B4CE4");

            entity.ToTable("metodos_pago");

            entity.HasIndex(e => e.codigo, "UQ__metodos___40F9A2066E138F7A").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_automatico).HasDefaultValue(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.requiere_comprobante).HasDefaultValue(false);
        });

        modelBuilder.Entity<motivos_reprogramacion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__motivos___3213E83FA588819C");

            entity.ToTable("motivos_reprogramacion");

            entity.HasIndex(e => e.codigo, "UQ__motivos___40F9A206A66A51F0").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.requiere_aprobacion).HasDefaultValue(false);
        });

        modelBuilder.Entity<multa>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__multas__3213E83F8CB0458A");

            entity.HasIndex(e => e.codigo, "UQ__multas__40F9A206E3AF810B").IsUnique();

            entity.HasIndex(e => e.id_equipo, "idx_multas_equipo");

            entity.HasIndex(e => e.estado, "idx_multas_estado");

            entity.HasIndex(e => e.id_jugador, "idx_multas_jugador");

            entity.HasIndex(e => e.id_torneo, "idx_multas_torneo");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.fecha_aplicacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.generada_automaticamente).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Bs");
            entity.Property(e => e.monto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.monto_pagado)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.multa)
                .HasForeignKey(d => d.id_equipo)
                .HasConstraintName("FK__multas__id_equip__740F363E");

            entity.HasOne(d => d.id_eventoNavigation).WithMany(p => p.multa)
                .HasForeignKey(d => d.id_evento)
                .HasConstraintName("FK__multas__id_event__76EBA2E9");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.multaid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .HasConstraintName("FK__multas__id_jugad__75035A77");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.multa)
                .HasForeignKey(d => d.id_partido)
                .HasConstraintName("FK__multas__id_parti__75F77EB0");

            entity.HasOne(d => d.id_tipo_multaNavigation).WithMany(p => p.multa)
                .HasForeignKey(d => d.id_tipo_multa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__multas__id_tipo___731B1205");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.multa)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__multas__id_torne__7226EDCC");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.multaid_usuario_registroNavigations)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__multas__id_usuar__7CA47C3F");
        });

        modelBuilder.Entity<multas_historial_pago>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__multas_h__3213E83FA0DA9224");

            entity.HasIndex(e => e.codigo, "UQ__multas_h__40F9A2061D7D1FDE").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.codigo_transaccion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.comprobante_verificado).HasDefaultValue(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PROCESANDO");
            entity.Property(e => e.fecha_pago).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Bs");
            entity.Property(e => e.monto_pagado).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.id_metodo_pagoNavigation).WithMany(p => p.multas_historial_pagos)
                .HasForeignKey(d => d.id_metodo_pago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__multas_hi__id_me__035179CE");

            entity.HasOne(d => d.id_multaNavigation).WithMany(p => p.multas_historial_pagos)
                .HasForeignKey(d => d.id_multa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__multas_hi__id_mu__025D5595");

            entity.HasOne(d => d.id_usuario_pagoNavigation).WithMany(p => p.multas_historial_pagoid_usuario_pagoNavigations)
                .HasForeignKey(d => d.id_usuario_pago)
                .HasConstraintName("FK__multas_hi__id_us__090A5324");

            entity.HasOne(d => d.id_usuario_verificacionNavigation).WithMany(p => p.multas_historial_pagoid_usuario_verificacionNavigations)
                .HasForeignKey(d => d.id_usuario_verificacion)
                .HasConstraintName("FK__multas_hi__id_us__07220AB2");
        });

        modelBuilder.Entity<notificacione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__notifica__3213E83FD75957FE");

            entity.HasIndex(e => e.codigo, "UQ__notifica__40F9A2067B41A3BA").IsUnique();

            entity.HasIndex(e => e.enviada, "idx_notif_enviada").HasFilter("([enviada]=(0))");

            entity.HasIndex(e => e.leida, "idx_notif_leida").HasFilter("([leida]=(0))");

            entity.HasIndex(e => e.id_tipo_notificacion, "idx_notif_tipo");

            entity.HasIndex(e => e.id_usuario_destino, "idx_notif_usuario");

            entity.Property(e => e.accion_tipo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.archivada).HasDefaultValue(false);
            entity.Property(e => e.canal)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("IN_APP");
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.email_enviado).HasDefaultValue(false);
            entity.Property(e => e.enviada).HasDefaultValue(false);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.leida).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.prioridad)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("MEDIA");
            entity.Property(e => e.push_enviado).HasDefaultValue(false);
            entity.Property(e => e.sms_enviado).HasDefaultValue(false);
            entity.Property(e => e.titulo)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_equipo)
                .HasConstraintName("FK__notificac__id_eq__34E8D562");

            entity.HasOne(d => d.id_multaNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_multa)
                .HasConstraintName("FK__notificac__id_mu__36D11DD4");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_partido)
                .HasConstraintName("FK__notificac__id_pa__35DCF99B");

            entity.HasOne(d => d.id_suspensionNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_suspension)
                .HasConstraintName("FK__notificac__id_su__37C5420D");

            entity.HasOne(d => d.id_tipo_notificacionNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_tipo_notificacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__notificac__id_ti__3118447E");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_torneo)
                .HasConstraintName("FK__notificac__id_to__33F4B129");

            entity.HasOne(d => d.id_usuario_destinoNavigation).WithMany(p => p.notificaciones)
                .HasForeignKey(d => d.id_usuario_destino)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__notificac__id_us__320C68B7");
        });

        modelBuilder.Entity<partido>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__partidos__3213E83FD9A74F81");

            entity.HasIndex(e => e.codigo, "UQ__partidos__40F9A20601E20468").IsUnique();

            entity.HasIndex(e => e.id_arbitro_principal, "idx_partidos_arbitro");

            entity.HasIndex(e => e.id_cancha, "idx_partidos_cancha");

            entity.HasIndex(e => e.estado, "idx_partidos_estado");

            entity.HasIndex(e => e.id_torneo_fase, "idx_partidos_fase");

            entity.HasIndex(e => e.fecha_hora, "idx_partidos_fecha");

            entity.HasIndex(e => e.id_equipo_local, "idx_partidos_local");

            entity.HasIndex(e => e.id_torneo, "idx_partidos_torneo");

            entity.HasIndex(e => e.id_equipo_visitante, "idx_partidos_visitante");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.cancha_descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_partido_ida).HasDefaultValue(false);
            entity.Property(e => e.es_partido_vuelta).HasDefaultValue(false);
            entity.Property(e => e.es_wo).HasDefaultValue(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PROGRAMADO");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.goles_local_prorroga).HasDefaultValue(0);
            entity.Property(e => e.goles_visitante_prorroga).HasDefaultValue(0);
            entity.Property(e => e.hubo_penales).HasDefaultValue(false);
            entity.Property(e => e.hubo_prorroga).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.tiempo_actual)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_arbitro_asistente_1Navigation).WithMany(p => p.partidoid_arbitro_asistente_1Navigations)
                .HasForeignKey(d => d.id_arbitro_asistente_1)
                .HasConstraintName("FK__partidos__id_arb__6F7F8B4B");

            entity.HasOne(d => d.id_arbitro_asistente_2Navigation).WithMany(p => p.partidoid_arbitro_asistente_2Navigations)
                .HasForeignKey(d => d.id_arbitro_asistente_2)
                .HasConstraintName("FK__partidos__id_arb__7073AF84");

            entity.HasOne(d => d.id_arbitro_principalNavigation).WithMany(p => p.partidoid_arbitro_principalNavigations)
                .HasForeignKey(d => d.id_arbitro_principal)
                .HasConstraintName("FK__partidos__id_arb__6E8B6712");

            entity.HasOne(d => d.id_canchaNavigation).WithMany(p => p.partidos)
                .HasForeignKey(d => d.id_cancha)
                .HasConstraintName("FK__partidos__id_can__6ABAD62E");

            entity.HasOne(d => d.id_cuarto_arbitroNavigation).WithMany(p => p.partidoid_cuarto_arbitroNavigations)
                .HasForeignKey(d => d.id_cuarto_arbitro)
                .HasConstraintName("FK__partidos__id_cua__7167D3BD");

            entity.HasOne(d => d.id_equipo_ganador_woNavigation).WithMany(p => p.partidoid_equipo_ganador_woNavigations)
                .HasForeignKey(d => d.id_equipo_ganador_wo)
                .HasConstraintName("FK__partidos__id_equ__7814D14C");

            entity.HasOne(d => d.id_equipo_localNavigation).WithMany(p => p.partidoid_equipo_localNavigations)
                .HasForeignKey(d => d.id_equipo_local)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos__id_equ__68D28DBC");

            entity.HasOne(d => d.id_equipo_visitanteNavigation).WithMany(p => p.partidoid_equipo_visitanteNavigations)
                .HasForeignKey(d => d.id_equipo_visitante)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos__id_equ__69C6B1F5");

            entity.HasOne(d => d.id_llaveNavigation).WithMany(p => p.partidos)
                .HasForeignKey(d => d.id_llave)
                .HasConstraintName("FK__partidos__id_lla__67DE6983");

            entity.HasOne(d => d.id_partido_idaNavigation).WithMany(p => p.Inverseid_partido_idaNavigation)
                .HasForeignKey(d => d.id_partido_ida)
                .HasConstraintName("FK__partidos__id_par__6D9742D9");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.partidos)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos__id_tor__65F62111");

            entity.HasOne(d => d.id_torneo_faseNavigation).WithMany(p => p.partidos)
                .HasForeignKey(d => d.id_torneo_fase)
                .HasConstraintName("FK__partidos__id_tor__66EA454A");

            entity.HasOne(d => d.id_usuario_modificacionNavigation).WithMany(p => p.partidoid_usuario_modificacionNavigations)
                .HasForeignKey(d => d.id_usuario_modificacion)
                .HasConstraintName("FK__partidos__id_usu__7AF13DF7");
        });

        modelBuilder.Entity<partidos_asistencium>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__partidos__3213E83FF959E3DF");

            entity.HasIndex(e => e.codigo, "UQ__partidos__40F9A206F987DD8E").IsUnique();

            entity.HasIndex(e => new { e.id_partido, e.id_jugador }, "partidos_asistencia_unico").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.motivo_ausencia)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.partidos_asistencia)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_eq__2AA05119");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.partidos_asistenciumid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_ju__29AC2CE0");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.partidos_asistencia)
                .HasForeignKey(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_pa__28B808A7");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.partidos_asistenciumid_usuario_registroNavigations)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__partidos___id_us__2C88998B");
        });

        modelBuilder.Entity<partidos_evento>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__partidos__3213E83F8A112603");

            entity.HasIndex(e => e.codigo, "UQ__partidos__40F9A2060271FC42").IsUnique();

            entity.HasIndex(e => e.id_partido, "idx_eventos_partido");

            entity.HasIndex(e => e.tipo_evento, "idx_eventos_tipo");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_autogol).HasDefaultValue(false);
            entity.Property(e => e.es_penal).HasDefaultValue(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.tiempo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.tipo_evento)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.partidos_eventos)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_eq__038683F8");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.partidos_eventoid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .HasConstraintName("FK__partidos___id_ju__047AA831");

            entity.HasOne(d => d.id_jugador_asistenciaNavigation).WithMany(p => p.partidos_eventoid_jugador_asistenciaNavigations)
                .HasForeignKey(d => d.id_jugador_asistencia)
                .HasConstraintName("FK__partidos___id_ju__093F5D4E");

            entity.HasOne(d => d.id_jugador_entraNavigation).WithMany(p => p.partidos_eventoid_jugador_entraNavigations)
                .HasForeignKey(d => d.id_jugador_entra)
                .HasConstraintName("FK__partidos___id_ju__0662F0A3");

            entity.HasOne(d => d.id_jugador_saleNavigation).WithMany(p => p.partidos_eventoid_jugador_saleNavigations)
                .HasForeignKey(d => d.id_jugador_sale)
                .HasConstraintName("FK__partidos___id_ju__056ECC6A");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.partidos_eventos)
                .HasForeignKey(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_pa__02925FBF");

            entity.HasOne(d => d.id_usuario_registroNavigation).WithMany(p => p.partidos_eventoid_usuario_registroNavigations)
                .HasForeignKey(d => d.id_usuario_registro)
                .HasConstraintName("FK__partidos___id_us__0B27A5C0");
        });

        modelBuilder.Entity<partidos_observacione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__partidos__3213E83F228F4592");

            entity.HasIndex(e => e.codigo, "UQ__partidos__40F9A20684F113A5").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_publica).HasDefaultValue(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_arbitroNavigation).WithMany(p => p.partidos_observaciones)
                .HasForeignKey(d => d.id_arbitro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_ar__10E07F16");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.partidos_observaciones)
                .HasForeignKey(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_pa__0FEC5ADD");
        });

        modelBuilder.Entity<partidos_reprogramacione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__partidos__3213E83FF629DB8E");

            entity.HasIndex(e => e.codigo, "UQ__partidos__40F9A206365D8618").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.fecha_solicitud).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.generada_automaticamente).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.requiere_aprobacion).HasDefaultValue(true);
            entity.Property(e => e.score_fecha_sugerida).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.id_cancha_nuevaNavigation).WithMany(p => p.partidos_reprogramacioneid_cancha_nuevaNavigations)
                .HasForeignKey(d => d.id_cancha_nueva)
                .HasConstraintName("FK__partidos___id_ca__269AB60B");

            entity.HasOne(d => d.id_cancha_originalNavigation).WithMany(p => p.partidos_reprogramacioneid_cancha_originalNavigations)
                .HasForeignKey(d => d.id_cancha_original)
                .HasConstraintName("FK__partidos___id_ca__25A691D2");

            entity.HasOne(d => d.id_motivoNavigation).WithMany(p => p.partidos_reprogramaciones)
                .HasForeignKey(d => d.id_motivo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_mo__24B26D99");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.partidos_reprogramaciones)
                .HasForeignKey(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__partidos___id_pa__23BE4960");

            entity.HasOne(d => d.id_usuario_aprobacionNavigation).WithMany(p => p.partidos_reprogramacioneid_usuario_aprobacionNavigations)
                .HasForeignKey(d => d.id_usuario_aprobacion)
                .HasConstraintName("FK__partidos___id_us__2B5F6B28");

            entity.HasOne(d => d.id_usuario_solicitudNavigation).WithMany(p => p.partidos_reprogramacioneid_usuario_solicitudNavigations)
                .HasForeignKey(d => d.id_usuario_solicitud)
                .HasConstraintName("FK__partidos___id_us__278EDA44");
        });

        modelBuilder.Entity<penales_detalle>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__penales___3213E83F2A0EB9D3");

            entity.ToTable("penales_detalle");

            entity.HasIndex(e => e.codigo, "UQ__penales___40F9A2067D6233CF").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.resultado)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.penales_detalles)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__penales_d__id_eq__2116E6DF");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.penales_detalles)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__penales_d__id_ju__220B0B18");

            entity.HasOne(d => d.id_tanda_penalesNavigation).WithMany(p => p.penales_detalles)
                .HasForeignKey(d => d.id_tanda_penales)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__penales_d__id_ta__2022C2A6");
        });

        modelBuilder.Entity<penales_tandum>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__penales___3213E83F9D48F79A");

            entity.HasIndex(e => e.codigo, "UQ__penales___40F9A2069E60D082").IsUnique();

            entity.HasIndex(e => e.id_partido, "UQ__penales___42D83E450C89F60F").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.hubo_muerte_subita).HasDefaultValue(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_equipo_ganadorNavigation).WithMany(p => p.penales_tanda)
                .HasForeignKey(d => d.id_equipo_ganador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__penales_t__id_eq__1975C517");

            entity.HasOne(d => d.id_partidoNavigation).WithOne(p => p.penales_tandum)
                .HasForeignKey<penales_tandum>(d => d.id_partido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__penales_t__id_pa__1881A0DE");
        });

        modelBuilder.Entity<preferencias_notificacione>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__preferen__3213E83F5C6B0155");

            entity.HasIndex(e => e.codigo, "UQ__preferen__40F9A2060C1D395C").IsUnique();

            entity.HasIndex(e => new { e.id_usuario, e.id_tipo_notificacion }, "preferencias_notif_unico").IsUnique();

            entity.Property(e => e.activado).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.email_activado).HasDefaultValue(true);
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.push_activado).HasDefaultValue(true);
            entity.Property(e => e.sms_activado).HasDefaultValue(false);

            entity.HasOne(d => d.id_tipo_notificacionNavigation).WithMany(p => p.preferencias_notificaciones)
                .HasForeignKey(d => d.id_tipo_notificacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__preferenc__id_ti__46136164");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.preferencias_notificaciones)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__preferenc__id_us__451F3D2B");
        });

        modelBuilder.Entity<solicitudes_equipo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__solicitu__3213E83F6A5D9BA4");

            entity.HasIndex(e => new { e.id_equipo, e.estado }, "IX_solicitudes_equipos_equipo_estado");

            entity.HasIndex(e => e.estado, "IX_solicitudes_equipos_estado");

            entity.HasIndex(e => e.fecha_solicitud, "IX_solicitudes_equipos_fecha_solicitud").IsDescending();

            entity.HasIndex(e => e.id_equipo, "IX_solicitudes_equipos_id_equipo");

            entity.HasIndex(e => e.id_jugador, "IX_solicitudes_equipos_id_jugador");

            entity.HasIndex(e => new { e.id_equipo, e.fecha_solicitud }, "IX_solicitudes_equipos_pendientes").HasFilter("([estado]='PENDIENTE' AND [activo]=(1))");

            entity.HasIndex(e => e.codigo, "UQ__solicitu__40F9A2060101FBAC").IsUnique();

            entity.HasIndex(e => e.codigo, "UQ_solicitudes_equipos_codigo").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.comentario).HasMaxLength(500);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_solicitud).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.mensaje).HasMaxLength(500);
            entity.Property(e => e.metadata).HasDefaultValue("{}");

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.solicitudes_equipos)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__solicitud__id_eq__06ED0088");

            entity.HasOne(d => d.id_jugadorNavigation).WithMany(p => p.solicitudes_equipoid_jugadorNavigations)
                .HasForeignKey(d => d.id_jugador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__solicitud__id_ju__07E124C1");

            entity.HasOne(d => d.id_usuario_procesadorNavigation).WithMany(p => p.solicitudes_equipoid_usuario_procesadorNavigations)
                .HasForeignKey(d => d.id_usuario_procesador)
                .HasConstraintName("FK__solicitud__id_us__0ABD916C");
        });

        modelBuilder.Entity<tipos_documento>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_do__3213E83F5DBD1D41");

            entity.ToTable("tipos_documento");

            entity.HasIndex(e => e.codigo, "UQ__tipos_do__40F9A2068223CC3C").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.patron_validacion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.requiere_foto).HasDefaultValue(false);
        });

        modelBuilder.Entity<tipos_enlace>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_en__3213E83F51979B88");

            entity.ToTable("tipos_enlace");

            entity.HasIndex(e => e.codigo, "UQ__tipos_en__40F9A206B8D14295").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.alcance)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.permite_expiracion).HasDefaultValue(true);
        });

        modelBuilder.Entity<tipos_formato_torneo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_fo__3213E83F64A9646B");

            entity.ToTable("tipos_formato_torneo");

            entity.HasIndex(e => e.codigo, "UQ__tipos_fo__40F9A206986F9424").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipos_multum>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_mu__3213E83F14D11471");

            entity.HasIndex(e => e.codigo, "UQ__tipos_mu__40F9A20691E644F1").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.categoria)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipos_notificacion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_no__3213E83F0F66D966");

            entity.ToTable("tipos_notificacion");

            entity.HasIndex(e => e.codigo, "UQ__tipos_no__40F9A2064476F534").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.categoria)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.permite_configuracion).HasDefaultValue(true);
            entity.Property(e => e.prioridad)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("MEDIA");
        });

        modelBuilder.Entity<tipos_rol>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_ro__3213E83F3399FFD6");

            entity.ToTable("tipos_rol");

            entity.HasIndex(e => e.codigo, "UQ__tipos_ro__40F9A2060005D9BE").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipos_superficie>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_su__3213E83F75EA8AF4");

            entity.ToTable("tipos_superficie");

            entity.HasIndex(e => e.codigo, "UQ__tipos_su__40F9A2063F02D727").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tipos_usuario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tipos_us__3213E83FEBD99D9D");

            entity.ToTable("tipos_usuario");

            entity.HasIndex(e => e.codigo, "UQ__tipos_us__40F9A2069C2C5FDB").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<tokens_recuperacion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__tokens_r__3213E83F0DC9C844");

            entity.ToTable("tokens_recuperacion");

            entity.HasIndex(e => e.token, "UQ__tokens_r__CA90DA7A8C3D87F8").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ip_solicitud)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.token)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.usado).HasDefaultValue(false);

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.tokens_recuperacions)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__tokens_re__id_us__4EA8A765");
        });

        modelBuilder.Entity<torneo>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos__3213E83F108AC14B");

            entity.HasIndex(e => e.codigo, "UQ__torneos__40F9A20669236476").IsUnique();

            entity.HasIndex(e => e.activo, "idx_torneos_activo").HasFilter("([activo]=(1))");

            entity.HasIndex(e => e.id_deporte, "idx_torneos_deporte");

            entity.HasIndex(e => e.estado, "idx_torneos_estado");

            entity.HasIndex(e => e.fecha_inicio, "idx_torneos_fecha");

            entity.HasIndex(e => e.id_formato, "idx_torneos_formato");

            entity.HasIndex(e => e.id_organizador, "idx_torneos_organizador");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.ciudad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.es_publico).HasDefaultValue(true);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PLANIFICACION");
            entity.Property(e => e.fase_actual)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fue_clonado).HasDefaultValue(false);
            entity.Property(e => e.genero_permitido)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.latitud).HasColumnType("decimal(10, 8)");
            entity.Property(e => e.longitud).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.numero_clonacion).HasDefaultValue(0);
            entity.Property(e => e.pais)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.permite_equipos_impares).HasDefaultValue(true);
            entity.Property(e => e.reglamento_nombre_archivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.requiere_aprobacion_equipos).HasDefaultValue(false);
            entity.Property(e => e.ubicacion)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.id_deporteNavigation).WithMany(p => p.torneos)
                .HasForeignKey(d => d.id_deporte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos__id_depo__0A9D95DB");

            entity.HasOne(d => d.id_formatoNavigation).WithMany(p => p.torneos)
                .HasForeignKey(d => d.id_formato)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos__id_form__0B91BA14");

            entity.HasOne(d => d.id_organizadorNavigation).WithMany(p => p.torneoid_organizadorNavigations)
                .HasForeignKey(d => d.id_organizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos__id_orga__1332DBDC");

            entity.HasOne(d => d.id_torneo_origenNavigation).WithMany(p => p.Inverseid_torneo_origenNavigation)
                .HasForeignKey(d => d.id_torneo_origen)
                .HasConstraintName("FK__torneos__id_torn__0C85DE4D");

            entity.HasOne(d => d.id_usuario_modificacionNavigation).WithMany(p => p.torneoid_usuario_modificacionNavigations)
                .HasForeignKey(d => d.id_usuario_modificacion)
                .HasConstraintName("FK__torneos__id_usua__160F4887");
        });

        modelBuilder.Entity<torneos_config_eliminatorium>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F2C02C197");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A2062C4F98AD").IsUnique();

            entity.HasIndex(e => e.id_torneo, "UQ__torneos___DBB62AF93987DF9D").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aplica_gol_visitante).HasDefaultValue(false);
            entity.Property(e => e.aplica_muerte_subita).HasDefaultValue(false);
            entity.Property(e => e.aplica_penales_empate).HasDefaultValue(true);
            entity.Property(e => e.aplica_prorroga).HasDefaultValue(true);
            entity.Property(e => e.aplica_sembrado).HasDefaultValue(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.criterio_sembrado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ida_y_vuelta).HasDefaultValue(false);
            entity.Property(e => e.sistema_bye)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("MEJORES_RANKING");
            entity.Property(e => e.tiene_cuartos).HasDefaultValue(true);
            entity.Property(e => e.tiene_dieciseisavos).HasDefaultValue(false);
            entity.Property(e => e.tiene_final).HasDefaultValue(true);
            entity.Property(e => e.tiene_octavos).HasDefaultValue(false);
            entity.Property(e => e.tiene_semifinales).HasDefaultValue(true);
            entity.Property(e => e.tiene_tercer_lugar).HasDefaultValue(true);

            entity.HasOne(d => d.id_torneoNavigation).WithOne(p => p.torneos_config_eliminatorium)
                .HasForeignKey<torneos_config_eliminatorium>(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_to__51300E55");
        });

        modelBuilder.Entity<torneos_config_grupos_eliminatorium>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F9084C992");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A20663B95EA7").IsUnique();

            entity.HasIndex(e => e.id_torneo, "UQ__torneos___DBB62AF95933A117").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aplica_mejores_terceros).HasDefaultValue(false);
            entity.Property(e => e.cantidad_mejores_terceros).HasDefaultValue(0);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.criterio_desempate_grupo_1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PUNTOS");
            entity.Property(e => e.criterio_desempate_grupo_2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ENFRENTAMIENTO_DIRECTO");
            entity.Property(e => e.criterio_desempate_grupo_3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("DIFERENCIA_GOLES");
            entity.Property(e => e.criterio_desempate_grupo_4)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("GOLES_FAVOR");
            entity.Property(e => e.criterio_desempate_grupo_5)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("TARJETAS_AMARILLAS");
            entity.Property(e => e.criterio_desempate_grupo_6)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("SORTEO");
            entity.Property(e => e.criterio_mejor_tercero_1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PUNTOS");
            entity.Property(e => e.criterio_mejor_tercero_2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("DIFERENCIA_GOLES");
            entity.Property(e => e.criterio_mejor_tercero_3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("GOLES_FAVOR");
            entity.Property(e => e.elim_aplica_penales).HasDefaultValue(true);
            entity.Property(e => e.elim_aplica_prorroga).HasDefaultValue(true);
            entity.Property(e => e.elim_aplica_sembrado).HasDefaultValue(false);
            entity.Property(e => e.elim_criterio_sembrado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.elim_gol_visitante).HasDefaultValue(false);
            entity.Property(e => e.elim_ida_y_vuelta).HasDefaultValue(false);
            entity.Property(e => e.elim_tiene_tercer_lugar).HasDefaultValue(true);
            entity.Property(e => e.equipos_clasifican_por_grupo).HasDefaultValue(2);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.grupos_ida_y_vuelta).HasDefaultValue(false);
            entity.Property(e => e.numero_vueltas_grupos).HasDefaultValue(1);
            entity.Property(e => e.puntos_derrota_grupos).HasDefaultValue(0);
            entity.Property(e => e.puntos_empate_grupos).HasDefaultValue(1);
            entity.Property(e => e.puntos_victoria_grupos).HasDefaultValue(3);
            entity.Property(e => e.puntos_wo_ganador_grupos).HasDefaultValue(3);
            entity.Property(e => e.puntos_wo_perdedor_grupos).HasDefaultValue(-1);

            entity.HasOne(d => d.id_torneoNavigation).WithOne(p => p.torneos_config_grupos_eliminatorium)
                .HasForeignKey<torneos_config_grupos_eliminatorium>(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_to__6442E2C9");
        });

        modelBuilder.Entity<torneos_config_horarios_liga>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F3F67F584");

            entity.ToTable("torneos_config_horarios_liga");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A2063614E194").IsUnique();

            entity.HasIndex(e => e.id_torneo, "idx_torneos_config_horarios_torneo");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.torneos_config_horarios_ligas)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_to__73DA2C14");
        });

        modelBuilder.Entity<torneos_config_liga>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F92D03B0A");

            entity.ToTable("torneos_config_liga");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A2063B2DECB6").IsUnique();

            entity.HasIndex(e => e.id_torneo, "UQ__torneos___DBB62AF965DD2C36").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aplica_penales_en_empate).HasDefaultValue(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.criterio_desempate_1)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PUNTOS");
            entity.Property(e => e.criterio_desempate_2)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ENFRENTAMIENTO_DIRECTO");
            entity.Property(e => e.criterio_desempate_3)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("DIFERENCIA_GOLES");
            entity.Property(e => e.criterio_desempate_4)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("GOLES_FAVOR");
            entity.Property(e => e.criterio_desempate_5)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("GOLES_VISITA");
            entity.Property(e => e.criterio_desempate_6)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("TARJETAS_AMARILLAS");
            entity.Property(e => e.criterio_desempate_7)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("SORTEO");
            entity.Property(e => e.descansos_por_equipo).HasDefaultValue(0);
            entity.Property(e => e.equipos_ascienden).HasDefaultValue(0);
            entity.Property(e => e.equipos_clasifican_copa).HasDefaultValue(0);
            entity.Property(e => e.equipos_descienden).HasDefaultValue(0);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ida_y_vuelta).HasDefaultValue(false);
            entity.Property(e => e.numero_vueltas).HasDefaultValue(1);
            entity.Property(e => e.puntos_derrota).HasDefaultValue(0);
            entity.Property(e => e.puntos_derrota_penales).HasDefaultValue(1);
            entity.Property(e => e.puntos_empate).HasDefaultValue(1);
            entity.Property(e => e.puntos_victoria).HasDefaultValue(3);
            entity.Property(e => e.puntos_victoria_penales).HasDefaultValue(2);
            entity.Property(e => e.puntos_wo_ganador).HasDefaultValue(3);
            entity.Property(e => e.puntos_wo_perdedor).HasDefaultValue(-1);

            entity.HasOne(d => d.id_torneoNavigation).WithOne(p => p.torneos_config_liga)
                .HasForeignKey<torneos_config_liga>(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_to__367C1819");
        });

        modelBuilder.Entity<torneos_config_multa>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83FE925A3D5");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A20637DE1D96").IsUnique();

            entity.HasIndex(e => new { e.id_torneo, e.id_tipo_multa }, "torneos_config_multas_unico").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.aplica_automaticamente).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_creacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.moneda)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Bs");
            entity.Property(e => e.monto).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.id_tipo_multaNavigation).WithMany(p => p.torneos_config_multa)
                .HasForeignKey(d => d.id_tipo_multa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_ti__03BB8E22");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.torneos_config_multa)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_c__id_to__02C769E9");
        });

        modelBuilder.Entity<torneos_fase>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F726B9089");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A206976395E0").IsUnique();

            entity.HasIndex(e => new { e.id_torneo, e.id_fase_catalogo, e.letra_grupo }, "torneos_fases_unico").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.esta_activa).HasDefaultValue(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("PENDIENTE");
            entity.Property(e => e.letra_grupo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.nombre_grupo)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_fase_catalogoNavigation).WithMany(p => p.torneos_fases)
                .HasForeignKey(d => d.id_fase_catalogo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_f__id_fa__0D44F85C");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.torneos_fases)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_f__id_to__0C50D423");
        });

        modelBuilder.Entity<torneos_historial_clonacion>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83FEED3EDB0");

            entity.ToTable("torneos_historial_clonacion");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A20631BE3492").IsUnique();

            entity.Property(e => e.cambios_formato).HasDefaultValue(false);
            entity.Property(e => e.cambios_nombre).HasDefaultValue(false);
            entity.Property(e => e.clon_calendario).HasDefaultValue(false);
            entity.Property(e => e.clon_configuracion).HasDefaultValue(true);
            entity.Property(e => e.clon_equipos).HasDefaultValue(true);
            entity.Property(e => e.clon_multas).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_clonacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.formato_anterior)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.formato_nuevo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombre_anterior)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.nombre_nuevo)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.id_torneo_clonadoNavigation).WithMany(p => p.torneos_historial_clonacionid_torneo_clonadoNavigations)
                .HasForeignKey(d => d.id_torneo_clonado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_h__id_to__1EA48E88");

            entity.HasOne(d => d.id_torneo_origenNavigation).WithMany(p => p.torneos_historial_clonacionid_torneo_origenNavigations)
                .HasForeignKey(d => d.id_torneo_origen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_h__id_to__1DB06A4F");

            entity.HasOne(d => d.id_usuario_clonadorNavigation).WithMany(p => p.torneos_historial_clonacions)
                .HasForeignKey(d => d.id_usuario_clonador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_h__id_us__1F98B2C1");
        });

        modelBuilder.Entity<torneos_llafe>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F13AEF242");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A206E78C808B").IsUnique();

            entity.HasIndex(e => new { e.id_torneo, e.id_torneo_fase }, "idx_llaves_fase");

            entity.HasIndex(e => e.id_llave_siguiente, "idx_llaves_siguiente");

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.posicion_siguiente)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.tiene_bye).HasDefaultValue(false);

            entity.HasOne(d => d.id_equipo_con_byeNavigation).WithMany(p => p.torneos_llafeid_equipo_con_byeNavigations)
                .HasForeignKey(d => d.id_equipo_con_bye)
                .HasConstraintName("FK__torneos_l__id_eq__214BF109");

            entity.HasOne(d => d.id_equipo_ganadorNavigation).WithMany(p => p.torneos_llafeid_equipo_ganadorNavigations)
                .HasForeignKey(d => d.id_equipo_ganador)
                .HasConstraintName("FK__torneos_l__id_eq__2334397B");

            entity.HasOne(d => d.id_equipo_inferiorNavigation).WithMany(p => p.torneos_llafeid_equipo_inferiorNavigations)
                .HasForeignKey(d => d.id_equipo_inferior)
                .HasConstraintName("FK__torneos_l__id_eq__1F63A897");

            entity.HasOne(d => d.id_equipo_superiorNavigation).WithMany(p => p.torneos_llafeid_equipo_superiorNavigations)
                .HasForeignKey(d => d.id_equipo_superior)
                .HasConstraintName("FK__torneos_l__id_eq__1E6F845E");

            entity.HasOne(d => d.id_llave_siguienteNavigation).WithMany(p => p.Inverseid_llave_siguienteNavigation)
                .HasForeignKey(d => d.id_llave_siguiente)
                .HasConstraintName("FK__torneos_l__id_ll__22401542");

            entity.HasOne(d => d.id_partidoNavigation).WithMany(p => p.torneos_llaves)
                .HasForeignKey(d => d.id_partido)
                .HasConstraintName("FK_llaves_partido");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.torneos_llaves)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_l__id_to__1C873BEC");

            entity.HasOne(d => d.id_torneo_faseNavigation).WithMany(p => p.torneos_llaves)
                .HasForeignKey(d => d.id_torneo_fase)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_l__id_to__1D7B6025");
        });

        modelBuilder.Entity<torneos_sembrado>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__torneos___3213E83F249901AD");

            entity.ToTable("torneos_sembrado");

            entity.HasIndex(e => e.codigo, "UQ__torneos___40F9A2064EFCAB80").IsUnique();

            entity.HasIndex(e => new { e.id_torneo, e.id_torneo_fase, e.id_equipo }, "torneos_sembrado_unico").IsUnique();

            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.criterio_base)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.puntos_base).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.torneos_sembrados)
                .HasForeignKey(d => d.id_equipo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_s__id_eq__17C286CF");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.torneos_sembrados)
                .HasForeignKey(d => d.id_torneo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_s__id_to__15DA3E5D");

            entity.HasOne(d => d.id_torneo_faseNavigation).WithMany(p => p.torneos_sembrados)
                .HasForeignKey(d => d.id_torneo_fase)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__torneos_s__id_to__16CE6296");
        });

        modelBuilder.Entity<usuario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__usuarios__3213E83FCD91DCE0");

            entity.HasIndex(e => e.codigo, "UQ__usuarios__40F9A2066C3DC2F3").IsUnique();

            entity.HasIndex(e => e.email, "UQ__usuarios__AB6E61640F1E9102").IsUnique();

            entity.HasIndex(e => e.activo, "idx_usuarios_activo").HasFilter("([activo]=(1))");

            entity.HasIndex(e => new { e.id_tipo_documento, e.numero_documento }, "idx_usuarios_documento");

            entity.HasIndex(e => e.email, "idx_usuarios_email");

            entity.HasIndex(e => e.id_tipo_usuario, "idx_usuarios_tipo");

            entity.HasIndex(e => new { e.id_tipo_documento, e.numero_documento }, "usuarios_documento_unico").IsUnique();

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.altura_cm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.apellidos)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ciudad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.email_verificado).HasDefaultValue(false);
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_registro).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.genero)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.intentos_fallidos).HasDefaultValue(0);
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.nombres)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.numero_documento)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.pais)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.peso_kg).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.salt)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.telefono)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.telefono_emergencia)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.telefono_verificado).HasDefaultValue(false);

            entity.HasOne(d => d.id_tipo_documentoNavigation).WithMany(p => p.usuarios)
                .HasForeignKey(d => d.id_tipo_documento)
                .HasConstraintName("FK__usuarios__id_tip__00200768");

            entity.HasOne(d => d.id_tipo_usuarioNavigation).WithMany(p => p.usuarios)
                .HasForeignKey(d => d.id_tipo_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios__id_tip__7F2BE32F");
        });

        modelBuilder.Entity<usuarios_role>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__usuarios__3213E83F78317FAE");

            entity.HasIndex(e => e.codigo, "UQ__usuarios__40F9A2067F8EC38B").IsUnique();

            entity.HasIndex(e => e.estado, "idx_roles_estado");

            entity.HasIndex(e => e.id_torneo, "idx_roles_torneo");

            entity.HasIndex(e => e.id_usuario, "idx_roles_usuario");

            entity.Property(e => e.activo).HasDefaultValue(true);
            entity.Property(e => e.codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.estado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVO");
            entity.Property(e => e.fecha_asignacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.fecha_modificacion).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.metadata).HasDefaultValue("{}");
            entity.Property(e => e.origen_asignacion)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.id_enlace_origenNavigation).WithMany(p => p.usuarios_roles)
                .HasForeignKey(d => d.id_enlace_origen)
                .HasConstraintName("FK_usuarios_roles_enlace");

            entity.HasOne(d => d.id_equipoNavigation).WithMany(p => p.usuarios_roles)
                .HasForeignKey(d => d.id_equipo)
                .HasConstraintName("FK__usuarios___id_eq__43A1090D");

            entity.HasOne(d => d.id_rolNavigation).WithMany(p => p.usuarios_roles)
                .HasForeignKey(d => d.id_rol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios___id_ro__41B8C09B");

            entity.HasOne(d => d.id_torneoNavigation).WithMany(p => p.usuarios_roles)
                .HasForeignKey(d => d.id_torneo)
                .HasConstraintName("FK__usuarios___id_to__42ACE4D4");

            entity.HasOne(d => d.id_usuarioNavigation).WithMany(p => p.usuarios_roleid_usuarioNavigations)
                .HasForeignKey(d => d.id_usuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__usuarios___id_us__40C49C62");

            entity.HasOne(d => d.id_usuario_asignadorNavigation).WithMany(p => p.usuarios_roleid_usuario_asignadorNavigations)
                .HasForeignKey(d => d.id_usuario_asignador)
                .HasConstraintName("FK__usuarios___id_us__467D75B8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
