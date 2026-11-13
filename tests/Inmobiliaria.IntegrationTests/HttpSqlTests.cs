using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.Data.SqlClient;

namespace Inmobiliaria.IntegrationTests;

[Collection("HTTP y SQL")]
public sealed class HttpSqlTests(ApiSqlFixture app)
{
    private static int documento;
    private HttpClient Cliente() => app.CreateClient();
    private async Task<LoginResponse> Login(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, ApiSqlFixture.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login;
    }

    private async Task<(string Email, int Usuario)> Registrar(HttpClient client, string rol = "vendedores")
    {
        var email = Guid.NewGuid().ToString("N") + "@integration.example";
        using var response = await client.PostAsJsonAsync("/api/v1/auth/" + rol,
            new RegistroRequest("Ana", "Perez", email, ApiSqlFixture.Password, "8095550000", Interlocked.Increment(ref documento).ToString("D11")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var usuario = (await response.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        return (email, usuario.IdUsuario);
    }

    private async Task Aprobar(int usuario)
    {
        using var admin = Cliente();
        await Login(admin, ApiSqlFixture.AdminEmail);
        var pendientes = (await admin.GetFromJsonAsync<List<VendedorPendienteDto>>("/api/v1/admin/vendedores/pendientes"))!;
        var vendedor = Assert.Single(pendientes, p => p.IdUsuario == usuario);
        using var response = await admin.PostAsJsonAsync($"/api/v1/admin/vendedores/{vendedor.IdVendedor}/aprobar", new DecisionRequest(null));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM HistorialAprobaciones WHERE IdVendedor=@id AND Accion='Aprobado'", new SqlParameter("@id", vendedor.IdVendedor)));
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Notificaciones WHERE IdUsuarioDestino=@id", new SqlParameter("@id", usuario)));
    }

    private async Task<PropiedadRequest> Datos(HttpClient client)
    {
        var tipos = (await client.GetFromJsonAsync<List<TipoPropiedadDto>>("/api/v1/catalogos/tipos"))!;
        var provincias = (await client.GetFromJsonAsync<List<ProvinciaDto>>("/api/v1/catalogos/provincias"))!;
        return new(tipos.First(t => t.Nombre == "Casa").IdTipoPropiedad, "Casa de prueba", "Descripcion", provincias.First().IdProvincia,
            "Santo Domingo", "Centro", "Calle de prueba", 2, 1, 1, 80);
    }

    private static async Task<int> Id(HttpResponseMessage response, string nombre)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty(nombre).GetInt32();
    }

    [Fact]
    public async Task RegistroLoginYPerfil_PersistenCuentaYRechazanDuplicado()
    {
        using var client = Cliente();
        var cuenta = await Registrar(client, "compradores");
        await Login(client, cuenta.Email);
        var perfil = await client.GetFromJsonAsync<UsuarioResponse>("/api/v1/auth/me");
        Assert.Equal(cuenta.Usuario, perfil!.IdUsuario);
        Assert.Equal("Comprador", perfil.Rol);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Usuarios WHERE IdUsuario=@id AND UltimoAcceso IS NOT NULL AND PasswordHash<>@password", new SqlParameter("@id", cuenta.Usuario), new SqlParameter("@password", ApiSqlFixture.Password)));
        using var duplicate = await client.PostAsJsonAsync("/api/v1/auth/compradores", new RegistroRequest("Ana", "Perez", cuenta.Email, ApiSqlFixture.Password, "8095550000", "99999999999"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(cuenta.Email, "incorrecta"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task AutorizacionYValidacion_Devuelven401403Y400()
    {
        using var client = Cliente();
        using var anonymous = await client.GetAsync("/api/v1/propiedades");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var cuenta = await Registrar(client);
        await Login(client, cuenta.Email);
        using var pending = await client.PostAsJsonAsync("/api/v1/propiedades", await Datos(client));
        Assert.Equal(HttpStatusCode.Forbidden, pending.StatusCode);
        using var admin = await client.GetAsync("/api/v1/admin/vendedores/pendientes");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/compradores", new { });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Propiedades_CRUDPersisteEnSqlYSeparaVendedores()
    {
        using var client = Cliente();
        var cuenta = await Registrar(client);
        await Aprobar(cuenta.Usuario);
        await Login(client, cuenta.Email);
        var datos = await Datos(client);
        using var create = await client.PostAsJsonAsync("/api/v1/propiedades", datos);
        var id = await Id(create, "idPropiedad");
        using var update = await client.PutAsJsonAsync($"/api/v1/propiedades/{id}", datos with { Titulo = "Actualizada" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal("Actualizada", (await client.GetFromJsonAsync<PropiedadDto>($"/api/v1/propiedades/{id}"))!.Titulo);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Propiedades WHERE IdPropiedad=@id AND Titulo=N'Actualizada'", new SqlParameter("@id", id)));
        using var otro = Cliente();
        var segunda = await Registrar(otro);
        await Aprobar(segunda.Usuario);
        await Login(otro, segunda.Email);
        using var forbiddenRead = await otro.GetAsync($"/api/v1/propiedades/{id}");
        Assert.Equal(HttpStatusCode.NotFound, forbiddenRead.StatusCode);
        using var forbiddenWrite = await otro.PutAsJsonAsync($"/api/v1/propiedades/{id}", datos);
        Assert.Equal(HttpStatusCode.NotFound, forbiddenWrite.StatusCode);
        using var forbiddenDelete = await otro.DeleteAsync($"/api/v1/propiedades/{id}");
        Assert.Equal(HttpStatusCode.NotFound, forbiddenDelete.StatusCode);
        using var delete = await client.DeleteAsync($"/api/v1/propiedades/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, await app.Scalar("SELECT COUNT(*) FROM Propiedades WHERE IdPropiedad=@id AND EliminadoEn IS NULL", new SqlParameter("@id", id)));
    }

    [Fact]
    public async Task Publicacion_ApareceEnCatalogoYBloqueaEliminarPropiedad()
    {
        using var client = Cliente();
        var cuenta = await Registrar(client);
        await Aprobar(cuenta.Usuario);
        await Login(client, cuenta.Email);
        using var create = await client.PostAsJsonAsync("/api/v1/propiedades", await Datos(client));
        var id = await Id(create, "idPropiedad");
        using var publish = await client.PostAsJsonAsync("/api/v1/publicaciones", new PublicacionRequest(id, "Alquiler", 15250.75m, "DOP", 15250.75m, null));
        var publicacion = await Id(publish, "idPublicacion");
        using var publico = Cliente();
        var lista = (await publico.GetFromJsonAsync<List<PublicacionResumenDto>>("/api/v1/publicaciones?moneda=DOP"))!;
        Assert.Equal(15250.75m, Assert.Single(lista, p => p.IdPublicacion == publicacion).Precio);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Publicaciones WHERE IdPublicacion=@id AND Precio=15250.75", new SqlParameter("@id", publicacion)));
        using var delete = await client.DeleteAsync($"/api/v1/propiedades/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Propiedades WHERE IdPropiedad=@id AND EliminadoEn IS NOT NULL", new SqlParameter("@id", id)));
        using var oculta = await client.GetAsync($"/api/v1/propiedades/{id}");
        Assert.Equal(HttpStatusCode.NotFound, oculta.StatusCode);
        using var restaurar = await client.PostAsync($"/api/v1/propiedades/{id}/restaurar", null);
        Assert.Equal(HttpStatusCode.NoContent, restaurar.StatusCode);
        Assert.Equal(1, await app.Scalar("SELECT COUNT(*) FROM Publicaciones WHERE IdPublicacion=@id AND EstadoPublicacion='Pausada'", new SqlParameter("@id", publicacion)));
        using var reactivar = await client.PostAsync($"/api/v1/publicaciones/{publicacion}/reactivar", null);
        Assert.Equal(HttpStatusCode.NoContent, reactivar.StatusCode);
        using var retirar = await client.PostAsync($"/api/v1/publicaciones/{publicacion}/retirar", null);
        Assert.Equal(HttpStatusCode.NoContent, retirar.StatusCode);
        using var invalid = await publico.GetAsync("/api/v1/publicaciones?pagina=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Sql_ImponeUnicidadYClaveForanea()
    {
        var duplicate = await Assert.ThrowsAsync<SqlException>(() => app.ExecuteSql("INSERT INTO Roles(Nombre) VALUES(N'Administrador')"));
        Assert.Contains(duplicate.Number, new[] { 2601, 2627 });
        var foreignKey = await Assert.ThrowsAsync<SqlException>(() => app.ExecuteSql("INSERT INTO Administradores(IdUsuario,NivelPermiso) VALUES(-1,N'Completo')"));
        Assert.Equal(547, foreignKey.Number);
    }

    [Fact]
    public async Task Sql_TransaccionRevertidaNoDejaDatos()
    {
        await using var connection = new SqlConnection(app.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var command = new SqlCommand("INSERT INTO Roles(Nombre) VALUES(N'RollbackTest')", connection, transaction);
        await command.ExecuteNonQueryAsync();
        await transaction.RollbackAsync();
        Assert.Equal(0, await app.Scalar("SELECT COUNT(*) FROM Roles WHERE Nombre=N'RollbackTest'"));
    }
}
