/*
  Asegura que las seis vistas de mediciones de 5 minutos tengan la columna "fase":

      vw_Medicion5M                (crudo)
      vw_Medicion5M_PromedioHora   (promedio por hora)
      vw_Medicion5M_PromedioDia    (promedio por día, sobre PromedioHora)
      vw_Medicion5M_PromedioSemana (días etiquetados con año/semana)
      vw_Medicion5M_PromedioMes    (días etiquetados con año/mes)
      vw_Medicion5M_PromedioAnio   (promedio por mes, sobre PromedioDia)

  Motivo: la API filtra todas estas vistas por fase (1 = A, 2 = B, 3 = C). En bases con
  vistas antiguas sin esa columna, la consulta falla con "Invalid column name 'fase'"
  (error 500 en /api/Medicion5M/crudo, promedio-hora, etc.) y los promedios mezclan las
  tres fases en un solo valor.

  ES SEGURO EN CUALQUIER BASE:
  - Una vista que YA tiene la columna "fase" NO se toca (se conserva tal como está).
  - Solo se crea o redefine una vista si no existe o si le falta "fase". En ese caso se usa
    la definición original agregando "fase" al SELECT y al GROUP BY, sin cambiar los cálculos.
  - No modifica datos. Se puede ejecutar varias veces. Al final muestra qué hizo con cada vista.

  vw_Medicion5M_EnergiaHora no se toca (ver 20261028_VistaEnergiaHora.sql).
*/
USE MedicionQP;
GO
SET NOCOUNT ON;
GO

/* Vistas que se crearon o actualizaron en esta ejecución (para el reporte final). */
IF OBJECT_ID('tempdb..#VistasActualizadas') IS NOT NULL DROP TABLE #VistasActualizadas;
CREATE TABLE #VistasActualizadas (vista sysname NOT NULL);
GO

/* NIVEL 0 - CRUDO: 288 lecturas por día y fase (una cada 5 minutos). */
IF COL_LENGTH('dbo.vw_Medicion5M', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,        -- 1 = A, 2 = B, 3 = C
        intervalo,   -- 0 a 287
        valor
    FROM dbo.Medicion5M;');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M');
END
GO

/* NIVEL 1 - PROMEDIO POR HORA: 24 valores por día y fase (promedia las 12 lecturas de cada hora). */
IF COL_LENGTH('dbo.vw_Medicion5M_PromedioHora', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioHora
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,
        (intervalo / 12)   AS hora,     -- 0 a 23
        AVG(valor)         AS valorPromedio
    FROM dbo.Medicion5M
    GROUP BY idMedidor, idVar5M, fecha, fase, (intervalo / 12);');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M_PromedioHora');
END
GO

/* NIVEL 2 - PROMEDIO POR DÍA: 1 valor por día y fase (promedia las 24 horas de PromedioHora). */
IF COL_LENGTH('dbo.vw_Medicion5M_PromedioDia', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioDia
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,
        AVG(valorPromedio) AS valorPromedio
    FROM dbo.vw_Medicion5M_PromedioHora
    GROUP BY idMedidor, idVar5M, fecha, fase;');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M_PromedioDia');
END
GO

/* NIVEL 3a - DÍAS DE LA SEMANA: un valor por día, etiquetado con año/semana (no vuelve a promediar). */
IF COL_LENGTH('dbo.vw_Medicion5M_PromedioSemana', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioSemana
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)           AS anio,
        DATEPART(WEEK, fecha) AS semana,
        fecha,
        valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia;');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M_PromedioSemana');
END
GO

/* NIVEL 3b - DÍAS DEL MES: un valor por día, etiquetado con año/mes (no vuelve a promediar). */
IF COL_LENGTH('dbo.vw_Medicion5M_PromedioMes', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioMes
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)   AS anio,
        MONTH(fecha)  AS mes,
        fecha,
        valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia;');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M_PromedioMes');
END
GO

/* NIVEL 4 - PROMEDIO MENSUAL DEL AÑO: un valor por mes y fase (promedia los días de PromedioDia). */
IF COL_LENGTH('dbo.vw_Medicion5M_PromedioAnio', 'fase') IS NULL
BEGIN
    EXEC (N'CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioAnio
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)         AS anio,
        MONTH(fecha)        AS mes,
        AVG(valorPromedio)  AS valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia
    GROUP BY idMedidor, idVar5M, fase, YEAR(fecha), MONTH(fecha);');
    INSERT INTO #VistasActualizadas VALUES ('vw_Medicion5M_PromedioAnio');
END
GO

/* Reporte: qué se hizo con cada vista y si ya tiene la columna fase. */
SELECT v.vista,
       CASE WHEN a.vista IS NOT NULL THEN 'creada o actualizada' ELSE 'sin cambios (ya tenía fase)' END AS accion,
       CASE WHEN COL_LENGTH('dbo.' + v.vista, 'fase') IS NULL THEN 'FALTA fase' ELSE 'ok' END AS estado
FROM (VALUES ('vw_Medicion5M'), ('vw_Medicion5M_PromedioHora'), ('vw_Medicion5M_PromedioDia'),
             ('vw_Medicion5M_PromedioSemana'), ('vw_Medicion5M_PromedioMes'), ('vw_Medicion5M_PromedioAnio')) AS v(vista)
LEFT JOIN #VistasActualizadas AS a ON a.vista = v.vista
ORDER BY v.vista;

DROP TABLE #VistasActualizadas;
GO
