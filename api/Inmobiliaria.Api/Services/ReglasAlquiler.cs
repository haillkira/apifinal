namespace Inmobiliaria.Api.Services;
public static class ReglasAlquiler
{
    public static bool TipoPermitido(string tipo) => tipo is "Apartamento" or "Casa";
    public static void ValidarOferta(string operacion,string moneda,decimal precio,decimal? deposito,decimal? mantenimiento)
    {
        if (operacion != "Alquiler") throw new ArgumentException("Esta version solo permite alquiler.");
        if (moneda is not ("DOP" or "USD")) throw new ArgumentException("Moneda permitida: DOP o USD.");
        if (precio <= 0 || deposito < 0 || mantenimiento < 0) throw new ArgumentException("Revisa los importes.");
    }
}
