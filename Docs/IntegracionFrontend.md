# Integración del frontend con medidores

Esta API permite que el frontend inicie sesión, consulte los medidores registrados y solicite conexión por el identificador del medidor. El frontend no envía `dirRed`, `dirRF` ni configuración serial; la API obtiene esos datos de la base de datos y de los JSON del servidor.

## Direcciones locales de la API

En el perfil de desarrollo actual:

- HTTPS: `https://localhost:7090`
- HTTP: `http://localhost:5201` (la API redirige a HTTPS)

Usa HTTPS desde el navegador y confía el certificado local de desarrollo de .NET si el navegador lo solicita.

## Autenticación

1. Llama `POST /api/Auth/login` con correo y contraseña:

   ```json
   {
     "correo": "admin@ejemplo.com",
     "contrasena": "contraseña"
   }
   ```

2. Guarda el campo `token` de la respuesta y envíalo como `Authorization: Bearer <token>` en las rutas protegidas.
3. Tanto la consulta como la conexión requieren rol `Administrador` o `SuperAdministrador`.

## Mostrar medidores para seleccionar

`GET /api/MedidoresQP`

Encabezado requerido:

```text
Authorization: Bearer <token>
```

Respuesta `200 OK`:

```json
[
  {
    "idMedidor": 1,
    "descripcion": "Medidor principal",
    "edo": 10,
    "idCtrlRF": 2
  }
]
```

El frontend debe conservar `idMedidor` como valor seleccionado. Estados actuales: `0` inactivo, `10` activo, `11` en uso y `20` en mantenimiento. La API devuelve el catálogo completo; el frontend puede mostrar el estado y deshabilitar los que no deba ofrecer.

## Solicitar conexión

`POST /api/ConexionMedidor/conectar`

Encabezados requeridos:

```text
Authorization: Bearer <token>
Content-Type: application/json
```

Cuerpo:

```json
{
  "idMedidor": 1
}
```

Respuestas principales:

- `200 OK`: `{"conectado":true,"mensaje":"Conexión establecida."}`
- `400 Bad Request`: solicitud o datos de dirección inválidos.
- `401 Unauthorized`: falta el token o expiró.
- `403 Forbidden`: el usuario no tiene el rol requerido.
- `404 Not Found`: no existe ese `idMedidor`.
- `503 Service Unavailable`: la DLL no consiguió establecer comunicación con el medidor, lanzó una excepción al conectar, el `dirRed` del medidor no cabe en el `ushort` de la DLL, o el puerto estuvo ocupado por otra solicitud más de 30 s (en ese caso conviene reintentar).

Ejemplo JavaScript:

```js
const respuesta = await fetch(`${API_URL}/api/ConexionMedidor/conectar`, {
  method: "POST",
  headers: {
    "Authorization": `Bearer ${token}`,
    "Content-Type": "application/json"
  },
  body: JSON.stringify({ idMedidor: medidorSeleccionado.idMedidor })
});

const resultado = await respuesta.json();
```

## CORS

La política `Frontend` ya lee la lista permitida desde `Frontend:AllowedOrigins`. Agrega el origen exacto donde se ejecuta el frontend (esquema, host y puerto), por ejemplo en configuración local:

```json
"Frontend": {
  "AllowedOrigins": [
    "http://localhost:5173"
  ]
}
```

Para producción, configura el dominio publicado del frontend mediante configuración del entorno. También puede usarse la variable de entorno `Frontend__AllowedOrigins__0`. No uses `*` para el despliegue; configura únicamente los orígenes que alojan el frontend.

## Comunicación con el medidor

El servidor donde corre la API debe tener conectado el dispositivo RF en el COM configurado en `Jsons/TransporteConfig.json`. `Jsons/ProtocoloConfig.json` controla `UsarRF`. La dirección RF del medidor se obtiene de la base de datos mediante `idMedidor`.

## Conectar y sincronizar son operaciones distintas

`POST /api/ConexionMedidor/conectar` solo solicita conexión. No sincroniza el medidor.

`POST /api/ConexionMedidor/sincronizar` ejecuta una secuencia independiente: la API llama primero a `Conectar` y, si la DLL confirma la conexión, llama a `Sincronizar` con el punto configurado en el servidor. Ambas llamadas se serializan juntas para que otra solicitud no interfiera con el puerto.

El frontend no necesita llamar antes a `/conectar` para usar `/sincronizar`: esta ruta conecta como primer paso de su propia operación. Usa `/conectar` cuando solo se quiera comprobar o establecer la conexión, y `/sincronizar` cuando se necesiten las dos etapas.

La petición del frontend es la misma y no incluye la dirección del punto:

```json
{
  "idMedidor": 1
}
```

Respuesta cuando ambas etapas tienen éxito (`200 OK`):

```json
{
  "conectado": true,
  "sincronizado": true,
  "mensaje": "Conexión y sincronización establecidas."
}
```

Si conecta pero no sincroniza, responde `503 Service Unavailable` con `conectado: true` y `sincronizado: false`. Si no conecta, no intenta sincronizar y devuelve ambos indicadores en `false`.

El servidor usa `Sincronizacion:Punto:IdPunto` (actualmente `114`) para localizar el registro de la tabla `PuntosIO`. La API toma el `Tag` y `DirCCEC` de ese registro; copia `DirCCEC` al campo `PuntoIODto.DirComsCEC` que exige la DLL para SimIV. El `IdPunto` solo selecciona el registro en la base de datos: no se envía a la DLL. `/sincronizar` responde `503` si no existe el `IdPunto` configurado o si `DirCCEC` no cabe en el `ushort` de la DLL.

## Solicitar lecturas por grupo funcional

Todas estas rutas funcionan igual: el frontend solo manda `idMedidor`, la API carga de `PuntosIO` los registros activos (`Edo = 1`) del grupo funcional (`IdGF`, tabla `IO_GrupoFuncional`), conecta con el medidor y llama al método de la DLL que corresponde al grupo.

| Ruta (`POST`) | Grupo | `IdGF` | Método de la DLL |
|---|---|---|---|
| `/api/ConexionMedidor/instantaneos` | Instantáneos | `1` | `SolInstT` |
| `/api/ConexionMedidor/variables5min` | Variables 5 minutales (UV5M) | `2` | `SolUV5M` |
| `/api/ConexionMedidor/consumos` | Consumos | `3` | `SolConsumos` |
| `/api/ConexionMedidor/calidadpotencia` | Calidad de la potencia | `5` | `SolCalPot` |

```json
{
  "idMedidor": 1
}
```

Para cada punto, la API copia `Tag`, `DirCCEC` a `PuntoIODto.DirComsCEC` y `DirMB` a `PuntoIODto.DirModbus`. La DLL escribe el valor recibido en `PuntoIODto.Valor`; la respuesta incluye cada punto con ese valor.

La respuesta contiene `idMedidor`, `idGF`, `conectado`, `lecturaExitosa`, `mensaje` y `puntos`. Cada elemento de `puntos` contiene `idPunto`, `tag`, `dirCCEC`, `dirModbus` y `valor`. Los puntos y sus direcciones son los que estén guardados en la base de datos para ese `IdGF` con `Edo = 1`; los puntos inactivos no se piden ni aparecen en la respuesta.

Ejemplo de llamada desde el frontend. Si la API devuelve `503`, el cuerpo sigue teniendo información útil, por ejemplo si alcanzó a conectar o qué mensaje mostrar:

```js
// ruta: "instantaneos", "variables5min", "consumos" o "calidadpotencia"
async function solicitarGrupo(apiUrl, token, ruta, idMedidor) {
  const respuesta = await fetch(`${apiUrl}/api/ConexionMedidor/${ruta}`, {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ idMedidor })
  });

  const resultado = await respuesta.json();

  if (!respuesta.ok) {
    throw new Error(resultado.mensaje ?? `Error HTTP ${respuesta.status}`);
  }

  return resultado; // resultado.puntos contiene los valores leídos
}
```

La operación completa (conexión y solicitud) se mantiene serializada para evitar que otra petición use el puerto COM al mismo tiempo. Responde `503` si no se pudo conectar, la DLL no confirmó la lectura, no hay puntos activos con ese `IdGF`, el puerto estuvo ocupado por otra solicitud más de 30 s o alguna dirección no cabe en el `ushort` que requiere la DLL.

## Solicitar armónicos

`POST /api/ConexionMedidor/armonicos` lee los armónicos en una sola conexión. Recibe el mismo cuerpo que las lecturas por grupo:

```json
{
  "idMedidor": 1
}
```

La API carga de `PuntosIO` los registros activos (`Edo = 1`) con `IdGF = 6` y envía **cada uno** a la DLL como `PuntoIODto` (`Tag`, `DirCCEC` → `DirComsCEC`, `DirMB` → `DirModbus`), junto con la fase, que es el `IdGE` del punto (`1` = A, `2` = B, `3` = C). Por cada punto llama a `SolArmonicos(medidor, fase, punto)`, que devuelve un arreglo de valores (la DLL pide 30 por punto). Puede haber varios puntos por fase: por ejemplo `ARM_VA` (voltaje) y `ARM_IA` (corriente) con `IdGE = 1`. Los puntos inactivos no se piden ni aparecen en la respuesta.

La DLL decide qué solicitar a partir del `Tag`, que debe ser `ARM_VA`, `ARM_VB`, `ARM_VC`, `ARM_IA`, `ARM_IB` o `ARM_IC`; con otro `Tag` la DLL no lee nada y el punto aparece con `exitosa: false`.

```json
{
  "idMedidor": 1,
  "idGF": 6,
  "conectado": true,
  "lecturaExitosa": true,
  "mensaje": "Se solicitaron los armónicos de 6 punto(s).",
  "puntos": [
    {
      "fase": 1,
      "nombreFase": "A",
      "tipo": "V",
      "idPunto": 91,
      "tag": "ARM_VA",
      "dirCCEC": 0,
      "dirModbus": 40127,
      "exitosa": true,
      "valores": [100.0, 2.3, 0.8]
    }
  ]
}
```

`puntos` viene ordenado por fase (A, B, C) y, dentro de cada fase, voltaje (`tipo: "V"`) antes que corriente (`tipo: "I"`). `valores` conserva el orden en que la DLL entrega los armónicos. Si un punto falla, la lectura de los demás continúa; ese punto aparece con `exitosa: false` y `valores: []`, y la respuesta es `503` con `lecturaExitosa: false` y un `mensaje` con los `Tag` que fallaron. También responde `503` si no se pudo conectar, no hay puntos activos con `IdGF = 6`, algún punto tiene un `IdGE` distinto de 1, 2 o 3, el puerto estuvo ocupado más de 30 s o alguna dirección no cabe en el `ushort` de la DLL. Si el medidor no existe responde `404`.

## Solicitar fasores

`POST /api/ConexionMedidor/fasores` lee en vivo los instantáneos y devuelve, por fase, el voltaje, la corriente, el factor de potencia y el fasor de corriente, listos para mostrar y dibujar el diagrama fasorial. Recibe el mismo cuerpo que las demás lecturas:

```json
{
  "idMedidor": 1
}
```

La API carga de `PuntosIO` **solo** los puntos activos (`Edo = 1`) con `IdGF = 1` y estos 15 `Tag`, y los pide a la DLL con `SolInstT` en una sola conexión:

| Fase | Potencia activa | Potencia reactiva | Factor de potencia | Voltaje | Corriente | Voltaje de referencia |
|---|---|---|---|---|---|---|
| A | `PA` | `PRA` | `FPA` | `VA` | `IA` | 0° |
| B | `PB` | `PRB` | `FPB` | `VB` | `IB` | 240° |
| C | `PC` | `PRC` | `FPC` | `VC` | `IC` | 120° |

Si falta alguno de los 15 puntos, está inactivo o está repetido, la API no llama a la DLL y responde `503` con un `mensaje` que dice cuáles.

### Cálculo (por fase)

El fasor se calcula **solo** con P, PR y FP. El voltaje y la corriente no intervienen en el cálculo: se devuelven para mostrarlos y para dar el largo de las flechas.

1. La DLL entrega el FP multiplicado por 1000 (`-809` = `-0.809`); la API lo divide entre 1000 y lo limita a `-1..1`. V e I ya llegan en volts y amperes y se mandan sin cambios.
2. `anguloFactorPotencia` (phi) = arccos(FP), en grados.
3. Cuadrante según los signos de potencia activa (P) y reactiva (PR), y `anguloDesfase` (theta_lag):

   | Cuadrante | Signos | Tipo | theta_lag |
   |---|---|---|---|
   | I | P > 0, PR > 0 | Consumo inductivo | phi |
   | II | P < 0, PR > 0 | Generación capacitiva | phi |
   | III | P < 0, PR < 0 | Generación inductiva | 360 − phi |
   | IV | P > 0, PR < 0 | Consumo capacitivo | 360 − phi |

4. `anguloCorriente` = (ángulo del voltaje − theta_lag) mod 360, de 0 a menos de 360. `anguloCorriente180` es el mismo ángulo de −180 a 180.
5. `potenciaAparente` (S) = raíz(P² + PR²), en las mismas unidades que las potencias.

Casos especiales:

- **Sin carga** (P y PR valen 0): `tipo: "Sin carga"`, ángulos de corriente y phi en `null` (no hay corriente que dibujar). `voltaje` y `corriente` sí traen su valor, para dibujar al menos la flecha de voltaje con `anguloVoltaje`.
- **Valor que no llegó**: ese campo va en `null` en su fase (mostrar "—"). Si el que falta es P, PR o FP, la fase sale con `tipo: "Sin lectura"` y los campos calculados en `null`; si falta V o I, solo ese campo queda en `null` y los ángulos se calculan normalmente.
- Si solo P o solo PR vale 0, se toma como positivo (por ejemplo, PR = 0 con P > 0 es cuadrante I).
- `anguloDesfase` puede valer `360` (FP = 1 en cuadrantes III o IV); en el diagrama equivale a 0°.

### Respuesta

```json
{
  "idMedidor": 1,
  "idGF": 1,
  "conectado": true,
  "lecturaExitosa": true,
  "mensaje": "Se calcularon los fasores de las 3 fases.",
  "fases": [
    {
      "fase": 1,
      "nombreFase": "A",
      "voltaje": 120.5,
      "corriente": 4.23,
      "factorPotencia": -0.809,
      "cuadrante": 3,
      "tipo": "Generación inductiva",
      "anguloVoltaje": 0,
      "anguloFactorPotencia": 144.0,
      "anguloDesfase": 216.0,
      "anguloCorriente": 144.0,
      "anguloCorriente180": 144.0,
      "potenciaAparente": 239.53
    }
  ]
}
```

`fases` siempre trae A, B y C en ese orden. Los ángulos no se redondean; el frontend decide cuántos decimales mostrar. En el diagrama, el ángulo se mide desde el eje horizontal positivo y crece en sentido antihorario: la flecha de voltaje va en `anguloVoltaje` con largo `voltaje`, y la de corriente en `anguloCorriente` con largo `corriente`.

Responde `200` si conectó y llegaron los 15 valores, `404` si el medidor no existe y `503` en cualquier otro caso (no conectó, la DLL no confirmó la lectura, algún valor no llegó, faltan puntos en `PuntosIO` o el puerto estuvo ocupado más de 30 s). En un `503` el cuerpo trae el mismo formato, con `lecturaExitosa: false`, el `mensaje` a mostrar y las tres `fases` con `null` en lo que no llegó (si no conectó, todos los valores vienen en `null`). Solo si el `503` ocurre antes de intentar la lectura (faltan puntos en `PuntosIO`, direcciones inválidas o el puerto estaba ocupado) `fases` viene vacío.

### Actualizar cada cierto tiempo

La API devuelve una lectura por llamada; para que el diagrama se actualice solo, el frontend repite la llamada con el intervalo que elija el usuario. Hay que pedir la siguiente lectura **cuando termine la anterior** más el intervalo (con `setTimeout`, no con `setInterval`): el puerto COM se usa de uno en uno, y si las peticiones se encimaran quedarían en cola hasta fallar con `503`. Se recomienda un intervalo mínimo de 2 segundos.

```js
// Inicia la lectura periódica de fasores. Devuelve una función para detenerla.
function iniciarFasores(apiUrl, token, idMedidor, intervaloMs, alRecibir, alFallar) {
  let activo = true;
  let temporizador = null;
  const intervalo = Math.max(intervaloMs, 2000);

  async function leer() {
    try {
      const respuesta = await fetch(`${apiUrl}/api/ConexionMedidor/fasores`, {
        method: "POST",
        headers: {
          "Authorization": `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ idMedidor })
      });
      const resultado = await respuesta.json();

      if (respuesta.ok) {
        alRecibir(resultado); // resultado.fases -> V, I, FP y phi por fase; dibujar VA..VC e IA..IC
      } else {
        // En un 503, resultado.fases trae lo que sí llegó y null en lo demás (mostrar "—").
        alFallar(resultado.mensaje ?? `Error HTTP ${respuesta.status}`, resultado);
      }
    } catch (error) {
      alFallar(error.message);
    } finally {
      // Siempre se programa la siguiente lectura al terminar la actual, aunque haya fallado.
      if (activo) temporizador = setTimeout(leer, intervalo);
    }
  }

  leer();

  return () => {
    activo = false;
    clearTimeout(temporizador);
  };
}

// Uso: const detener = iniciarFasores(apiUrl, token, 1, 5000, dibujar, mostrarAviso);
// Al salir de la pantalla o cambiar de medidor: detener();
```

Si `respuesta.status` es `401`, conviene detener la lectura y pedir que el usuario vuelva a iniciar sesión en lugar de seguir reintentando. Si varios usuarios tienen los fasores abiertos a la vez, las lecturas se turnan el puerto y cada una tarda más.
