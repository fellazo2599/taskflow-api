# TaskFlow API

API REST para gestionar tareas, hecha con ASP.NET Core 10 y PostgreSQL.

Cada tarea tiene título, descripción, estado (`Pending`, `InProgress`, `Done`), responsable y fecha límite. La API permite crear, listar, ver, editar y borrar tareas. El listado se puede filtrar por estado y por responsable, y está paginado.

**Stack:** .NET 10 (LTS), ASP.NET Core (controllers), EF Core 10 + Npgsql, FluentValidation, Serilog, xUnit, OpenAPI + Swagger UI, Docker.

---

## Cómo ejecutarlo

### Opción A: todo con Docker (no hace falta instalar .NET)

```bash
docker compose up -d --build
```

Esto levanta PostgreSQL 16 y la API; la API aplica las migraciones al arrancar. Cuando termina:

- Swagger UI: http://localhost:8080/swagger
- API: http://localhost:8080/api/tasks
- Health: http://localhost:8080/health/ready

Para pararlo: `docker compose down` (añade `-v` para borrar también los datos).

### Opción B: la API con `dotnet run`

Requiere el [.NET SDK 10](https://dotnet.microsoft.com/download), cualquier versión `10.0.x`.

```bash
docker compose up -d postgres         # solo la base de datos, en localhost:5433
dotnet run --project src/TaskFlow.Api
```

En `Development` la API aplica las migraciones pendientes al arrancar (`Database:ApplyMigrationsOnStartup`). Cuando arranca:

- Swagger UI: http://localhost:5250/swagger
- Documento OpenAPI: http://localhost:5250/openapi/v1.json

La base, el usuario y la contraseña son `taskflow`. El puerto es 5433 para no chocar con un PostgreSQL local en el 5432.

Si no tienes Docker, apunta la connection string a cualquier PostgreSQL:

```bash
ConnectionStrings__Default="Host=localhost;Port=5432;Database=taskflow;Username=<user>;Password=<pass>" \
  dotnet run --project src/TaskFlow.Api
```

### Tests

```bash
dotnet test
```

Los tests no necesitan Docker ni PostgreSQL: los de integración levantan la API real sobre SQLite en memoria.

### Migraciones

`dotnet-ef` está fijado como herramienta local del repo:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api

# crear una migración nueva
dotnet tool run dotnet-ef migrations add <Nombre> \
  --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api \
  --output-dir Persistence/Migrations
```

---

## Endpoints

| Método | Ruta | Respuestas |
|---|---|---|
| `GET` | `/api/tasks?status=&assignee=&page=1&pageSize=20` | `200` página de tareas · `400` filtro o paginación inválidos |
| `GET` | `/api/tasks/{id}` | `200` · `404` |
| `POST` | `/api/tasks` | `201` + header `Location` · `400` · `413` |
| `PUT` | `/api/tasks/{id}` | `200` · `400` · `404` · `413` |
| `DELETE` | `/api/tasks/{id}` | `204` · `404` |
| `GET` | `/health/live` | `200` si el proceso responde |
| `GET` | `/health/ready` | `200` si además la base de datos responde · `503` si no |

Además, cualquier endpoint de `/api` puede devolver `429` si se supera el rate limit, o `500` ante un error inesperado.

### Ejemplos

```bash
# Crear (status es opcional: por defecto es Pending)
curl -i -X POST http://localhost:8080/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"title":"Preparar demo","description":"Slides y datos","assignee":"Ana","dueDate":"2026-12-01"}'

# Listar las tareas pendientes de Ana, segunda página de 10
curl 'http://localhost:8080/api/tasks?status=Pending&assignee=ana&page=2&pageSize=10'

# Editar (PUT reemplaza la tarea completa: todos los campos son obligatorios)
curl -X PUT http://localhost:8080/api/tasks/1 \
  -H 'Content-Type: application/json' \
  -d '{"title":"Preparar demo","assignee":"Bob","status":"InProgress","dueDate":"2026-12-03"}'

# Borrar
curl -i -X DELETE http://localhost:8080/api/tasks/1
```

Con `dotnet run`, el puerto es `5250` en lugar de `8080`.

Respuesta del listado:

```json
{
  "items": [
    {
      "id": 1,
      "title": "Preparar demo",
      "description": "Slides y datos",
      "status": "Pending",
      "assignee": "Ana",
      "dueDate": "2026-12-01",
      "createdAt": "2026-11-18T15:55:20.47Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

### Formato de error

Todos los errores tienen el mismo cuerpo: **un único código estable** en `UPPER_SNAKE_CASE`. El status HTTP va en la respuesta, no en el body.

```json
{ "code": "DUE_DATE_IN_PAST" }
```

Los errores de validación siguen el formato `CAMPO_REGLA`. Si hay varios campos mal, se devuelve el primero.

| Código | Status | Cuándo |
|---|---|---|
| `TITLE_REQUIRED` / `TITLE_TOO_LONG` | 400 | Título vacío o de más de 200 caracteres |
| `DESCRIPTION_TOO_LONG` | 400 | Descripción de más de 2000 caracteres |
| `ASSIGNEE_REQUIRED` / `ASSIGNEE_TOO_LONG` | 400 | Responsable vacío o de más de 100 caracteres |
| `STATUS_REQUIRED` | 400 | Falta el estado en un `PUT` |
| `STATUS_INVALID` | 400 | Estado desconocido o numérico (en el body o en el query) |
| `DUE_DATE_REQUIRED` | 400 | Falta la fecha límite |
| `DUE_DATE_IN_PAST` | 400 | Al crear, la fecha límite es anterior a hoy (UTC) |
| `<CAMPO>_INVALID` | 400 | Un valor no se puede leer: tipo incorrecto, fecha que no es `yyyy-MM-dd` o JSON cortado dentro de ese campo (p. ej. `DUE_DATE_INVALID`, `PAGE_INVALID`) |
| `PAGE_OUT_OF_RANGE` / `PAGE_SIZE_OUT_OF_RANGE` | 400 | `page` fuera de 1–21.474.836, o `pageSize` fuera de 1–100 |
| `REQUEST_INVALID` | 400 | Body vacío o JSON ilegible |
| `TASK_NOT_FOUND` | 404 | La tarea no existe o fue borrada |
| `PAYLOAD_TOO_LARGE` | 413 | El body supera 1 MB |
| `TOO_MANY_REQUESTS` | 429 | Se superó el rate limit (incluye el header `Retry-After`) |
| `SERVER_ERROR` | 500 | Error inesperado (se registra en el log, sin dar detalles al cliente) |

Los rechazos que se producen antes de llegar a la lógica de la API (ruta inexistente `404`, método no permitido `405`, `Content-Type` distinto de JSON `415`) devuelven solo el status, sin cuerpo.

---

## Arquitectura

```
src/
  TaskFlow.Domain/          Entidades y reglas puras: TaskItem, TaskState, BaseEntity, límites de campos
  TaskFlow.Application/     Casos de uso: TaskService, Rm/Vm, validators, adapters,
                            ITaskRepository, ErrorCodes, NotFoundException
  TaskFlow.Infrastructure/  EF Core: AppDbContext, configuraciones, interceptores, TaskRepository,
                            migraciones, health check de la base de datos
  TaskFlow.Api/             Controllers, manejo de errores, middlewares (correlation id, headers de seguridad),
                            logging, CORS, rate limiting, health endpoints, Program.cs
tests/
  TaskFlow.Tests/           Unit/ (validators, service, errores) + Integration/ (API completa)
```

Las dependencias van así: `Api → Application → Domain ← Infrastructure` (y `Api → Infrastructure` solo para registrar el DI). Application no conoce EF Core ni HTTP.

Recorrido de una petición:

`TasksController` (no tiene lógica) → `TaskService`, que valida con FluentValidation, construye objetos con los adapters y aplica las reglas → `ITaskRepository` → EF Core.

Si algo falla, el `GlobalExceptionHandler` traduce la excepción a status HTTP + `{ code }`.

El pipeline HTTP, en orden:

1. correlation id
2. log de la request
3. manejo de errores
4. HSTS y redirección a HTTPS
5. headers de seguridad
6. Swagger (solo Development)
7. CORS
8. rate limiting
9. controllers y health checks

---

## Decisiones

**Arquitectura y capas**

- **.NET 10 (LTS).** Es la versión con soporte largo. .NET 9 (STS) dejó de tener soporte en noviembre de 2026.
- **Clean Architecture en 4 proyectos.** Para una sola entidad puede parecer mucho, pero la prueba pide desacoplamiento y así se ve:
  - el dominio no depende de nada;
  - los casos de uso no dependen de EF Core ni de ASP.NET;
  - `TaskService` se testea con un repositorio fake.
- **Repositorio definido en Application e implementado en Infrastructure.** Así la persistencia se puede cambiar o simular sin tocar los casos de uso. No hay repositorio genérico ni Unit of Work: con un solo agregado no aportan nada.
- **Controllers finos.** Solo delegan en el servicio y declaran sus respuestas con `ProducesResponseType`, que alimenta el documento OpenAPI.
- **DTOs `*Rm` (entrada) / `*Vm` (salida) y adapters estáticos** (`ToEntity`, `ApplyTo`, `ToVm`). La entidad nunca sale por la API, y la creación y el mapeo de objetos están en un solo sitio por cada dirección.
- **Sin strings ni números mágicos.** Los códigos de error, los nombres de settings, los límites de campos, las rutas de health y los headers son constantes o enums.

**Validación y errores**

- **FluentValidation es el único mecanismo de validación.** Se valida dentro del servicio, de modo que cualquier punto de entrada queda validado. El `[Required]` implícito de MVC está desactivado para no mezclar dos sistemas.
- **Errores centralizados** en un único `IExceptionHandler`, que es el único lugar donde se decide el status HTTP:
  - `ValidationException` → 400
  - `NotFoundException` → 404
  - `BadHttpRequestException` → su propio status, por ejemplo 413
  - cualquier otra → 500 (y se loguea)

  Application lanza excepciones que llevan solo el código, sin status HTTP.
- **El body de error es solo `{ code }`.** No se devuelven mensajes: el cliente traduce el código a su idioma. Los errores de *model binding* (JSON mal formado, enum desconocido, tipo incorrecto) también salen en este formato, en lugar del `ProblemDetails` por defecto.
- **Contrato estricto.** Los enums viajan solo como texto (`"InProgress"`); un número se rechaza, aunque .NET lo aceptaría por defecto.

**Datos y persistencia**

- **PostgreSQL + EF Core con migraciones**, columnas en `snake_case` y enums guardados como texto. Así la base se puede leer desde `psql` y no se rompe si cambia el orden del enum.
- **Soft delete y auditoría con interceptores de EF:**
  - `CreatedAt` y `UpdatedAt` se rellenan solos.
  - Un `Remove()` se convierte en `IsDeleted = true`.
  - Un global query filter oculta las filas borradas.

  Los servicios no tienen que acordarse de nada de esto.
- **Migraciones al arrancar solo si `Database:ApplyMigrationsOnStartup` está activo** (lo está en Development). En producción deberían ejecutarse en el pipeline de despliegue.

**Reglas de negocio** (supuestos, porque el enunciado no las define)

- Al crear, si no se manda el estado, es `Pending`.
- Al crear, la fecha límite no puede ser anterior a hoy (UTC). Al editar sí puede, para que una tarea vencida se siga pudiendo actualizar.
- `PUT` reemplaza la tarea completa: todos los campos son obligatorios.
- El responsable es texto libre (máximo 100 caracteres), porque no hay módulo de usuarios. El filtro por responsable compara el texto completo, sin distinguir mayúsculas.
- Se recortan los espacios de los textos, y una descripción vacía se guarda como `null`.
- El listado se ordena por fecha límite ascendente y después por `id`, para que la paginación sea estable.
- La paginación va por `page` / `pageSize` (20 por defecto, máximo 100). Los valores fuera de rango devuelven 400 en lugar de recortarse en silencio. `page` tiene un tope para que el `OFFSET` de SQL no desborde.
- La "fecha de hoy" sale de un `TimeProvider` inyectado, así que las reglas con fechas se testean con un reloj fijo.

**Logs**

- **Serilog** con logs estructurados. Los niveles se configuran en la sección `Serilog` de `appsettings`. El formato depende del entorno:
  - en Development, texto legible (y el SQL generado);
  - en el resto, un JSON por línea, listo para un agregador de logs.
- **Una línea por request** con método, ruta, status y duración. Los health checks sanos no se loguean, para no llenar el log de sondeos.
- **Correlation id.** Cada respuesta lleva `X-Correlation-Id`, y todas las líneas de log de esa request incluyen el mismo id. Si el cliente manda su propio id, se reutiliza, pero solo si tiene un formato seguro (para evitar inyección en los logs). Si no, se usa el trace id.
- **Qué se loguea:**
  - las escrituras (crear, editar, borrar), en `Information` y con el id de la tarea;
  - el rate limit superado, en `Warning`;
  - los errores inesperados, en `Error` y con la excepción.

  Los errores esperados (400/404) no se loguean como errores. Nunca se loguean bodies ni datos sensibles.

**Seguridad**

- **CORS desde configuración** (`Cors:AllowedOrigins`), limitado a los verbos y headers que usa la API. Si la lista está vacía, no se permite ningún origen cruzado; Development permite `localhost:3000` y `localhost:5173`.
- **Headers de seguridad:** `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, más una CSP estricta y `no-store` en `/api`. Se añaden al empezar la respuesta, de modo que también salen en las respuestas de error.
- **Rate limiting** de ventana fija por IP: 100 peticiones por minuto, configurable. Al superarlo devuelve `429` con `Retry-After`. Los health checks quedan excluidos.
- **Body limitado a 1 MB** (Kestrel permite 30 MB por defecto).
- HSTS fuera de Development, sin el header `Server` de Kestrel, y el contenedor Docker corre con un usuario sin privilegios.
- No hay autenticación: el enunciado no la pide (ver la sección siguiente).

**Operación**

- **Health checks:** `/health/live` comprueba solo el proceso; `/health/ready` comprueba además la base de datos y devuelve `503` si no responde, para que un orquestador deje de mandarle tráfico.
- **Docker:** build multi-stage, cacheando el restore por los `.csproj`, con imagen final de runtime. `docker compose` levanta la base y la API, y la API espera a que la base esté sana.
- **CI con GitHub Actions** en cada push a `main` y en cada PR: formato (`dotnet format`), build en Release, tests y build de la imagen Docker.

**Tests**

- **Unitarios:** validators, `TaskService` con repositorio fake, el adapter de códigos de error y el exception handler.
- **Integración:** `WebApplicationFactory` con el pipeline real y SQLite en memoria. Cubren:
  - `201` con `Location`, `204`, `404` y los `400` de validación y de binding;
  - filtros con paginación;
  - headers de seguridad y correlation id;
  - health checks;
  - `429`, y que los health checks no cuentan para el rate limit.
- **Por qué SQLite y no Postgres en los tests:** `dotnet test` funciona sin Docker. La contrapartida es que SQLite no es el motor de producción.

**Tooling**

- Central Package Management (`Directory.Packages.props`), `TreatWarningsAsErrors`, `.editorconfig` verificado con `dotnet format`, SDK fijado en `global.json` y `dotnet-ef` como herramienta local.

---

## Qué haría con más tiempo

**Funcionalidad**

- **Autenticación y autorización (JWT)**, y que el responsable sea un usuario real (con clave foránea) en vez de texto libre.
- **Concurrencia optimista** con `xmin` de Postgres y `ETag`/`If-Match`, devolviendo `409`/`412` para no pisar ediciones simultáneas.
- **`PATCH /api/tasks/{id}/status`** con reglas de transición de estado (por ejemplo, si una tarea `Done` se puede reabrir o no).
- Ordenación configurable (`sortBy`) y búsqueda por texto en título y descripción.
- Devolver la lista completa de errores de validación si el cliente la necesita.

**Base de datos y tests**

- **Tests de integración contra Postgres real con Testcontainers**, porque SQLite difiere en collation, tipos y traducción de funciones.
- **Índice para el filtro por responsable:** un índice funcional sobre `lower(assignee)` o una collation case-insensitive. Ahora mismo ese filtro no usa índice.

**Operación**

- Migraciones con `dotnet ef migrations bundle` en el pipeline de despliegue, en lugar de aplicarlas al arrancar.
- `ForwardedHeaders` para que el rate limit vea la IP real detrás de un proxy, y un rate limit distribuido (Redis) si hay varias instancias.
- OpenTelemetry (trazas y métricas) exportado a un backend de observabilidad.
- Versionado de la API.

---

## Cómo usé la IA

Usé **Claude Code** (Claude Opus) como asistente de programación en la terminal. Trabajé con él como con un compañero al que le doy contexto y le reviso el trabajo, no como generador automático.

**Qué le pedí**

- Que antes de escribir código me presentara un plan y una estimación del tamaño del trabajo, y que esperara mi aprobación.
- Que siguiera las convenciones 
  - controllers finos;
  - DTOs `Rm`/`Vm` y adapters estáticos;
  - FluentValidation;
  - errores `{ code }` centralizados;
  - paginación con `PagedResult`;
  - soft delete con auditoría;
  - CORS por configuración;
  - una clase por archivo y sin strings mágicos.
- Arquitectura por capas con foco en el desacoplamiento, validación con FluentValidation, manejo de CORS y seguridad, errores centralizados y adapters para crear objetos.

**Qué acepté de su propuesta**

- .NET 10 en lugar de .NET 9, que es lo que usa mi proyecto de referencia, porque .NET 9 ya no tiene soporte.
- La separación en 4 proyectos con `ITaskRepository` en Application. Mi proyecto de referencia no tiene repositorio: los servicios usan el `DbContext` directamente.
- Llevar la validación al servicio y decidir el status HTTP solo en el exception handler. En mi proyecto de referencia se valida en el controller y la excepción de negocio lleva el status.
- Devolver 400 cuando la paginación está fuera de rango, en lugar de recortarla, y que CORS con la lista vacía no permita ningún origen, en lugar de permitirlos todos.
- SQLite en memoria para los tests de integración (con la limitación anotada arriba) y PostgreSQL con `docker-compose` en el puerto 5433.

**Qué se corrigió durante el desarrollo**

Antes de cada commit se compiló, se ejecutó la API y se probó con `curl` contra un PostgreSQL real (status codes, códigos de error, soft delete en la tabla, CORS, headers y 429). Así salieron estos problemas:

- **Headers de seguridad.** En la primera versión, las respuestas de error (`404`, `400`) salían sin ellos, porque el exception handler limpia la respuesta. Se pasaron a `Response.OnStarting`, y hay un test de integración que lo cubre.
- **Orden del pipeline.** Swagger UI quedaba fuera del middleware de headers; se reordenó.
- **Docker.** No estaba disponible en la máquina, así que la migración y la API se verificaron contra un PostgreSQL 16 local con la connection string por variable de entorno. El `docker-compose.yml` solo se validó con `docker compose config`.

Todas las decisiones de diseño y las reglas de negocio supuestas están en este README para que se puedan revisar.
