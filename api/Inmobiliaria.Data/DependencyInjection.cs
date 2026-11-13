using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Data.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Inmobiliaria.Data;

public static class DependencyInjection
{
    public static string NombreContexto => typeof(ContextoDatos).Name;

    public static IServiceCollection AddInmobiliariaData(this IServiceCollection servicios, string cadena)
    {
        servicios.AddDbContext<ContextoDatos>(opciones => opciones.UseSqlServer(cadena));
        servicios.AddScoped<IPropiedadRepository, PropiedadRepository>();
        servicios.AddScoped<IArchivoRepository, PropiedadRepository>();
        servicios.AddScoped<ICuentasRepository, CuentasRepository>();
        servicios.AddScoped<ISolicitudesRepository, SolicitudesRepository>();
        servicios.AddScoped<IVisitasRepository, VisitasRepository>();
        servicios.AddScoped<IImagenesRepository, ImagenesRepository>();
        return servicios;
    }
}
