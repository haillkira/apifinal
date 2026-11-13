using Inmobiliaria.Application;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
namespace Inmobiliaria.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ImagenesController(IImagenesRepository imagenes, AccesoService acceso, IWebHostEnvironment entorno, IConfiguration config) : ControllerBase
{
    private const long Maximo = 5 * 1024 * 1024;
    private string Carpeta => config["Imagenes:Carpeta"] ?? Path.Combine(entorno.ContentRootPath, "App_Data", "imagenes");
    private string Ruta(string nombre)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(nombre, "^[a-f0-9]{32}\\.jpg$")) throw new ArgumentException("Imagen no válida.");
        return Path.Combine(Carpeta, nombre);
    }
    private async Task<bool> Propia(int id) => await imagenes.EsPropietario(id, await acceso.Vendedor(User));

    [Authorize, HttpGet("propiedades/{id:int}/imagenes")]
    public async Task<IActionResult> Listar(int id) => !await Propia(id) ? NotFound() : Ok(await imagenes.Listar(id));

    [Authorize, HttpPost("propiedades/{id:int}/imagenes"), RequestSizeLimit(43 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 43 * 1024 * 1024)]
    public async Task<IActionResult> Subir(int id, [FromForm] List<IFormFile> archivos)
    {
        var vendedor = await acceso.Vendedor(User);
        if (!await imagenes.EsPropietario(id, vendedor)) return NotFound();
        if (archivos.Count is < 1 or > 8 || (await imagenes.Listar(id)).Count + archivos.Count > 8)
            return Conflict(new { mensaje = "Máximo 8 imágenes. Elimina alguna antes de agregar más." });
        var nombres = new List<string>();
        var guardado = false;
        try
        {
            Directory.CreateDirectory(Carpeta);
            foreach (var archivo in archivos)
            {
                if (archivo.Length is <= 0 or > Maximo) return BadRequest(new { mensaje = "Cada imagen debe pesar como máximo 5 MB." });
                await using var input = archivo.OpenReadStream();
                using var data = SKData.Create(input);
                using var codec = SKCodec.Create(data);
                if (codec is null) return BadRequest(new { mensaje = "El archivo no es una imagen válida." });
                var info = codec.Info;
                if ((long)info.Width * info.Height > 24_000_000 || info.Width > 12000 || info.Height > 12000)
                    return BadRequest(new { mensaje = "La imagen supera los 24 megapíxeles permitidos." });
                if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                    return BadRequest(new { mensaje = "Selecciona imágenes JPG, PNG o WebP." });
                using var bitmap = SKBitmap.Decode(codec);
                if (bitmap is null) return BadRequest(new { mensaje = "La imagen está dañada." });
                var girada = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
                var ancho = girada ? info.Height : info.Width;
                var alto = girada ? info.Width : info.Height;
                var escala = Math.Min(1d, 1600d / Math.Max(ancho, alto));
                using var surface = SKSurface.Create(new SKImageInfo(Math.Max(1, (int)(ancho * escala)), Math.Max(1, (int)(alto * escala))));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                canvas.Scale((float)escala);
                switch (codec.EncodedOrigin)
                {
                    case SKEncodedOrigin.TopRight: canvas.Translate(ancho, 0); canvas.Scale(-1, 1); break;
                    case SKEncodedOrigin.BottomRight: canvas.Translate(ancho, alto); canvas.RotateDegrees(180); break;
                    case SKEncodedOrigin.BottomLeft: canvas.Translate(0, alto); canvas.Scale(1, -1); break;
                    case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                    case SKEncodedOrigin.RightTop: canvas.Translate(ancho, 0); canvas.RotateDegrees(90); break;
                    case SKEncodedOrigin.RightBottom: canvas.Translate(ancho, alto); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                    case SKEncodedOrigin.LeftBottom: canvas.Translate(0, alto); canvas.RotateDegrees(270); break;
                }
                canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
                using var imagen = surface.Snapshot();
                using var jpeg = imagen.Encode(SKEncodedImageFormat.Jpeg, 85);
                var nombre = Guid.NewGuid().ToString("N") + ".jpg";
                nombres.Add(nombre);
                await using var salida = System.IO.File.Create(Ruta(nombre));
                jpeg.SaveTo(salida);
            }
            guardado = await imagenes.Agregar(id, vendedor, nombres);
            return guardado ? Ok(await imagenes.Listar(id)) : Conflict(new { mensaje = "Máximo 8 imágenes. Actualiza el listado." });
        }
        finally { if (!guardado) foreach (var nombre in nombres) System.IO.File.Delete(Ruta(nombre)); }
    }

    [Authorize, HttpDelete("propiedades/{id:int}/imagenes/{imagen:int}")]
    public async Task<IActionResult> Eliminar(int id, int imagen)
    {
        var nombre = await imagenes.Eliminar(id, await acceso.Vendedor(User), imagen);
        if (nombre is null) return NotFound();
        System.IO.File.Delete(Ruta(nombre)); return NoContent();
    }

    [Authorize, HttpGet("propiedades/{id:int}/imagenes/{imagen:int}/archivo")]
    public async Task<IActionResult> Privada(int id, int imagen)
    {
        if (!await Propia(id)) return NotFound();
        return await Archivo(id, imagen);
    }

    [HttpGet("publicaciones/{id:int}/imagenes")]
    public async Task<IActionResult> Publicas(int id)
    {
        var propiedad = await imagenes.PropiedadPublica(id);
        if (propiedad is null) return NotFound();
        return Ok((await imagenes.Listar(propiedad.Value)).Select(i => i with { Url = $"api/v1/publicaciones/{id}/imagenes/{i.IdImagen}/archivo" }));
    }

    [HttpGet("publicaciones/{id:int}/imagenes/{imagen:int}/archivo")]
    public async Task<IActionResult> Publica(int id, int imagen)
    {
        var propiedad = await imagenes.PropiedadPublica(id);
        return propiedad is null ? NotFound() : await Archivo(propiedad.Value, imagen);
    }
    private async Task<IActionResult> Archivo(int propiedad, int imagen)
    {
        var item = (await imagenes.Listar(propiedad)).SingleOrDefault(i => i.IdImagen == imagen);
        if (item is null) return NotFound();
        var ruta = Ruta(item.Url);
        if (!System.IO.File.Exists(ruta)) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(ruta, "image/jpeg");
    }
}
