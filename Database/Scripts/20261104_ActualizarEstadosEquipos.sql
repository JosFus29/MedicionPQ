/*
  Actualiza el catálogo de estados (columna edo) de MedidorQP y ControladorRF:

      0  = inactivo          (antes era 20)
      10 = activo
      11 = en uso
      20 = en mantenimiento  (nuevo significado)

  Pasos:
  1. Muestra cuántos registros hay por estado ANTES del cambio.
  2. Convierte el inactivo anterior (20) al nuevo inactivo (0). Hasta este script,
     20 solo podía significar "inactivo", así que todos esos registros pasan a 0.
  3. Si queda algún valor fuera de 0, 10, 11 o 20, se detiene sin cambiar nada y lo informa.
  4. Agrega una regla CHECK para que la base solo acepte esos cuatro valores.

  Ejecutar UNA sola vez por base: si se vuelve a correr después de que alguien
  ya puso equipos en mantenimiento (20), los pasaría a inactivo (0).
  Si algo falla, se deshace todo.
*/
USE MedicionQP;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- 1) Estados actuales, antes del cambio
SELECT 'ControladorRF' AS tabla, edo, COUNT(*) AS registros FROM dbo.ControladorRF GROUP BY edo
UNION ALL
SELECT 'MedidorQP', edo, COUNT(*) FROM dbo.MedidorQP GROUP BY edo
ORDER BY tabla, edo;

BEGIN TRANSACTION;

-- 2) El inactivo anterior (20) pasa al nuevo inactivo (0)
UPDATE dbo.ControladorRF SET edo = 0 WHERE edo = 20;
UPDATE dbo.MedidorQP     SET edo = 0 WHERE edo = 20;

-- 3) Ningún registro puede quedar fuera del catálogo
IF EXISTS (SELECT 1 FROM dbo.ControladorRF WHERE edo NOT IN (0, 10, 11, 20))
   OR EXISTS (SELECT 1 FROM dbo.MedidorQP WHERE edo NOT IN (0, 10, 11, 20))
    THROW 51000, 'Hay controladores o medidores con un edo fuera de 0, 10, 11 o 20. Corrígelos y vuelve a ejecutar; no se cambió nada.', 1;

-- 4) Regla en la base: solo 0, 10, 11 o 20
IF OBJECT_ID(N'dbo.CK_ControladorRF_edo', N'C') IS NULL
    ALTER TABLE dbo.ControladorRF
        ADD CONSTRAINT CK_ControladorRF_edo CHECK (edo IN (0, 10, 11, 20));

IF OBJECT_ID(N'dbo.CK_MedidorQP_edo', N'C') IS NULL
    ALTER TABLE dbo.MedidorQP
        ADD CONSTRAINT CK_MedidorQP_edo CHECK (edo IN (0, 10, 11, 20));

COMMIT TRANSACTION;
GO

-- Verificación: estados después del cambio
SELECT 'ControladorRF' AS tabla, edo, COUNT(*) AS registros FROM dbo.ControladorRF GROUP BY edo
UNION ALL
SELECT 'MedidorQP', edo, COUNT(*) FROM dbo.MedidorQP GROUP BY edo
ORDER BY tabla, edo;
GO
