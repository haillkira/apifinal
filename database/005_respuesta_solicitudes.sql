SET XACT_ABORT ON;
IF COL_LENGTH('dbo.Solicitudes','Respuesta') IS NULL
    ALTER TABLE dbo.Solicitudes ADD Respuesta nvarchar(1000) NULL;
