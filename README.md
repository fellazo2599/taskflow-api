# TaskManager API

API REST para gestionar tareas, hecha con ASP.NET Core 10 y PostgreSQL.

Cada tarea tiene título, descripción, estado (`Pending`, `InProgress`, `Done`), responsable y fecha límite. La API permite crear, listar, ver, editar y borrar tareas. El listado se puede filtrar por estado y por responsable, y está paginado.

**Stack:** .NET 10 (LTS), ASP.NET Core (controllers), EF Core 10 + Npgsql, FluentValidation, xUnit, OpenAPI + Swagger UI.

---

## Cómo ejecutarlo

### Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (la versión está fijada en `global.json`)
- Docker, para levantar PostgreSQL. Si no tienes Docker, mira [Sin Docker](#sin-docker).

### 1. Levantar la base de datos

```bash
docker compose up -d
```

Esto levanta PostgreSQL 16 en `localhost:5433`, con base, usuario y contraseña `taskmanager`. Uso el puerto 5433 para no chocar con un Postgres local que esté en el 5432.

### 2. Arrancar la API

```bash
dotnet run --project src/TaskManager.Api
```

En `Development` la API **aplica las migraciones pendientes al arrancar** (`Database:ApplyMigrationsOnStartup`). Cuando arranca:

- Swagger UI: http://localhost:5250/swagger
- Documento OpenAPI: http://localhost:5250/openapi/v1.json

### 3. Ejecutar los tests

```bash
dotnet test
```

Los tests no necesitan Docker ni Postgres: los de integración usan SQLite en memoria.

### Migraciones a mano

`dotnet-ef` está fijado como herramienta local del repo:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api
```

Para crear una migración nueva:

```bash
dotnet tool run dotnet-ef migrations add <Nombre> \
  --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api \
  --output-dir Persistence/Migrations
```

### Sin Docker

Apunta la connection string a cualquier PostgreSQL con una variable de entorno:

```bash
ConnectionStrings__Default="Host=localhost;Port=5432;Database=taskmanager;Username=<user>;Password=<pass>" \
  dotnet run --project src/TaskManager.Api
```

---

## Endpoints

| Método | Ruta | Respuestas |
|---|---|---|
| `GET` | `/api/tasks?status=&assignee=&page=1&pageSize=20` | `200` página de tareas · `400` filtro o paginación inválidos |
| `GET` | `/api/tasks/{id}` | `200` · `404` |
| `POST` | `/api/tasks` | `201` + header `Location` · `400` |
| `PUT` | `/api/tasks/{id}` | `200` · `400` · `404` |
| `DELETE` | `/api/tasks/{id}` | `204` · `404` |

Además, cualquier endpoint puede devolver `429` si se supera el rate limit, o `500` ante un error inesperado.

### Ejemplos

```bash
# Crear (status es opcional: por defecto es Pending)
curl -i -X POST http://localhost:5250/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{"title":"Preparar demo","description":"Slides y datos","assignee":"Ana","dueDate":"2026-12-01"}'

# Listar las tareas pendientes de Ana, segunda página de 10
curl 'http://localhost:5250/api/tasks?status=Pending&assignee=ana&page=2&pageSize=10'

# Editar (PUT reemplaza la tarea completa: todos los campos son obligatorios)
curl -X PUT http://localhost:5250/api/tasks/1 \
  -H 'Content-Type: application/json' \
  -d '{"title":"Preparar demo","assignee":"Bob","status":"InProgress","dueDate":"2026-12-03"}'

# Borrar
curl -i -X DELETE http://localhost:5250/api/tasks/1
```

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
| `STATUS_INVALID` | 400 | Estado desconocido, en el body o en el query |
| `DUE_DATE_REQUIRED` | 400 | Falta la fecha límite |
| `DUE_DATE_IN_PAST` | 400 | Al crear, la fecha límite es anterior a hoy (UTC) |
| `DUE_DATE_INVALID` | 400 | La fecha no tiene formato `yyyy-MM-dd` |
| `PAGE_OUT_OF_RANGE` / `PAGE_SIZE_OUT_OF_RANGE` | 400 | `page < 1`, o `pageSize` fuera del rango 1–100 |
| `PAGE_INVALID` / `PAGE_SIZE_INVALID` | 400 | El valor no es numérico |
| `REQUEST_INVALID` | 400 | Body vacío o JSON mal formado |
| `TASK_NOT_FOUND` | 404 | La tarea no existe o fue borrada |
| `TOO_MANY_REQUESTS` | 429 | Se superó el rate limit (incluye el header `Retry-After`) |
| `SERVER_ERROR` | 500 | Error inesperado (se registra en el log, sin dar detalles al cliente) |

---

## Arquitectura

```
src/
  TaskManager.Domain/          Entidades y reglas puras: TaskItem, TaskState, BaseEntity, límites de campos
  TaskManager.Application/     Casos de uso: TaskService, Rm/Vm, validators, adapters,
                               ITaskRepository, ErrorCodes, NotFoundException
  TaskManager.Infrastructure/  EF Core: AppDbContext, configuraciones, interceptores,
                               TaskRepository, migraciones
  TaskManager.Api/             Controllers, manejo de errores, middlewares, CORS, rate limiting, Program.cs
tests/
  TaskManager.Tests/           Unit/ (validators, service, errores) + Integration/ (API completa)
```

Las dependencias van así: `Api → Application → Domain ← Infrastructure` (y `Api → Infrastructure` solo para registrar el DI). Application no conoce EF Core ni HTTP.

Recorrido de una petición:

`TasksController` (no tiene lógica) → `TaskService`, que valida con FluentValidation, construye objetos con los adapters y aplica las reglas → `ITaskRepository` → EF Core.

Si algo falla, el `GlobalExceptionHandler` traduce la excepción a status HTTP + `{ code }`.

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

**Validación y errores**

- **FluentValidation es el único mecanismo de validación.** Se valida dentro del servicio, de modo que cualquier punto de entrada queda validado. El `[Required]` implícito de MVC está desactivado para no mezclar dos sistemas.
- **Errores centralizados** en un único `IExceptionHandler`, que es el único lugar donde se decide el status HTTP:
  - `ValidationException` → 400
  - `NotFoundException` → 404
  - cualquier otra → 500 (y se loguea)

  Application lanza excepciones que llevan solo el código, sin status HTTP.
- **El body de error es solo `{ code }`.** No se devuelven mensajes: el cliente traduce el código a su idioma. Los errores de *model binding* (JSON mal formado, enum desconocido, tipo incorrecto) también salen en este formato, en lugar del `ProblemDetails` por defecto.
- **Los errores de negocio esperados (400/404) no se loguean.** Solo se loguean los inesperados y las escrituras (crear, editar, borrar), con logs estructurados.

**Datos y persistencia**

- **PostgreSQL + EF Core con migraciones**, columnas en `snake_case` y enums guardados como texto. Así la base se puede leer desde `psql` y no se rompe si cambia el orden del enum.
- **Soft delete y auditoría con interceptores de EF:**
  - `CreatedAt` y `UpdatedAt` se rellenan solos.
  - Un `Remove()` se convierte en `IsDeleted = true`.
  - Un global query filter oculta las filas borradas.

  Los servicios no tienen que acordarse de nada de esto.
- **Migraciones al arrancar solo si `Database:ApplyMigrationsOnStartup` está activo** (lo está en Development). En producción deberían ejecutarse en el pipeline de despliegue.

**Reglas de negocio** (supuestos míos, porque el enunciado no las define)

- Al crear, si no se manda el estado, es `Pending`.
- Al crear, la fecha límite no puede ser anterior a hoy (UTC). Al editar sí puede, para que una tarea vencida se siga pudiendo actualizar.
- `PUT` reemplaza la tarea completa: todos los campos son obligatorios.
- El responsable es texto libre (máximo 100 caracteres), porque no hay módulo de usuarios. El filtro por responsable compara el texto completo, sin distinguir mayúsculas.
- Se recortan los espacios de los textos, y una descripción vacía se guarda como `null`.
- El listado se ordena por fecha límite ascendente y después por `id`, para que la paginación sea estable.
- La paginación va por `page` / `pageSize` (20 por defecto, máximo 100). **Los valores fuera de rango devuelven 400** en lugar de recortarse en silencio, para que el cliente se entere de su error.
- La "fecha de hoy" sale de un `TimeProvider` inyectado, así que las reglas con fechas se testean con un reloj fijo.

**Seguridad**

- **CORS desde configuración** (`Cors:AllowedOrigins`), limitado a los verbos y headers que usa la API. Si la lista está vacía, no se permite ningún origen cruzado; Development permite `localhost:3000` y `localhost:5173`.
- **Headers de seguridad:** `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, más una CSP estricta y `no-store` en `/api`. Se añaden al empezar la respuesta, porque el exception handler borra los headers que se hayan puesto antes.
- **Rate limiting** de ventana fija por IP: 100 peticiones por minuto, configurable. Al superarlo devuelve `429` con `Retry-After`.
- HSTS fuera de Development y sin el header `Server` de Kestrel.
- No hay autenticación: el enunciado no la pide (ver la sección siguiente).

**Tests**

- **Unitarios:** validators, `TaskService` con repositorio fake y el mapeo de códigos de error.
- **Integración:** `WebApplicationFactory` con el pipeline real y SQLite en memoria. Cubren 201/400/404/204, filtros con paginación, headers de seguridad y 429.
- **Por qué SQLite y no Postgres en los tests:** `dotnet test` funciona sin Docker. La contrapartida es que SQLite no es el motor de producción.

**Tooling**

- Central Package Management (`Directory.Packages.props`), `TreatWarningsAsErrors`, SDK fijado en `global.json` y `dotnet-ef` como herramienta local.

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

- Health checks (`/health/live`, `/health/ready` comprobando la BD).
- Dockerfile de la API y un `docker-compose` completo.
- CI en GitHub Actions (build + test).
- Migraciones con `dotnet ef migrations bundle` en el despliegue, en lugar de aplicarlas al arrancar.
- `ForwardedHeaders` para que el rate limit vea la IP real detrás de un proxy, y un rate limit distribuido (Redis) si hay varias instancias.
- Observabilidad con OpenTelemetry (trazas y métricas) y un correlation id en los logs.
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
