using System.Text.RegularExpressions;
using Inmobiliaria.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Inmobiliaria.IntegrationTests;

public sealed class ApiSqlFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Always local, unique and disposable. Never reads application/user-secret SQL connections.
    private readonly string database = "Inmobiliaria_Integration_" + Guid.NewGuid().ToString("N");
    private const string Master = @"Server=.\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true";
    public string ImagenesCarpeta { get; } = Path.Combine(Path.GetTempPath(), "Inmobiliaria_Imagenes_" + Guid.NewGuid().ToString("N"));
    public string ConnectionString => new SqlConnectionStringBuilder(Master) { InitialCatalog = database }.ConnectionString;
    public const string AdminEmail = "admin@integration.example";
    public const string Password = "Integration-password-2026!";
    private bool created;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Imagenes:Carpeta", ImagenesCarpeta);
        builder.UseSetting("ConnectionStrings:InmobiliariaConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Jwt:Issuer", "integration-tests");
        builder.UseSetting("Jwt:Audience", "integration-tests");
    }

    public async Task InitializeAsync()
    {
        await using var master = new SqlConnection(Master);
        await master.OpenAsync();
        await new SqlCommand($"CREATE DATABASE [{database}]", master).ExecuteNonQueryAsync();
        created = true;
        try
        {
            foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Sql"), "*.sql").Order())
            {
                // Adapt only the scripts' database-name guard to this generated test database.
                var sql = (await File.ReadAllTextAsync(file)).Replace("N'InmobiliariaDB'", $"N'{database}'");
                foreach (var batch in Regex.Split(sql, @"^\s*GO\s*\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    if (!string.IsNullOrWhiteSpace(batch)) await ExecuteSql(batch);
            }
            using var scope = Services.CreateScope();
            Assert.True(await scope.ServiceProvider.GetRequiredService<CuentasService>()
                .CrearPrimerAdministrador(AdminEmail, Password));
        }
        catch { await DropDatabase(); throw; }
    }

    public async Task<int> ExecuteSql(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> Scalar(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task DropDatabase()
    {
        if (!created) return;
        if (!Regex.IsMatch(database, "^Inmobiliaria_Integration_[0-9a-f]{32}$")) throw new InvalidOperationException("Unsafe database name.");
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(Master);
        await connection.OpenAsync();
        await new SqlCommand($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]", connection).ExecuteNonQueryAsync();
        created = false;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await DropDatabase();
        if (Directory.Exists(ImagenesCarpeta))
        {
            foreach (var archivo in Directory.GetFiles(ImagenesCarpeta, "*.jpg")) File.Delete(archivo);
            Directory.Delete(ImagenesCarpeta);
        }
    }
}

[CollectionDefinition("HTTP y SQL", DisableParallelization = true)]
public sealed class SqlCollection : ICollectionFixture<ApiSqlFixture> { }
