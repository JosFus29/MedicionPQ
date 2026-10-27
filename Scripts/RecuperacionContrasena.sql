USE [MedicionQP]
GO

/* =============================================
   NUEVA TABLA: RecuperacionContrasena
   Guarda los códigos de 6 dígitos que se envían por correo
   cuando un usuario olvidó su contraseña.
   - El código NO se guarda en texto plano: solo su hash.
   - Cada código vence (columna expira) y solo se puede usar una vez (columna usado).
   - intentos cuenta los códigos incorrectos; al llegar a 5 el código se invalida.
   Se puede ejecutar más de una vez: si la tabla ya existe, no hace nada.
   ============================================= */
IF OBJECT_ID(N'[dbo].[RecuperacionContrasena]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RecuperacionContrasena]
    (
        [idRecuperacion] INT IDENTITY(1,1) NOT NULL,
        [idUsuario]      INT            NOT NULL,
        [codigoHash]     NVARCHAR(200)  NOT NULL,
        [expira]         DATETIME2      NOT NULL,
        [intentos]       INT            NOT NULL CONSTRAINT [DF_RecuperacionContrasena_intentos] DEFAULT (0),
        [usado]          BIT            NOT NULL CONSTRAINT [DF_RecuperacionContrasena_usado] DEFAULT (0),
        [fechaCreacion]  DATETIME2      NOT NULL CONSTRAINT [DF_RecuperacionContrasena_fechaCreacion] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_RecuperacionContrasena] PRIMARY KEY CLUSTERED ([idRecuperacion] ASC),
        CONSTRAINT [FK_RecuperacionContrasena_Usuario] FOREIGN KEY ([idUsuario])
            REFERENCES [dbo].[Usuario] ([idUsuario])
    );

    -- Acelera la búsqueda del código vigente de un usuario.
    CREATE INDEX [IX_RecuperacionContrasena_idUsuario]
        ON [dbo].[RecuperacionContrasena] ([idUsuario]);
END
GO

-- Verificación: debe mostrar las 7 columnas de la tabla.
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'RecuperacionContrasena'
ORDER BY ORDINAL_POSITION;
GO
