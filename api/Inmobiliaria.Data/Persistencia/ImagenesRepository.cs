using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.EntityFrameworkCore;
#if DATABASE_FIRST_GENERATED
using Inmobiliaria.Data.Entities.DatabaseFirst;
#else
using Inmobiliaria.Data.Entities;
#endif
namespace Inmobiliaria.Data.Persistencia;
internal sealed class ImagenesRepository(ContextoDatos db) : IImagenesRepository
{
    public Task<bool> EsPropietario(int propiedad, int vendedor) => db.Propiedades.AnyAsync(p => p.IdPropiedad == propiedad && p.IdVendedor == vendedor && p.EliminadoEn == null);
    public Task<List<ImagenDto>> Listar(int propiedad) => db.ImagenesPropiedades.AsNoTracking().Where(i => i.IdPropiedad == propiedad)
        .OrderBy(i => i.Orden).ThenBy(i => i.IdImagen).Select(i => new ImagenDto(i.IdImagen, i.UrlImagen, i.Orden)).ToListAsync();
    public async Task<bool> Agregar(int propiedad, int vendedor, IReadOnlyList<string> nombres)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedad}");
        if (!await EsPropietario(propiedad, vendedor)) return false;
        var actuales = await db.ImagenesPropiedades.Where(i => i.IdPropiedad == propiedad).ToListAsync();
        if (nombres.Count < 1 || actuales.Count + nombres.Count > 8) return false;
        var orden = actuales.Count == 0 ? 0 : actuales.Max(i => i.Orden) + 1;
        foreach (var nombre in nombres) db.ImagenesPropiedades.Add(new ImagenesPropiedades {
            IdPropiedad = propiedad, UrlImagen = nombre, Orden = orden++, FechaSubida = DateTime.UtcNow,
            EsPrincipal = false
        });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    }
    public async Task<string?> Eliminar(int propiedad, int vendedor, int imagen)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT IdPropiedad FROM dbo.Propiedades WITH (UPDLOCK,HOLDLOCK) WHERE IdPropiedad={propiedad}");
        if (!await EsPropietario(propiedad, vendedor)) return null;
        var item = await db.ImagenesPropiedades.SingleOrDefaultAsync(i => i.IdImagen == imagen && i.IdPropiedad == propiedad);
        if (item is null) return null;
        db.ImagenesPropiedades.Remove(item); await db.SaveChangesAsync(); await tx.CommitAsync(); return item.UrlImagen;
    }
    public Task<int?> PropiedadPublica(int publicacion)
    {
        var ahora = DateTime.UtcNow;
        return (from a in db.Publicaciones join p in db.Propiedades on a.IdPropiedad equals p.IdPropiedad
            join v in db.Vendedores on p.IdVendedor equals v.IdVendedor
            join u in db.Usuarios on v.IdUsuario equals u.IdUsuario
            where a.IdPublicacion == publicacion && a.EstadoPublicacion == "Publicada" && a.TipoOperacion == "Alquiler"
                && p.EliminadoEn == null && p.EstadoPropiedad == "Disponible" && v.CuentaAprobada && v.EstadoAprobacion == "Aprobado" && u.EstadoCuenta == "Activa"
                && !db.Suspensiones.Any(s => s.IdUsuario == u.IdUsuario && s.Activa && s.FechaInicio <= ahora && (s.FechaFin == null || s.FechaFin > ahora))
            select (int?)p.IdPropiedad).SingleOrDefaultAsync();
    }
}
