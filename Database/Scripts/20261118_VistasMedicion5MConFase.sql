/*
  Agrega la columna "fase" a las seis vistas de mediciones de 5 minutos:

      vw_Medicion5M                (crudo)
      vw_Medicion5M_PromedioHora   (promedio por hora)
      vw_Medicion5M_PromedioDia    (promedio por día, sobre PromedioHora)
      vw_Medicion5M_PromedioSemana (días etiquetados con año/semana)
      vw_Medicion5M_PromedioMes    (días etiquetados con año/mes)
      vw_Medicion5M_PromedioAnio   (promedio por mes, sobre PromedioDia)

  Motivo: la API filtra todas estas vistas por fase (1 = A, 2 = B, 3 = C), pero las
  vistas originales no tenían esa columna. La consulta fallaba con
  "Invalid column name 'fase'" (error 500 en /api/Medicion5M/crudo, promedio-hora, etc.)
  y, además, los promedios mezclaban las tres fases en un solo valor.

  Cambio: solo se agrega "fase" al SELECT y al GROUP BY. Los cálculos, nombres de
  columnas y la forma en que cada vista se construye sobre la anterior quedan igual
  que en las vistas originales.

  No modifica datos: solo vuelve a definir las vistas. Se puede ejecutar varias veces.
  vw_Medicion5M_EnergiaHora no se toca (ya tiene fase; ver 20261028_VistaEnergiaHora.sql).
*/
USE MedicionQP;
GO

/* NIVEL 0 - CRUDO: 288 lecturas por día y fase (una cada 5 minutos). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,        -- 1 = A, 2 = B, 3 = C
        intervalo,   -- 0 a 287
        valor
    FROM dbo.Medicion5M;
GO

/* NIVEL 1 - PROMEDIO POR HORA: 24 valores por día y fase (promedia las 12 lecturas de cada hora). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioHora
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,
        (intervalo / 12)   AS hora,     -- 0 a 23
        AVG(valor)         AS valorPromedio
    FROM dbo.Medicion5M
    GROUP BY idMedidor, idVar5M, fecha, fase, (intervalo / 12);
GO

/* NIVEL 2 - PROMEDIO POR DÍA: 1 valor por día y fase (promedia las 24 horas de PromedioHora). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioDia
AS
    SELECT
        idMedidor,
        idVar5M,
        fecha,
        fase,
        AVG(valorPromedio) AS valorPromedio
    FROM dbo.vw_Medicion5M_PromedioHora
    GROUP BY idMedidor, idVar5M, fecha, fase;
GO

/* NIVEL 3a - DÍAS DE LA SEMANA: un valor por día, etiquetado con año/semana (no vuelve a promediar). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioSemana
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)           AS anio,
        DATEPART(WEEK, fecha) AS semana,
        fecha,
        valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia;
GO

/* NIVEL 3b - DÍAS DEL MES: un valor por día, etiquetado con año/mes (no vuelve a promediar). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioMes
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)   AS anio,
        MONTH(fecha)  AS mes,
        fecha,
        valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia;
GO

/* NIVEL 4 - PROMEDIO MENSUAL DEL AÑO: un valor por mes y fase (promedia los días de PromedioDia). */
CREATE OR ALTER VIEW dbo.vw_Medicion5M_PromedioAnio
AS
    SELECT
        idMedidor,
        idVar5M,
        fase,
        YEAR(fecha)         AS anio,
        MONTH(fecha)        AS mes,
        AVG(valorPromedio)  AS valorPromedio
    FROM dbo.vw_Medicion5M_PromedioDia
    GROUP BY idMedidor, idVar5M, fase, YEAR(fecha), MONTH(fecha);
GO

/* Verificación: las siete vistas vw_Medicion5M* deben mostrar "ok". */
SELECT v.name AS vista,
       CASE WHEN c.name IS NULL THEN 'FALTA fase' ELSE 'ok' END AS estado
FROM sys.views AS v
LEFT JOIN sys.columns AS c ON c.object_id = v.object_id AND c.name = 'fase'
WHERE v.name LIKE 'vw[_]Medicion5M%'
ORDER BY v.name;
GO