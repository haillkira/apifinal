using System.Linq.Expressions;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif

namespace Inmobiliaria.Data.Persistencia;

internal sealed class PropiedadRepository(ContextoDatos db) : IPropiedadRepository
{
    private static readonly Expression<Func<Propiedades, PropiedadDto>> Proyectar = p => new PropiedadDto
    {
        IdPropiedad = p.IdPropiedad,
        IdTipoPropiedad = p.IdTipoPropiedad,
        IdProvincia = p.IdProvincia,
        Titulo = p.Titulo,
        Descripcion = p.Descripcion,
        Ciudad = p.Ciudad,
        Sector = p.Sector,
        Direccion = p.Direccion,
        Habitaciones = p.Habitaciones,
        Banos = p.Banos,
        Parqueos = p.Parqueos,
        MetrosCuadrados = p.MetrosCuadrados,
        EstadoPropiedad = p.EstadoPropiedad
    };

    public Task<string?> NombreTipo(int id) => db.TiposPropiedad.AsNoTracking()
        .Where(t => t.IdTipoPropiedad == id).Select(t => t.Nombre).FirstOrDefaultAsync();

    public Task<bool> ExisteProvincia(int id) => db.Provincias.AnyAsync(p => p.IdProvincia == id);

    public async Task<int?> Guardar(int idVendedor, PropiedadDto propiedad, DateTime ahora)
    {
        Propiedades entidad;
        if (propiedad.IdPropiedad == 0)
        {
            entidad = new Propiedades
            {
                IdVendedor = idVendedor,
                EstadoPropiedad = "Disponible",
                FechaCreacion = ahora
            };
            db.Propiedades.Add(entidad);
        }
        else
        {
            var actual = await Propias(idVendedor).SingleOrDefaultAsync(p => p.IdPropiedad == propiedad.IdPropiedad);
            if (actual is null)
                return null;

            entidad = actual;
            entidad.ActualizadoEn = ahora;
        }

        entidad.IdTipoPropiedad = propiedad.IdTipoPropiedad;
        entidad.IdProvincia = propiedad.IdProvincia;
        entidad.Titulo = propiedad.Titulo;
        entidad.Descripcion = propiedad.Descripcion;
        entidad.Ciudad = propiedad.Ciudad;
        entidad.Sector = propiedad.Sector;
        entidad.Direccion = propiedad.Direccion;
        entidad.Habitaciones = propiedad.Habitaciones;
        entidad.Banos = propiedad.Banos;
        entidad.Parqueos = propiedad.Parqueos;
        entidad.MetrosCuadrados = propiedad.MetrosCuadrados;
        await db.SaveChangesAsync();
        return entidad.IdPropiedad;
    }

    public Task<PropiedadDto?> Buscar(int idVendedor, int id) => Propias(idVendedor).AsNoTracking()
        .Where(p => p.IdPropiedad == id).Select(Proyectar).SingleOrDefaultAsync();

    public Task<List<PropiedadDto>> Listar(int idVendedor, Expression<Func<PropiedadDto, bool>> criterio) =>
        Propias(idVendedor).AsNoTracking().Select(Proyectar).Where(criterio)
            .OrderByDescending(p => p.IdPropiedad).ToListAsync();

    public async Task<EstadoEliminacion> Eliminar(int idVendedor, int id)
    {
        var propiedad = await Propias(idVendedor).SingleOrDefaultAsync(p => p.IdPropiedad == id);
        if (propiedad is null)
            return EstadoEliminacion.NoEncontrada;

        if (await db.Publicaciones.AnyAsync(p => p.IdPropiedad == id))
            return EstadoEliminacion.ConPublicaciones;

        db.Propiedades.Remove(propiedad);
        await db.SaveChangesAsync();
        return EstadoEliminacion.Eliminada;
    }

    public Task<List<TipoPropiedadDto>> TiposDeAlquiler() => db.TiposPropiedad.AsNoTracking()
        .Where(t => t.Nombre == "Apartamento" || t.Nombre == "Casa")
        .Select(t => new TipoPropiedadDto(t.IdTipoPropiedad, t.Nombre)).ToListAsync();

    public Task<List<ProvinciaDto>> Provincias() => db.Provincias.AsNoTracking().OrderBy(p => p.Nombre)
        .Select(p => new ProvinciaDto(p.IdProvincia, p.Nombre)).ToListAsync();

    public Task<PropiedadPublicable?> BuscarPublicable(int idVendedor, int idPropiedad) =>
        Propias(idVendedor).AsNoTracking().Where(p => p.IdPropiedad == idPropiedad)
            .Select(p => new PropiedadPublicable(p.EstadoPropiedad,
                db.TiposPropiedad.Where(t => t.IdTipoPropiedad == p.IdTipoPropiedad)
                    .Select(t => t.Nombre).FirstOrDefault()))
            .SingleOrDefaultAsync();

    public async Task<int> Publicar(PublicacionRequest solicitud, DateTime ahora)
    {
        var publicacion = new Publicaciones
        {
            IdPropiedad = solicitud.IdPropiedad,
            TipoOperacion = "Alquiler",
            Precio = solicitud.Precio,
            Moneda = solicitud.Moneda,
            Deposito = solicitud.Deposito,
            MantenimientoMensual = solicitud.MantenimientoMensual,
            EstadoPublicacion = "Publicada",
            FechaCreacion = ahora,
            FechaPublicacion = ahora
        };
        db.Publicaciones.Add(publicacion);
        await db.SaveChangesAsync();
        return publicacion.IdPublicacion;
    }

    public Task<List<PublicacionResumenDto>> Catalogo(string? moneda, int pagina, DateTime ahora)
    {
        var consulta =
            from anuncio in db.Publicaciones.AsNoTracking()
            join propiedad in db.Propiedades on anuncio.IdPropiedad equals propiedad.IdPropiedad
            join vendedor in db.Vendedores on propiedad.IdVendedor equals vendedor.IdVendedor
            join usuario in db.Usuarios on vendedor.IdUsuario equals usuario.IdUsuario
            join tipo in db.TiposPropiedad on propiedad.IdTipoPropiedad equals tipo.IdTipoPropiedad
            where anuncio.TipoOperacion == "Alquiler" && anuncio.EstadoPublicacion == "Publicada"
                && propiedad.EstadoPropiedad == "Disponible" && vendedor.CuentaAprobada
                && vendedor.EstadoAprobacion == "Aprobado" && usuario.EstadoCuenta == "Activa"
                && (tipo.Nombre == "Apartamento" || tipo.Nombre == "Casa")
                && (moneda == null || anuncio.Moneda == moneda)
                && !db.Suspensiones.Any(s => s.IdUsuario == usuario.IdUsuario && s.Activa
                    && s.FechaInicio <= ahora && (s.FechaFin == null || s.FechaFin > ahora))
            orderby anuncio.IdPublicacion descending
            select new PublicacionResumenDto(anuncio.IdPublicacion, propiedad.Titulo, propiedad.Ciudad,
                propiedad.Sector, propiedad.Habitaciones, propiedad.Banos, anuncio.Precio, anuncio.Moneda,
                anuncio.Deposito, anuncio.MantenimientoMensual);

        return consulta.Skip((pagina - 1) * 20).Take(20).ToListAsync();
    }

    private IQueryable<Propiedades> Propias(int idVendedor) => db.Propiedades.Where(p => p.IdVendedor == idVendedor);
}
