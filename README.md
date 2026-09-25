# Examen Parcial 2026-1 — Plataforma de Créditos (WebSocket + Cloud MQ)

Plataforma web interna para registrar y evaluar solicitudes de crédito.

| Elemento | Tecnología |
| --- | --- |
| Framework | ASP.NET Core MVC (.NET 10) + Identity |
| Persistencia | EF Core + SQLite (`app.db`) |
| Sesión / Cache | Redis (`Microsoft.Extensions.Caching.StackExchangeRedis`) |
| Tiempo real | SignalR Hub con transporte **WebSocket** (`/hubs/solicitudes`) |
| Cola | RabbitMQ gestionado en CloudAMQP (cola durable `solicitudes.notificaciones`) |
| Infra | Render.com (Web Service) + Redis Cloud |
| Control de versiones | GitHub, una rama y un PR por pregunta |

Repositorio: `https://github.com/diegofer039-cloud/examen-parcial-2026-1-credito`

---

## 1. Usuarios y roles de demostración

| Rol | Email | Contraseña |
| --- | --- | --- |
| Analista | `analista@demo.com` | `Analista123!` |
| Cliente 1 | `cliente1@demo.com` | `Cliente123!` |
| Cliente 2 | `cliente2@demo.com` | `Cliente123!` |

Datos iniciales (seed automático al arrancar): 2 clientes, 2 solicitudes (una **Pendiente** y una **Aprobada**) y el rol `Analista`.

---

## 2. Ejecución local

```bash
git clone https://github.com/diegofer039-cloud/examen-parcial-2026-1-credito.git
cd examen-parcial-2026-1-credito
dotnet restore
dotnet build
dotnet run
```

- Las migraciones EF Core y el seed se ejecutan automáticamente al arrancar (`SeedData.InitializeAsync`).
- Para aplicarlas manualmente:

```bash
dotnet ef database update
```

- La aplicación queda en la URL que muestra la consola (por defecto `https://localhost:7xxx` o la indicada en `Properties/launchSettings.json`).

---

## 3. Variables de entorno

| Variable | Ejemplo local | Ejemplo Render |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Production` |
| `ASPNETCORE_URLS` | (la usa el SDK en local) | `http://0.0.0.0:$PORT` **expandida por el start command** |
| `ConnectionStrings__DefaultConnection` | `DataSource=app.db;Cache=Shared` | `DataSource=/var/data/app.db;Cache=Shared` |
| `Redis__ConnectionString` | vacío → cache en memoria | `rediss://default:<PASSWORD>@<HOST>:<PORT>` |
| `RabbitMq__ConnectionString` | vacío → sin cola | `amqps://<user>:<pass>@<host>/<vhost>` |
| `RabbitMq__QueueName` | `solicitudes.notificaciones` | `solicitudes.notificaciones` |
| `RabbitMq__ConsumerEnabled` | `true` | `true` (probar con `false`) |
| `RabbitMq__PrefetchCount` | `10` | `10` |

Notas:

- Las credenciales **nunca** se suben al repositorio: se configuran en el dashboard de Render (Variables de entorno) o en `appsettings.Development.json` local fuera de Git.
- Si `Redis__ConnectionString` está vacío, la app usa `IDistributedCache` en memoria para que el desarrollo local funcione sin Redis. En Render **debe** estar configurada.
- Si `RabbitMq__ConnectionString` está vacío, el publicador registra el error y advierte que la notificación no pudo encolarse; el consumidor queda inactivo.
- La compresión dinámica de estáticos se desactiva (`<CompressionEnabled>false</CompressionEnabled>` en `CreditoPlataforma.csproj`) para evitar interacciones con WebSocket/negotiate y garantizar que los recursos se sirvan sin gzip (evita bloqueos SRI en scripts de Identity).

---

## 4. Estructura de ramas y PRs

| Pregunta | Rama | PR |
| --- | --- | --- |
| 1 Bootstrap + dominio | `feature/bootstrap-dominio` | #1 |
| 2 Catálogo y filtros | `feature/catalogo-solicitudes` | #2 |
| 3 Registro y validaciones | `feature/solicitudes` | #3 |
| 4 Sesión y cache Redis | `feature/sesion-redis` | #4 |
| 5 Panel del analista | `feature/panel-analista` | #5 |
| 6 WebSocket | `feature/websocket-notificaciones` | #6 |
| 7 Cloud MQ | `feature/cloudmq-notificaciones` | #7 |
| 8 Deploy Render | `deploy/render` | #8 |

Todas las ramas se crearon desde `main` actualizado y se cerraron con PR hacia `main`. No se trabajó directamente sobre `main`.

---

## 5. Reglas de negocio y validaciones (en servidor)

- `IngresosMensuales > 0` y `MontoSolicitado > 0`: atributos `[Range]`, check constraints de SQLite (`CK_Cliente_IngresosMensuales`, `CK_Solicitud_MontoSolicitado`).
- Solo una solicitud **Pendiente** por cliente: índice único parcial `IX_SolicitudesCredito_ClienteId ON (ClienteId) WHERE Estado = 0`.
- Registro: usuario autenticado, cliente activo, sin otra solicitud Pendiente y monto ≤ **10x** los ingresos mensuales.
- Aprobación: monto ≤ **5x** los ingresos mensuales; no se procesan solicitudes ya aprobadas/rechazadas; el rechazo exige `MotivoRechazo`.
- Filtros del catálogo: rechaza montos negativos y rangos de fechas invertidos (feedback en la misma vista).
- Panel `/Analista`: `[Authorize(Roles="Analista")]` → usuarios sin rol reciben *Acceso denegado*.

---

## 6. Evidencia — Pregunta 6: WebSocket

1. Abrir **dos navegadores/ventanas anónimas distintas**: una con `cliente1@demo.com` y otra con `analista@demo.com`.
2. En el cliente: entrar a **Mis solicitudes**. El badge `#estado-conexion` debe mostrar **Conectado (WebSocket)**.
3. En DevTools → *Network → WS*, filtrar por `hubs/solicitudes`: se observa la conexión `wss://.../hubs/solicitudes?id=...`.
4. El analista aprueba o rechaza una solicitud pendiente del cliente 1.
5. En la sesión del cliente 1 la fila cambia de estado **sin recargar** y aparece el aviso *Actualización en tiempo real*.
6. Con `cliente2@demo.com` abierto en otra ventana se comprueba que **no** recibe el evento.
7. Conexión anónima al Hub: `POST /hubs/solicitudes/negotiate` sin cookie de autenticación responde **401**.
8. Reconexión: cortar la red (DevTools → Offline) y restaurarla; el badge pasa por *Reconectando...* → *Conectado (reconectado)* y se invoca `ObtenerMisEstados` para recuperar el estado vigente.

Flujo respetado: **guardar en SQLite → invalidar caché Redis → emitir el evento**, dirigido con `Clients.User(usuarioId)` usando la identidad del servidor (`Context.UserIdentifier`); el navegador no envía `UsuarioId`.

---

## 7. Evidencia — Pregunta 7: Cloud MQ

### 7.1 Publicación

- Al registrar una solicitud válida se publica `SolicitudRegistrada` con `MessageId` (UUID), `SolicitudId`, `UsuarioId` y `FechaEventoUtc`.
- La publicación usa **publisher confirms** (`CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)`) y mensajes persistentes (`Persistent = true`).
- Si la validación o la persistencia fallan, **no se publica**. Si falla la publicación, la solicitud se conserva y la vista muestra la advertencia con el `MessageId` para reenviarlo manualmente (no se usa outbox).

### 7.2 Consumidor

- `BackgroundService` con `RabbitMQ.Client`, cola durable `solicitudes.notificaciones`.
- **ACK manual únicamente después de guardar** la `Notificacion` (Id, MessageId, SolicitudId, UsuarioId, Texto, FechaProcesamientoUtc).
- Índice único sobre `MessageId`: un redelivery **no** genera duplicado (se confirma sin insertar).
- Mensaje inválido o fallo de procesamiento: se registra en log y se rechaza **sin reencolar** (sin reintentos infinitos).

### 7.3 Prueba reproducible

```bash
# 1) Detener el consumidor y arrancar
RabbitMq__ConsumerEnabled=false dotnet run

# 2) Registrar una solicitud con usuario cliente1@demo.com
# 3) En CloudAMQP → Queue → mensajes: aparece 1 mensaje pendiente (Ready)

# 4) Reactivar el consumidor
RabbitMq__ConsumerEnabled=true dotnet run

# 5) Verificar que la cola queda en 0 y que en "Mis notificaciones" aparece UNA notificación:
#    "Recibimos tu solicitud de crédito y está pendiente de evaluación"

# 6) Reenviar (Requeue / Publish message) el mismo MessageId desde CloudAMQP
# 7) Comprobar en logs: "Redelivery detectado: <MessageId> ya estaba procesada"
#    y que la tabla Notificaciones sigue con un solo registro por MessageId
```

Las capturas y los pasos quedan documentados aquí y en cada PR.

---

## 8. Despliegue en Render

1. Subir el repo a GitHub (ya realizado).
2. En Render: **New → Blueprint** y seleccionar el repositorio; se lee `render.yaml`.
3. Completar las variables marcadas como *secret* (`Redis__ConnectionString`, `RabbitMq__ConnectionString`).
4. Desplegar.

> **Nota (plan Free):** Render no ofrece discos ni runtime `.NET` nativo en planes gratuitos, por lo que el despliegue usa **Docker** (`runtime: docker` + `Dockerfile` multi-etapa con .NET 10). Sin disco persistente, `app.db` vive en `/app` y se regenera en cada deploy; el seed (`SeedData.InitializeAsync`) repuebla los usuarios y solicitudes de demostración automáticamente. Para datos persistentes, pasarse a un plan con discos y agregar el bloque `disk` montando `/var/data`.

### Variables mínimas

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:${PORT}   # debe expandirse en el start command, no en la variable
ConnectionStrings__DefaultConnection=DataSource=/var/data/app.db;Cache=Shared
Redis__ConnectionString=rediss://...
RabbitMq__ConnectionString=amqps://...
RabbitMq__QueueName=solicitudes.notificaciones
RabbitMq__ConsumerEnabled=true
```

### Comando de inicio (expansión de `PORT`)

Render expone el puerto en la variable `PORT`. **No** se asume que `${PORT}` se expanda dentro de una variable de entorno: la expansión se hace en el shell del start command:

```bash
ASPNETCORE_URLS="http://0.0.0.0:$PORT" dotnet bin/Release/net10.0/CreditoPlataforma.dll
```

Equivalente en el dashboard de Render (Start Command):

```bash
dotnet bin/Release/net10.0/CreditoPlataforma.dll --urls "http://0.0.0.0:$PORT"
```

### Persistencia de SQLite entre despliegues y reinicios

- El bloque `disk` de `render.yaml` monta un disco persistente en `/var/data` y la conexión apunta a `/var/data/app.db`, por lo que los datos sobreviven a reinicios y despliegues (el sistema de archivos efímero se descarta).
- Si se usa el **plan Free** (sin discos), hay dos opciones: subir a un plan con discos o eliminar el bloque `disk` y aceptar que `app.db` se regenera en cada deploy (el seed repuebla los datos de demostración). En local, `app.db` vive en la raíz del proyecto.
- Las migraciones se aplican automáticamente al arrancar, así que el archivo se crea/actualiza solo.

### Checklist de verificación online

- [ ] Login y registro de usuario.
- [ ] Registro de solicitud y validaciones (monto > 10x ingresos, cliente inactivo, segunda Pendiente).
- [ ] Panel del analista: aprobar / rechazar con motivo; acceso denegado sin rol.
- [ ] Sesión y cache Redis: enlace *Ver última solicitud {Monto}* y caché de 60 s del listado.
- [ ] WebSocket seguro (`wss://`) en DevTools, aviso sin recargar.
- [ ] Publicación y consumo en CloudAMQP con cola en 0 y notificación visible.
- [ ] Reconexión del Hub y recuperación de estado.

---

## 9. Relación entre las prácticas

- **WebSocket** comunica al navegador el resultado de la evaluación de inmediato.
- **Cloud MQ** desacopla el registro de la solicitud del procesamiento de su notificación de recepción.
- Cada práctica se demuestra por separado y tiene su propio PR hacia `main`.
