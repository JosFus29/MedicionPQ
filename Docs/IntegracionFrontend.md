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

La operación de sincronización todavía no forma parte de este contrato.
