/*
  Crea (o actualiza) la vista dbo.vw_Medicion5M_EnergiaHora.

  Total de energía por hora de las variables P5M de energía (las que empiezan con "Energia"
  en Vars5M: P5M_EE, P5M_ER, P5M_ERI, P5M_ERII, P5M_ERIII y P5M_ERIV) y su acumulado del día.

  - totalHora:    suma de los valores de 5 minutos de esa hora.
  - acumuladoDia: suma de totalHora desde la hora 0 hasta esa hora, del mismo día.
  - intervalos:   cuántos registros de 5 minutos tuvo la hora (12 = hora completa).

  - "intervalo" va de 0 a 287 (00:00 a 23:55, como indica el README), así que hora = intervalo / 12.
  - Supuesto a confirmar con datos reales: "valor" es la energía de ese intervalo de 5 minutos,
    no la lectura acumulada del contador del medidor (en ese caso habría que restar en lugar de sumar).

  La vista calcula al consultar, así que siempre está al día con Medicion5M.
  Se puede volver a ejecutar de forma segura.
*/
CREATE OR ALTER VIEW dbo.vw_Medicion5M_EnergiaHora
AS
WITH PorHora AS
(
    SELECT
        m.idMedidor,
        m.idVar5M,
        v.simbolo,
        m.fecha,
        m.fase,
        CAST(m.intervalo / 12 AS int) AS hora,
        SUM(m.valor)                  AS totalHora,
        COUNT(*)                      AS intervalos
    FROM dbo.Medicion5M AS m
    INNER JOIN dbo.Vars5M AS v ON v.idVar5M = m.idVar5M
    WHERE v.simbolo LIKE 'P5M[_]E%'   -- [_] = guion bajo literal; deja fuera P5M_F, P5M_FP, etc.
    GROUP BY m.idMedidor, m.idVar5M, v.simbolo, m.fecha, m.fase, m.intervalo / 12
)
SELECT
    idMedidor,
    idVar5M,
    simbolo,
    fecha,
    fase,
    hora,
    totalHora,
    intervalos,
    SUM(totalHora) OVER (
        PARTITION BY idMedidor, idVar5M, fecha, fase
        ORDER BY hora
        ROWS UNBOUNDED PRECEDING
    ) AS acumuladoDia
FROM PorHora;
GO

-- Verificación: las primeras horas de energía entregada (P5M_EE) en fase A.
SELECT TOP (24) *
FROM dbo.vw_Medicion5M_EnergiaHora
WHERE simbolo = 'P5M_EE' AND fase = 1
ORDER BY fecha DESC, hora;
GO
