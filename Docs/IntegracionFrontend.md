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
- `503 Service Unavailable`: la DLL no consiguió establecer comunicación con el medidor.

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

El punto se configura en el servidor, en `Sincronizacion:Punto`, con `Tag` y `DirComsCEC`. Esta DLL usa `DirComsCEC` para el protocolo SimIV. El valor debe ser la dirección real confirmada para el medidor; `null` deja disponible `/conectar`, pero `/sincronizar` responde `503` hasta configurarlo. No se debe usar una dirección de prueba.
