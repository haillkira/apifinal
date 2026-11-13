#if DATABASE_FIRST_GENERATED
global using ContextoDatos = Inmobiliaria.Data.DatabaseFirst.InmobiliariaDbFirstContext;
#else
global using ContextoDatos = Inmobiliaria.Data.InmobiliariaDbContext;
#endif
