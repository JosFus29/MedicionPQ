/*
  Permite los roles Usuario (2), Administrador (1) y SuperAdministrador (5)
  en dbo.Usuario. Se puede volver a ejecutar de forma segura.
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
        THROW 51000, 'No existe la tabla dbo.Usuario en la base de datos actual.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WHERE rol IS NULL OR rol NOT IN (1, 2, 5))
        THROW 51001, 'Hay valores de rol distintos de 1, 2 o 5. Corrígelos antes de aplicar el constraint.', 1;

    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.Usuario')
          AND name = N'CK_Usuario_Rol'
    )
        ALTER TABLE dbo.Usuario DROP CONSTRAINT CK_Usuario_Rol;

    ALTER TABLE dbo.Usuario WITH CHECK
        ADD CONSTRAINT CK_Usuario_Rol CHECK (rol IN (1, 2, 5));

    ALTER TABLE dbo.Usuario CHECK CONSTRAINT CK_Usuario_Rol;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
