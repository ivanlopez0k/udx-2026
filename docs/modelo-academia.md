# Modelo de Academia: docentes en muchas academias

Rama: `ModeloAcademia` · Migración: `AcademiaMuchosDocentes`

## Resumen

Antes cada `PerfilDocente` pertenecía a **una** academia (`PerfilDocente.AcademyId`, obligatorio).
Ahora la relación es **muchos a muchos** a través de la tabla `DocentesAcademias`, con un rango por academia.

- Todo docente tiene siempre **exactamente una academia personal** (`IsPersonal = true`), que no se muestra.
- Además puede ser miembro de varias academias **oficiales** (`IsPersonal = false`).
- `SolicitudClase.AcademyId` y `ClaseAgendada.AcademyId` **no cambian**.

```
Usuario 1 ──── 0..1 PerfilDocente 1 ──── * DocenteAcademia * ──── 1 Academia
   │                                        (Role, JoinedAt)          │
   └──────────── 0..* (creador, TeacherId nullable) ──────────────────┘
```

## Entidades

### Nueva: `DocenteAcademia` (tabla `DocentesAcademias`)

| Campo | Tipo C# | SQL | Null | Notas |
|---|---|---|---|---|
| Id | int | int identity | No | PK (hereda de `EntityBase`) |
| TeacherId | int | int | No | FK → `PerfilesDocentes.Id` (mismo nombre que en `DocenteMateria`) |
| AcademyId | int | int | No | FK → `Academias.Id` |
| Role | `AcademyRole` | int | No | `DOCENTE = 0`, `VICEDIRECTOR = 1`, `DIRECTOR = 2` |
| JoinedAt | DateTime | datetime2 | No | Fecha de ingreso (UTC) |

- Índice único `(TeacherId, AcademyId)`: un docente no puede estar dos veces en la misma academia.
- Navegaciones: `PerfilDocente.DocentesAcademias` y `Academia.Members` (configuradas con Fluent API).
- Las FKs quedan con `DeleteBehavior.NoAction`, como el resto del modelo.

### Modificada: `Academia` (tabla `Academias`)

| Campo | Tipo C# | SQL | Null | Cambio |
|---|---|---|---|---|
| Id | int | int | No | sin cambios |
| Name | string | nvarchar(max) | No | sin cambios |
| Description | string | nvarchar(max) | No | sin cambios |
| Code | string | nvarchar(450) | No | sin cambios (índice único) |
| CreatedAt | DateTime | datetime2 | No | sin cambios |
| **IsPersonal** | bool | bit | No | **nuevo**, default `0` |
| **Location** | string? | nvarchar(max) | Sí | **nuevo** (vacío en las personales) |
| **Phone** | string? | nvarchar(max) | Sí | **nuevo** (vacío en las personales) |
| **Email** | string? | nvarchar(max) | Sí | **nuevo** (vacío en las personales) |
| **TeacherId** | int? | int | **Sí** | **antes `int` obligatorio**. Ahora significa "usuario que la creó"; no da permisos; queda `null` si ese usuario se elimina |
| **Members** | `ICollection<DocenteAcademia>` | — | — | **nuevo** (navegación) |

### Modificada: `PerfilDocente` (tabla `PerfilesDocentes`)

| Campo | Tipo C# | SQL | Null | Cambio |
|---|---|---|---|---|
| ~~AcademyId~~ | ~~int~~ | ~~int~~ | ~~No~~ | **eliminado** (con su FK `FK_PerfilesDocentes_Academias_AcademyId` e índice `IX_PerfilesDocentes_AcademyId`) |
| ~~Academy~~ | ~~Academia~~ | — | — | **eliminado** (navegación) |
| **DocentesAcademias** | `ICollection<DocenteAcademia>` | — | — | **nuevo** (navegación) |

## Migración `AcademiaMuchosDocentes`

EF generaba la migración borrando `PerfilesDocentes.AcademyId` primero, lo que perdía la academia de cada docente.
El `Up()` se reescribió a mano en este orden (todo dentro de la transacción de la migración, sin IDs hardcodeados):

1. **Academias**: agrega `IsPersonal` (bit, not null, default 0), `Location`, `Phone`, `Email` (nullable) y pasa `TeacherId` a nullable.
2. **Crea `DocentesAcademias`** con sus FKs, índice por `AcademyId` e índice único `(TeacherId, AcademyId)`.
3. **Copia la relación actual**: por cada `PerfilDocente` inserta una membresía en la academia de su `AcademyId`.
   `Role = DIRECTOR (2)` si `Academias.TeacherId = PerfilesDocentes.UserId`, si no `DOCENTE (0)`. `JoinedAt = GETUTCDATE()`.
4. **Detecta academias personales** (ver criterio abajo) y les pone `IsPersonal = 1`.
5. **Normaliza rangos**: en las academias personales todas las membresías pasan a `DOCENTE (0)`.
6. **Completa personales faltantes**: a cada docente sin membresía en una academia personal (los que se habían registrado con código)
   se le crea una (`Name = "{u.Name} Academy"`, `Description = ''`, `Code` de 8 caracteres con `NEWID()` como en
   `AcademiaCodeAndAcademyIdFix`, `CreatedAt = GETUTCDATE()`, `TeacherId = u.Id`, `IsPersonal = 1`) y luego su membresía `DOCENTE`.
   Se hace en dos sentencias: 6a crea las academias, 6b vincula cada docente con la academia personal más nueva y sin miembros cuyo creador es él.
7. **Recién ahora** borra FK, índice y columna `PerfilesDocentes.AcademyId`.

### Criterio para detectar academias personales

Una academia existente se marca como personal si cumple **todo**:

- `Name = CONCAT(nombre del usuario creador, ' Academy')`
- `Description = ''` o `NULL`
- **no tiene miembros distintos de su creador** en `DocentesAcademias` (después del paso 3)

Son las que creaba automáticamente el `Register` cuando el docente no ingresaba código.
Si ya tienen más de un miembro (otros docentes se unieron con el código) quedan como **oficiales**, y su creador mantiene el rango `DIRECTOR`.

### `Down()`

1. Vuelve a crear `PerfilesDocentes.AcademyId` (nullable mientras se puebla).
2. Lo puebla con la academia **oficial** de mayor rango (y, a igual rango, la de `JoinedAt` más antiguo); si no tiene oficiales, la personal.
3. Borra las academias personales que no quedaron asignadas a ningún perfil ni son usadas por solicitudes/clases (las que creó el paso 6).
4. Vuelve `TeacherId` a obligatorio. **Limitación**: si un creador fue eliminado (`TeacherId = null`) no se puede recuperar el original;
   se asigna el usuario del miembro de mayor rango y más antiguo. Si la academia además no tiene miembros, el `Down()` se aborta con
   `THROW 50000` y hay que corregir esos registros a mano antes de revertir.
5. Elimina `DocentesAcademias` y las columnas nuevas, y restaura índice y FK de `PerfilesDocentes.AcademyId`.

## Cambios en endpoints

### `POST api/Auth/register` (rama TEACHER)

- Si viene `AcademyCode`: se busca por `Code` (case-insensitive). Si no existe **o es personal** → `400 "Código de academia no válido."` (mismo mensaje en ambos casos).
- Siempre se crea la academia personal (`"{Name} Academy"`, `Description = ""`, código aleatorio, `TeacherId = usuario.Id`, `IsPersonal = true`).
- Se crea el `PerfilDocente` (ya sin `AcademyId`) y la membresía `DOCENTE` en la personal.
- Si el código era válido, además se crea la membresía `DOCENTE` en esa academia oficial.
- La rama PARENT, el hashing y el Login no cambiaron.

### `GET api/Docente/{id}` — **cambio de contrato**

Antes devolvía `"academy": "<nombre>"` (string). Ahora devuelve `"academias"`: lista de academias **oficiales** del docente.
La personal nunca se incluye.

```json
"academias": [
  { "id": 3, "name": "Academia de Prueba" }
]
```

El cliente que consumía `academy` tiene que actualizarse (hoy no hay usos en `UDX.Server.Client` ni en `UDX.Shared`).

### `DELETE api/Docente/cuenta`

- Borra las membresías del docente (`DocentesAcademias`).
- Borra **solo** su academia personal.
- En las academias oficiales con `TeacherId = userId` pone `TeacherId = null` (no se borran).
- No reasigna director: eso lo hará otro endpoint (ver reglas).

### Seed

- "Academia de Prueba" (`SEED0001`) es **oficial** (`IsPersonal = false`) con `Location`/`Phone`/`Email` de prueba
  (si ya existía sin esos datos, se completan).
- El profesor de prueba tiene su academia personal + membresía `DOCENTE`, y membresía `DIRECTOR` en "Academia de Prueba".
- Sigue siendo idempotente y usa BCrypt.

## Reglas de negocio para los próximos endpoints

1. Todo docente tiene **exactamente 1** academia personal.
2. **Nadie se une** a una academia personal (unirse por código debe rechazar academias con `IsPersonal = true`).
3. La academia personal **no se muestra** en listados ni perfiles.
4. `Academia.TeacherId` = **creador**. **No da permisos**.
5. Los permisos salen de `DocenteAcademia.Role` (`DOCENTE`, `VICEDIRECTOR`, `DIRECTOR`).
6. Si una academia oficial se queda sin `DIRECTOR`, se asigna `DIRECTOR` al miembro más antiguo (`JoinedAt` menor).
7. El `Code` de una academia solo lo ven `DIRECTOR` y `VICEDIRECTOR`.
8. Ver una academia requiere estar logueado.

## Pendientes a confirmar con el cliente

- ¿Un docente puede realmente estar en varias academias oficiales a la vez? (el modelo lo permite)
- ¿A qué academia va el pago de una clase (`PagoDocente`)? Hoy el pago se relaciona con el docente y la clase, no con la academia.
- ¿Existen los rangos `VICEDIRECTOR`? (está en el enum pero ningún flujo lo asigna todavía)

## Problemas conocidos detectados (sin arreglar)

- **`EliminarCuenta` puede fallar por FKs**: todas las FKs son `NoAction`, y el endpoint no borra `DocentesMaterias`, `Resenas`,
  `SolicitudesClases`, `ClasesAgendadas`, `PagosDocentes`, `Contenidos` ni `Mensajes` que referencian al perfil/usuario.
  Si existen, el `SaveChanges` lanza excepción.
- **`EliminarCuenta` y la academia personal**: si la personal está referenciada por `SolicitudesClases.AcademyId` o
  `ClasesAgendadas.AcademyId` (las solicitudes viejas apuntaban a la academia autogenerada), su borrado también falla por FK.
- **`EliminarCuenta` no borra la foto** de perfil subida a `wwwroot/uploads/perfiles`.
- **`EliminarCuenta` no reasigna director** en las oficiales donde el docente era `DIRECTOR` (queda pendiente para otro endpoint).
- **`Register` hace varios `SaveChanges` sin transacción**: si falla a mitad puede quedar un usuario sin perfil o una academia personal huérfana.
  El `try/catch` del final no cubre los `SaveChanges` anteriores.
- **`GenerateAcademyCode` no verifica colisiones** con el índice único de `Code` (poco probable, pero posible).
- `Usuario.Name` no es único: dos usuarios con el mismo nombre generan academias personales con el mismo `Name` (no rompe nada, el `Code` es el único).

## Después de mergear

Cada integrante del equipo tiene que actualizar su base local:

```bash
dotnet ef database update --project UDX.BD --startup-project UDX.Server\UDX.Server
```
