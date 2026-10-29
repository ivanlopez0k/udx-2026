# Modelo de Academia (definido por el cliente)

Rama: `ModeloAcademia` · Migración: `AcademiaModeloCliente`

## Resumen

Antes cada `PerfilDocente` pertenecía a **una** academia (`PerfilDocente.AcademyId`, obligatorio) y la academia tenía un "creador" (`Academia.TeacherId`).
Ahora:

- Un docente puede ser miembro de **muchas** academias a través de la tabla `DocentesAcademias`.
- Las membresías **no tienen rangos**: no hay director/vicedirector. Cada academia tiene un único **responsable** (`Academia.ResponsableId`), que es quien la gestiona.
- Todo docente tiene siempre **una academia personal** (`IsPersonal = true`), que no se muestra y en la que nadie más puede entrar.
- Aparece el rol de usuario `ACADEMY`: una cuenta que representa a una academia oficial.
- Las academias y las membresías se dan de baja de forma **lógica** (`IsActive` / `LeftAt`).
- `SolicitudClase.AcademyId` y `ClaseAgendada.AcademyId` **no cambian**.

```
Usuario 1 ──── 0..1 PerfilDocente 1 ──── * DocenteAcademia * ──── 1 Academia
   │                                   (JoinedAt, IsActive, LeftAt)  │
   └────────────── 0..* (responsable, ResponsableId nullable) ───────┘
```

## Roles de usuario (`Usuario.Rol`)

Se guarda como `int`, por eso **los valores nuevos se agregan siempre al final**.

| Valor | Rol | Registro público | Qué crea `Register` |
|---|---|---|---|
| 0 | `PARENT` | Sí (valor por defecto) | `Hijo` + `PerfilPadre` |
| 1 | `TEACHER` | Sí | Academia personal + `PerfilDocente` + membresía activa en la personal (y en la oficial si trae código válido) |
| 2 | `ADMIN` | **No** (si viene, se registra como `PARENT`) | — |
| 3 | `ACADEMY` | Sí | Academia oficial con el usuario como responsable. No tiene `PerfilDocente` ni membresía |

## Entidades

### `DocenteAcademia` (tabla `DocentesAcademias`)

| Campo | Tipo C# | SQL | Null | Notas |
|---|---|---|---|---|
| Id | int | int identity | No | PK (hereda de `EntityBase`) |
| TeacherId | int | int | No | FK → `PerfilesDocentes.Id` |
| AcademyId | int | int | No | FK → `Academias.Id` |
| JoinedAt | DateTime | datetime2 | No | Fecha de ingreso (UTC) |
| IsActive | bool | bit (default 1) | No | `false` = salió o fue removido |
| LeftAt | DateTime? | datetime2 | Sí | Fecha de salida (UTC) |

- Índice único `(TeacherId, AcademyId)`: un docente no puede estar dos veces en la misma academia. Si vuelve, se **reactiva** la fila existente (`IsActive = true`, `LeftAt = null`).
- Navegaciones: `PerfilDocente.DocentesAcademias` y `Academia.Members` (Fluent API).

### `Academia` (tabla `Academias`)

| Campo | Tipo C# | SQL | Null | Notas |
|---|---|---|---|---|
| Id | int | int identity | No | |
| Name | string | nvarchar(max) | No | |
| Description | string | nvarchar(max) | No | |
| Code | string | nvarchar(450) | No | Único. Es el código con el que un docente se une |
| CreatedAt | DateTime | datetime2 | No | |
| IsPersonal | bool | bit (default 0) | No | **Nuevo**. Academia personal oculta del docente |
| IsActive | bool | bit (default 1) | No | **Nuevo** |
| Location | string? | nvarchar(max) | Sí | **Nuevo** |
| Phone | string? | nvarchar(max) | Sí | **Nuevo** |
| Email | string? | nvarchar(max) | Sí | **Nuevo** |
| PricePerHour | decimal? | decimal(18,2) | Sí | **Nuevo**. Precio general por hora |
| PricePerCourse | decimal? | decimal(18,2) | Sí | **Nuevo**. Precio general por curso |
| ResponsableId | int? | int | Sí | **Reemplaza a `TeacherId`** (renombrada, datos conservados). FK → `Usuarios.Id` |
| Members | `ICollection<DocenteAcademia>` | — | — | Navegación |

### `PerfilDocente`

- **Eliminados** `AcademyId` y `Academy`.
- **Nuevo** `ICollection<DocenteAcademia> DocentesAcademias`.

Todas las FKs siguen con `DeleteBehavior.NoAction` (loop en `AppDbContext.OnModelCreating`).

## Endpoints afectados

- **`AuthController.Register`**
  - Roles permitidos: `PARENT`, `TEACHER`, `ACADEMY`. `ADMIN` o un valor inválido → `PARENT`.
  - `TEACHER` con `AcademyCode`: se busca por `Code` sin distinguir mayúsculas y que **no** sea personal. Si no existe o es personal → `400 "Código de academia no válido."`. Si la academia estaba inactiva, se reactiva al entrar el docente.
  - `ACADEMY`: academia oficial con `Name = dto.Name`, `Email`, `Phone`, código generado y `ResponsableId` = el usuario nuevo.
- **`DocenteController.GetById`**: `academias` lista solo membresías activas en academias activas y no personales.
- **`DocenteController.EliminarCuenta`**: igual que antes: borra las membresías y la academia personal; en las oficiales donde era responsable deja `ResponsableId = null`. (La baja lógica de cuentas es otra tarjeta.)
- **`Seed`**: "Academia de Prueba" (`SEED0001`, oficial) con el profesor de prueba como responsable; el profesor tiene su personal y es miembro activo de ambas. Es idempotente y usa BCrypt.

## Migración `AcademiaModeloCliente` paso a paso

Se generó con `dotnet ef migrations add` y el `Up()` se reordenó a mano para no perder datos. No tiene IDs hardcodeados.

1. **Academias**: se agregan `IsPersonal` (default 0), `IsActive` (default 1), `Location`, `Phone`, `Email`, `PricePerHour`, `PricePerCourse`. `TeacherId` se **renombra** a `ResponsableId` (`sp_rename`, no drop + add), se hace nullable, y se recrean su índice (`IX_Academias_ResponsableId`) y su FK (`FK_Academias_Usuarios_ResponsableId`).
2. Se crea `DocentesAcademias` con sus FKs y el índice único.
3. Cada `PerfilDocente` pasa a ser miembro activo de la academia de su `AcademyId` actual (`JoinedAt = GETUTCDATE()`).
4. Se marcan como personales las academias existentes que cumplen el **criterio de personal** (ver abajo).
5. A cada docente que no es miembro de ninguna personal se le crea una (`"{Name} Academy"`, descripción vacía, código de 8 caracteres con `NEWID()`, responsable = el docente), salvo que ya tenga una personal propia sin miembros. Después se vincula a cada uno con esa personal.
6. Recién al final se borran la FK, el índice y la columna `PerfilesDocentes.AcademyId`.

`Down()` reconstruye el esquema anterior lo mejor posible; sus limitaciones están documentadas en el propio archivo (una sola academia por docente, academias sin responsable, usuarios `ACADEMY`, etc.).

### Criterio para detectar academias personales existentes

Una academia se considera personal si:

- su `Name` es `CONCAT(nombre del responsable, ' Academy')` (el formato que usaba `Register`),
- su `Description` está vacía o es `NULL`, y
- no tiene miembros cuyo perfil pertenezca a un usuario distinto del responsable.

Si un docente se unió con código a la academia de otro, esa academia queda como **oficial** (tiene un miembro ajeno) y su creador recibe una personal nueva en el paso 5.

## Reglas para los próximos endpoints

- **Gestión**: el responsable gestiona la academia: editar datos, remover docentes, regenerar el código y activarla/desactivarla.
- **Salir / remover**: `IsActive = false` + `LeftAt = now` en la membresía. No se borra nada.
- **Activación automática**: si una academia se queda sin miembros activos pasa sola a `IsActive = false`, y se reactiva cuando entra alguien.
- **Desactivación manual**: el responsable puede desactivarla aunque tenga miembros.
- **Personales**: nadie se une a una academia personal.
- **Inscripción de padres**: que un padre esté inscripto en una academia = tiene una solicitud de clase **ACEPTADA** en esa academia.
- **Precio**: profesor solo (academia personal) → `PerfilDocente.HourlyRate`; en una academia → los precios de la academia (`PricePerHour` / `PricePerCourse`).

## Pendientes (otras tarjetas)

- Precios personalizados por materia o por profesor dentro de la academia (si no hay uno personalizado, se usa el precio general).
- Baja lógica de cuentas (`EliminarCuenta`).
