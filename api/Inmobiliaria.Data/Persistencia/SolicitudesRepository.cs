using System.Data;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;

internal sealed class SolicitudesRepository(ContextoDatos db) : ISolicitudesRepository
{
    public async Task<string?> Responder(int vendedor, int solicitud, bool aceptar, string? motivo)
    {
        var propiedadId = await (from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
            select new { s.IdSolicitud, a.IdPropiedad }).Where(x => x.IdSolicitud == solicitud).Select(x => (int?)x.IdPropiedad).SingleOrDefaultAsync();
        if(propiedadId is null) return "NO_ENCONTRADA";
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedadId.Value}");
        var propiedad = await db.Propiedades.SingleOrDefaultAsync(p => p.IdPropiedad == propiedadId && p.IdVendedor == vendedor);
        if(propiedad is null) return "NO_ENCONTRADA";
        var entidad = await db.Solicitudes.SingleAsync(s => s.IdSolicitud == solicitud);
        var anuncio = await db.Publicaciones.SingleAsync(a => a.IdPublicacion == entidad.IdPublicacion);
        if(entidad.EstadoSolicitud != "Pendiente" || propiedad.EliminadoEn != null || propiedad.EstadoPropiedad != "Disponible" || anuncio.EstadoPublicacion != "Publicada") return "NO_DISPONIBLE";
        entidad.EstadoSolicitud = aceptar ? "Aceptada" : "Rechazada";
        entidad.Respuesta = motivo; entidad.FechaRespuesta = DateTime.UtcNow;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }
    public async Task<(EstadoSolicitudCreada Estado, int Id)> Crear(int idUsuario, SolicitudAlquilerRequest solicitud, DateTime ahora)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // Serializa envios del mismo comprador, incluidos los simultaneos, antes de comprobar duplicados.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdComprador FROM dbo.Compradores WITH (UPDLOCK, HOLDLOCK) WHERE IdUsuario = {idUsuario}");
        var comprador = await db.Compradores.SingleOrDefaultAsync(c => c.IdUsuario == idUsuario);
        if (comprador is null) return (EstadoSolicitudCreada.SinComprador, 0);
        var disponible = await (
            from anuncio in db.Publicaciones
            join propiedad in db.Propiedades on anuncio.IdPropiedad equals propiedad.IdPropiedad
            join vendedor in db.Vendedores on propiedad.IdVendedor equals vendedor.IdVendedor
            join usuario in db.Usuarios on vendedor.IdUsuario equals usuario.IdUsuario
            join tipo in db.TiposPropiedad on propiedad.IdTipoPropiedad equals tipo.IdTipoPropiedad
            where anuncio.IdPublicacion == solicitud.IdPublicacion && anuncio.TipoOperacion == "Alquiler"
                && propiedad.EliminadoEn == null && anuncio.EstadoPublicacion == "Publicada" && propiedad.EstadoPropiedad == "Disponible"
                && vendedor.CuentaAprobada && vendedor.EstadoAprobacion == "Aprobado" && usuario.EstadoCuenta == "Activa"
                && (tipo.Nombre == "Casa" || tipo.Nombre == "Apartamento")
                && !db.Suspensiones.Any(s => s.IdUsuario == usuario.IdUsuario && s.Activa && s.FechaInicio <= ahora
                    && (s.FechaFin == null || s.FechaFin > ahora))
            select anuncio.IdPublicacion).AnyAsync();
        if (!disponible) return (EstadoSolicitudCreada.NoDisponible, 0);
        if (await db.Solicitudes.AnyAsync(s => s.IdComprador == comprador.IdComprador
            && s.IdPublicacion == solicitud.IdPublicacion && (s.EstadoSolicitud == "Pendiente" || s.EstadoSolicitud == "EnRevision" || s.EstadoSolicitud == "Aceptada")))
            return (EstadoSolicitudCreada.Duplicada, 0);
        var entidad = new Solicitudes { IdComprador = comprador.IdComprador, IdPublicacion = solicitud.IdPublicacion,
            TipoSolicitud = "Alquiler", Mensaje = solicitud.Mensaje, EstadoSolicitud = "Pendiente", FechaSolicitud = ahora };
        db.Solicitudes.Add(entidad);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (EstadoSolicitudCreada.Creada, entidad.IdSolicitud);
    }

    public Task<List<SolicitudDto>> Listar(int idUsuario, bool vendedor) => (
        from s in db.Solicitudes.AsNoTracking()
        join anuncio in db.Publicaciones on s.IdPublicacion equals anuncio.IdPublicacion
        join propiedad in db.Propiedades on anuncio.IdPropiedad equals propiedad.IdPropiedad
        join v in db.Vendedores on propiedad.IdVendedor equals v.IdVendedor
        join c in db.Compradores on s.IdComprador equals c.IdComprador
        join u in db.Usuarios on c.IdUsuario equals u.IdUsuario
        where vendedor ? v.IdUsuario == idUsuario : c.IdUsuario == idUsuario
        orderby s.IdSolicitud descending
        select new SolicitudDto(s.IdSolicitud, s.IdPublicacion, propiedad.Titulo,
            u.Nombre + " " + u.Apellido, s.EstadoSolicitud, s.Mensaje, s.FechaSolicitud,
            propiedad.EliminadoEn != null || anuncio.EstadoPublicacion != "Publicada", s.Respuesta, s.FechaRespuesta)).ToListAsync();
}
