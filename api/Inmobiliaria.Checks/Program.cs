using Microsoft.EntityFrameworkCore;
using Inmobiliaria.Application;
if(!ReglasAlquiler.TipoPermitido("Apartamento") || !ReglasAlquiler.TipoPermitido("Casa") || ReglasAlquiler.TipoPermitido("Terreno"))throw new Exception("Tipos incorrectos.");
ReglasAlquiler.ValidarOferta("Alquiler","DOP",15000,15000,null);
var casos=new[]{("Venta","DOP",15000m),("Alquiler","EUR",15000m),("Alquiler","USD",-1m)};
foreach(var (operacion,moneda,precio) in casos){bool rejected=false;try{ReglasAlquiler.ValidarOferta(operacion,moneda,precio,null,null);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Caso invalido aceptado.");}
Console.WriteLine("OK: tipos permitidos, alquiler valido y rechazo de venta/moneda/importe.");
var options=new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Inmobiliaria.Data.InmobiliariaDbContext>();
Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions.UseSqlServer(options,"Server=localhost;Database=NoConnect;Integrated Security=True;TrustServerCertificate=True");
using var db=new Inmobiliaria.Data.InmobiliariaDbContext(options.Options);
if(db.Model.GetEntityTypes().Count()!=26)throw new Exception("Se esperaban 26 entidades.");
var ddl=db.Database.GenerateCreateScript();
if(!ddl.Contains("CREATE TABLE"))throw new Exception("Modelo SQL no generado.");
Console.WriteLine("OK: modelo EF de 26 entidades validado y DDL generado sin conexion SQL.");
