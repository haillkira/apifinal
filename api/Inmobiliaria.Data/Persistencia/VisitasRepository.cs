using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;
internal sealed class VisitasRepository(ContextoDatos db) : IVisitasRepository
{
    private IQueryable<Solicitudes> Accesibles(int usuario,int solicitud) =>
        from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion
        join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad
        join v in db.Vendedores on p.IdVendedor equals v.IdVendedor
        join c in db.Compradores on s.IdComprador equals c.IdComprador
        where s.IdSolicitud == solicitud && (v.IdUsuario == usuario || c.IdUsuario == usuario) select s;
    public async Task<List<VisitaDto>?> Listar(int usuario,int solicitud) {
        if(!await Accesibles(usuario,solicitud).AnyAsync()) return null;
        return await db.Visitas.AsNoTracking().Where(v => v.IdSolicitud == solicitud).OrderByDescending(v => v.IdVisita)
            .Select(v => new VisitaDto(v.IdVisita,v.FechaVisita,v.EstadoVisita,v.Notas)).ToListAsync();
    }
    public async Task<string?> Cambiar(int usuario,int solicitud,ProgramarVisitaRequest? nueva,int? visita,bool confirmar) {
        if(!await Accesibles(usuario,solicitud).AnyAsync()) return "NO_ENCONTRADA";
        var propiedadId = await (from s in db.Solicitudes join a in db.Publicaciones on s.IdPublicacion equals a.IdPublicacion where s.IdSolicitud == solicitud select a.IdPropiedad).SingleAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedadId}");
        var solicitudActual = await Accesibles(usuario,solicitud).SingleAsync();
        var propiedad = await db.Propiedades.SingleAsync(p => p.IdPropiedad == propiedadId);
        var vendedor = await db.Vendedores.SingleAsync(v => v.IdVendedor == propiedad.IdVendedor);
        var anuncio = await db.Publicaciones.SingleAsync(a => a.IdPublicacion == solicitudActual.IdPublicacion);
        if(nueva is not null || confirmar) {
            if(solicitudActual.EstadoSolicitud != "Aceptada" || propiedad.EliminadoEn != null || propiedad.EstadoPropiedad != "Disponible" || anuncio.EstadoPublicacion != "Publicada") return "CONFLICTO";
        }
        if(nueva is not null) {
            if(vendedor.IdUsuario != usuario) return "PROHIBIDO";
            if(nueva.Fecha <= DateTimeOffset.UtcNow) return "DATOS_INVALIDOS";
            if(await db.Visitas.AnyAsync(v => v.IdSolicitud == solicitud && (v.EstadoVisita == "Solicitada" || v.EstadoVisita == "Confirmada"))) return "CONFLICTO";
            db.Visitas.Add(new Visitas { IdSolicitud=solicitud,FechaVisita=nueva.Fecha.UtcDateTime,EstadoVisita="Solicitada",Notas=nueva.Notas?.Trim(),FechaCreacion=DateTime.UtcNow });
        } else {
            var entidad = await db.Visitas.SingleOrDefaultAsync(v => v.IdVisita == visita && v.IdSolicitud == solicitud);
            if(entidad is null) return "NO_ENCONTRADA";
            if(entidad.EstadoVisita is not ("Solicitada" or "Confirmada") || entidad.FechaVisita <= DateTime.UtcNow) return "CONFLICTO";
            if(confirmar && vendedor.IdUsuario == usuario) return "PROHIBIDO";
            if(confirmar && entidad.EstadoVisita != "Solicitada") return "CONFLICTO";
            entidad.EstadoVisita = confirmar ? "Confirmada" : "Cancelada";
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }
}
