using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.Data.SqlClient;
using SkiaSharp;
namespace Inmobiliaria.IntegrationTests;

[CollectionDefinition("Recorrido", DisableParallelization = true)]
public sealed class RecorridoCollection : ICollectionFixture<ApiSqlFixture> { }

[Collection("Recorrido")]
public sealed class RecorridoTests(ApiSqlFixture app)
{
    [Fact]
    public async Task RegistroAprobacionPublicacionSolicitud_RecorridoCompletoConAislamientoYConcurrencia()
    {
        using var vendedor = app.CreateClient();
        using var comprador = app.CreateClient();
        using var otro = app.CreateClient();
        using var admin = app.CreateClient();
        async Task Registrar(HttpClient client, string rol, string correo, string cedula)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/" + rol, new RegistroRequest("Prueba", "Recorrido", correo, ApiSqlFixture.Password, "8095550000", cedula));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        async Task Entrar(HttpClient client, string correo)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(correo, ApiSqlFixture.Password));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        }
        await Registrar(vendedor, "vendedores", "vendedor@recorrido.example", "10000000001");
        await Entrar(vendedor, "vendedor@recorrido.example");
        using var sinAprobacion = await vendedor.GetAsync("/api/v1/propiedades");
        Assert.Equal(HttpStatusCode.Forbidden, sinAprobacion.StatusCode);
        await Entrar(admin, ApiSqlFixture.AdminEmail);
        var pendientes = (await admin.GetFromJsonAsync<List<VendedorPendienteDto>>("/api/v1/admin/vendedores/pendientes"))!;
        using var aprobacion = await admin.PostAsJsonAsync($"/api/v1/admin/vendedores/{Assert.Single(pendientes).IdVendedor}/aprobar", new DecisionRequest(null));
        Assert.Equal(HttpStatusCode.NoContent, aprobacion.StatusCode);
        var tipos = (await vendedor.GetFromJsonAsync<List<TipoPropiedadDto>>("/api/v1/catalogos/tipos"))!;
        var provincias = (await vendedor.GetFromJsonAsync<List<ProvinciaDto>>("/api/v1/catalogos/provincias"))!;
        using var propiedad = await vendedor.PostAsJsonAsync("/api/v1/propiedades", new PropiedadRequest(tipos[0].IdTipoPropiedad, "Casa recorrido", "Descripcion", provincias[0].IdProvincia, "Ciudad", "Sector", "Direccion", 2, 1, 1, 80));
        Assert.Equal(HttpStatusCode.Created, propiedad.StatusCode);
        var idPropiedad = (await propiedad.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idPropiedad").GetInt32();
        using var publicacion = await vendedor.PostAsJsonAsync("/api/v1/publicaciones", new PublicacionRequest(idPropiedad, "Alquiler", 15000, "DOP", null, null));
        Assert.Equal(HttpStatusCode.Created, publicacion.StatusCode);
        var idPublicacion = (await publicacion.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idPublicacion").GetInt32();
        await Registrar(comprador, "compradores", "comprador@recorrido.example", "10000000002");
        await Entrar(comprador, "comprador@recorrido.example");
        var catalogo = (await comprador.GetFromJsonAsync<List<PublicacionResumenDto>>("/api/v1/publicaciones"))!;
        Assert.Contains(catalogo, p => p.IdPublicacion == idPublicacion);
        var solicitud = new SolicitudAlquilerRequest(idPublicacion, " Me interesa visitar la casa ");
        // Dos peticiones simultaneas deben producir una sola solicitud persistida.
        var respuestas = await Task.WhenAll(comprador.PostAsJsonAsync("/api/v1/solicitudes", solicitud), comprador.PostAsJsonAsync("/api/v1/solicitudes", solicitud));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in respuestas) response.Dispose();
        var enviadas = (await comprador.GetFromJsonAsync<List<SolicitudDto>>("/api/v1/solicitudes"))!;
        var recibidas = (await vendedor.GetFromJsonAsync<List<SolicitudDto>>("/api/v1/solicitudes"))!;
        Assert.Equal(Assert.Single(enviadas).IdSolicitud, Assert.Single(recibidas).IdSolicitud);
        Assert.Equal("Pendiente", enviadas[0].Estado);
        Assert.Equal("Me interesa visitar la casa", enviadas[0].Mensaje);
        var rutaRespuesta = $"/api/v1/solicitudes/{enviadas[0].IdSolicitud}/responder";
        using (var response = await comprador.PostAsJsonAsync(rutaRespuesta,new ResponderSolicitudRequest(true,null))) Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
        using (var response = await vendedor.PostAsJsonAsync(rutaRespuesta,new ResponderSolicitudRequest(false," "))) Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        var decisiones = await Task.WhenAll(vendedor.PostAsJsonAsync(rutaRespuesta,new ResponderSolicitudRequest(true,"Aceptada")),vendedor.PostAsJsonAsync(rutaRespuesta,new ResponderSolicitudRequest(false,"No disponible para este interesado")));
        Assert.Single(decisiones,r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(decisiones,r => r.StatusCode == HttpStatusCode.Conflict);
        foreach(var response in decisiones) response.Dispose();
        var respondida = Assert.Single((await comprador.GetFromJsonAsync<List<SolicitudDto>>("/api/v1/solicitudes"))!);
        Assert.Contains(respondida.Estado,new[]{"Aceptada","Rechazada"});
        Assert.NotNull(respondida.Respuesta); Assert.NotNull(respondida.FechaRespuesta);
        // Prepara una solicitud aceptada para el siguiente tramo del recorrido.
        await app.ExecuteSql("UPDATE Solicitudes SET EstadoSolicitud='Aceptada' WHERE IdSolicitud=@id",new SqlParameter("@id",respondida.IdSolicitud));
        var rutaVisitas=$"/api/v1/solicitudes/{respondida.IdSolicitud}/visitas";
        var propuesta=new ProgramarVisitaRequest(DateTimeOffset.UtcNow.AddDays(2),"Entrada principal");
        using(var response=await comprador.PostAsJsonAsync(rutaVisitas,propuesta)) Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
        var propuestas=await Task.WhenAll(vendedor.PostAsJsonAsync(rutaVisitas,propuesta),vendedor.PostAsJsonAsync(rutaVisitas,propuesta));
        Assert.Single(propuestas,r=>r.StatusCode==HttpStatusCode.NoContent);
        Assert.Single(propuestas,r=>r.StatusCode==HttpStatusCode.Conflict);
        foreach(var response in propuestas) response.Dispose();
        var visita=Assert.Single((await comprador.GetFromJsonAsync<List<VisitaDto>>(rutaVisitas))!);
        using(var response=await comprador.PostAsync($"{rutaVisitas}/{visita.IdVisita}/confirmar",null)) Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
        Assert.Equal("Confirmada",Assert.Single((await vendedor.GetFromJsonAsync<List<VisitaDto>>(rutaVisitas))!).Estado);
        using(var response=await comprador.PostAsync($"{rutaVisitas}/{visita.IdVisita}/cancelar",null)) Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Solicitudes WHERE IdPublicacion=@id", new SqlParameter("@id", idPublicacion)));
        await Registrar(otro, "compradores", "otro@recorrido.example", "10000000003");
        await Entrar(otro, "otro@recorrido.example");
        Assert.Empty((await otro.GetFromJsonAsync<List<SolicitudDto>>("/api/v1/solicitudes"))!);
        using var rolIncorrecto = await vendedor.PostAsJsonAsync("/api/v1/solicitudes", solicitud);
        Assert.Equal(HttpStatusCode.Forbidden, rolIncorrecto.StatusCode);

        // La galeria usa archivos reales, decodificacion real y SQL, incluido el limite concurrente.
        using var foto = new SKBitmap(20, 20);
        using var imagenPrueba = SKImage.FromBitmap(foto);
        using var png = imagenPrueba.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        MultipartFormDataContent Archivos(int cantidad, bool corrupta = false)
        {
            var multipart = new MultipartFormDataContent();
            for (var n = 0; n < cantidad; n++) multipart.Add(new ByteArrayContent(corrupta && n == cantidad - 1 ? new byte[] { 1, 2, 3 } : bytes), "archivos", "foto.png");
            return multipart;
        }
        var rutaImagenes = $"/api/v1/propiedades/{idPropiedad}/imagenes";
        using (var demasiadas = Archivos(9))
        using (var response = await vendedor.PostAsync(rutaImagenes, demasiadas)) Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using (var grande = new MultipartFormDataContent())
        {
            grande.Add(new ByteArrayContent(new byte[5 * 1024 * 1024 + 1]), "archivos", "grande.png");
            using var response = await vendedor.PostAsync(rutaImagenes, grande);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using (var invalidas = Archivos(2, true))
        using (var response = await vendedor.PostAsync(rutaImagenes, invalidas)) Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(Directory.GetFiles(app.ImagenesCarpeta));
        using (var content = Archivos(1))
        using (var response = await comprador.PostAsync(rutaImagenes, content)) Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using (var content = Archivos(7))
        using (var response = await vendedor.PostAsync(rutaImagenes, content)) Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var uno = Archivos(1);
        using var dos = Archivos(1);
        var cargas = await Task.WhenAll(vendedor.PostAsync(rutaImagenes, uno), vendedor.PostAsync(rutaImagenes, dos));
        Assert.Single(cargas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(cargas, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var response in cargas) response.Dispose();
        var imagenes = (await vendedor.GetFromJsonAsync<List<ImagenDto>>(rutaImagenes))!;
        Assert.Equal(8, imagenes.Count);
        Assert.Equal(8, Directory.GetFiles(app.ImagenesCarpeta).Length);
        using var anonimo = app.CreateClient();
        var galeria = (await anonimo.GetFromJsonAsync<List<ImagenDto>>($"/api/v1/publicaciones/{idPublicacion}/imagenes"))!;
        Assert.Equal(8, galeria.Count);
        using (var response = await anonimo.GetAsync(galeria[0].Url))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
            using var decoded = SKBitmap.Decode(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(20, decoded.Width);
        }
        using (var response = await vendedor.DeleteAsync(rutaImagenes + "/" + imagenes[0].IdImagen)) Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(7, Directory.GetFiles(app.ImagenesCarpeta).Length);
        using var ajeno = app.CreateClient();
        await Registrar(ajeno, "vendedores", "ajeno@recorrido.example", "10000000004");
        await Entrar(ajeno, "ajeno@recorrido.example");
        var nuevoPendiente = Assert.Single((await admin.GetFromJsonAsync<List<VendedorPendienteDto>>("/api/v1/admin/vendedores/pendientes"))!);
        using (var response = await admin.PostAsJsonAsync($"/api/v1/admin/vendedores/{nuevoPendiente.IdVendedor}/aprobar", new DecisionRequest(null))) Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var response = await ajeno.DeleteAsync(rutaImagenes + "/" + imagenes[1].IdImagen)) Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using (var content = Archivos(1))
        using (var response = await ajeno.PostAsync(rutaImagenes, content)) Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await app.ExecuteSql("UPDATE Publicaciones SET EstadoPublicacion='Pausada' WHERE IdPublicacion=@id", new SqlParameter("@id", idPublicacion));
        using (var response = await anonimo.GetAsync(galeria[1].Url)) Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var noDisponible = await otro.PostAsJsonAsync("/api/v1/solicitudes", solicitud);
        Assert.Equal(HttpStatusCode.Conflict, noDisponible.StatusCode);
    }
}
