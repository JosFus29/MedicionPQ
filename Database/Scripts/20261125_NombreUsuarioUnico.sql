/*
  Hace que el nombre de usuario (dbo.Usuario.nombreUsuario) sea único, porque ahora también sirve para iniciar sesión.

  Qué hace, en una sola transacción:
  1. Si la columna es NVARCHAR(MAX), la cambia a NVARCHAR(50) (no se puede indexar una columna MAX).
  2. Limpia los nombres: quita espacios al inicio y al final y deja un solo espacio entre palabras
     (igual que hace la API al guardar).
  3. Revisa que no haya nombres repetidos sin importar mayúsculas ("Ana López" y "ana lópez" son el mismo).
     Si hay, MUESTRA la lista y se detiene sin cambiar nada: corrígelos (en la pantalla de Usuarios o con el
     UPDATE de ejemplo del final) y vuelve a correr el script.
  4. Crea el índice único UX_Usuario_nombreUsuario.

  Se puede ejecutar varias veces: si el índice ya existe, no lo vuelve a crear.
  No toca contraseñas, correos ni ninguna otra columna.
*/
USE MedicionQP;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
        THROW 51010, 'No existe la tabla dbo.Usuario en la base de datos actual.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE nombreUsuario IS NULL OR LTRIM(RTRIM(nombreUsuario)) = N'')
        THROW 51011, 'Hay cuentas sin nombre de usuario. Asígnales uno antes de correr este script.', 1;

    /* 1. Una columna NVARCHAR(MAX) no se puede indexar: se pasa a NVARCHAR(50), el máximo que acepta la API. */
    IF EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.Usuario') AND name = N'nombreUsuario' AND max_length = -1)
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE LEN(nombreUsuario) > 50)
            THROW 51012, 'Hay nombres de usuario de más de 50 caracteres. Acórtalos antes de correr este script.', 1;
        ALTER TABLE dbo.Usuario ALTER COLUMN nombreUsuario NVARCHAR(50) NOT NULL;
    END;

    /* 2. Limpiar espacios: sin espacios al inicio o al final y uno solo entre palabras. */
    UPDATE dbo.Usuario
    SET nombreUsuario = LTRIM(RTRIM(nombreUsuario))
    WHERE nombreUsuario <> LTRIM(RTRIM(nombreUsuario)) OR DATALENGTH(nombreUsuario) <> DATALENGTH(LTRIM(RTRIM(nombreUsuario)));

    WHILE EXISTS (SELECT 1 FROM dbo.Usuario WHERE CHARINDEX(N'  ', nombreUsuario) > 0)
        UPDATE dbo.Usuario
        SET nombreUsuario = REPLACE(nombreUsuario, N'  ', N' ')
        WHERE CHARINDEX(N'  ', nombreUsuario) > 0;

    /* 3. Nombres repetidos (sin importar mayúsculas): se muestran y el script se detiene. */
    IF EXISTS (SELECT 1 FROM dbo.Usuario GROUP BY LOWER(nombreUsuario) HAVING COUNT(*) > 1)
    BEGIN
        SELECT u.idUsuario, u.nombreUsuario, u.correo, u.rol, u.edo
        FROM dbo.Usuario AS u
        WHERE LOWER(u.nombreUsuario) IN (SELECT LOWER(nombreUsuario) FROM dbo.Usuario GROUP BY LOWER(nombreUsuario) HAVING COUNT(*) > 1)
        ORDER BY LOWER(u.nombreUsuario), u.idUsuario;

        THROW 51013, 'Hay nombres de usuario repetidos (la lista está en Resultados). Cambia los repetidos y vuelve a correr el script. No se modificó nada.', 1;
    END;

    /* 4. Índice único: la base también impide repetidos, aunque alguien inserte directo en SQL. */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Usuario') AND name = N'UX_Usuario_nombreUsuario')
        CREATE UNIQUE INDEX UX_Usuario_nombreUsuario ON dbo.Usuario (nombreUsuario);

    COMMIT TRANSACTION;
    PRINT 'Listo: el nombre de usuario ya es único.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Verificación: debe aparecer el índice UX_Usuario_nombreUsuario con is_unique = 1. */
SELECT name, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Usuario') AND name = N'UX_Usuario_nombreUsuario';
GO

/*
  Ejemplo para corregir un repetido antes de volver a correr el script
  (cambia el id y el nombre por los de la lista que mostró el paso 3):

  UPDATE dbo.Usuario SET nombreUsuario = N'Arturo Avalos 2' WHERE idUsuario = 4003;
*/
