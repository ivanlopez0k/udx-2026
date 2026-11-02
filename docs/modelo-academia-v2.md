# Modelo de Academia v2: contratación por academia

Basado en la reunión con el cliente del 29/10/2026. Complementa a `docs/modelo-academia.md`: lo que no se menciona acá sigue igual.

> **Estado: propuesta para validar con el equipo.** Todavía no hay código ni migración. Los puntos marcados con ⚠️ afectan endpoints de otros compañeros.

## Qué definió el cliente

| Tema | Decisión |
|---|---|
| Contratación | El padre contrata a **la academia**, no a un docente. La academia asigna el docente y el padre **no elige**. Puede haber academias de un solo docente |
| Precio | Lo pone **la academia**: es el precio del servicio. Lo que la academia le pague al docente es un arreglo entre ellos, fuera de la app |
| Pagos | **Fuera de la app.** No se integra ninguna pasarela |
| Material | Lo ven el **docente** que lo subió, el **padre** (responsable del alumno) y el **responsable de la academia**. El admin queda para una versión futura |
| Fin del contrato | Si el padre deja la academia, se cierra el aula y **pierde el acceso** al material |
| Archivos | Sin un límite propio: cambiar lo menos posible mientras funcione |
| Dashboard | Versión futura, **no entra en la Expo** |

### Glosario: dos "responsables"

- **Responsable del alumno** = el **padre** (`PerfilPadre`).
- **Responsable de la academia** = el usuario que la gestiona (`Academia.ResponsableId`), algo así como un director sin el título. Puede ser una cuenta `ACADEMY` o, en una academia personal, el propio docente.

## Nuevo flujo de la solicitud

```
Padre                         Responsable de la academia
  │ POST /api/SolicitudClase       │
  │ (academia + materia, SIN docente)
  │ ─────────────── PENDING ─────▶ │
  │                                │ PUT /{id}/estado
  │                                │  · ACCEPTED + DocenteId  → se crea ClaseAgendada
  │                                │  · DECLINED
  │ ◀──────── ACCEPTED ─────────── │
  │                                │
  │ PUT /{id}/finalizar   ó   PUT /{id}/finalizar
  └────────────── FINISHED ────────┘   (el padre pierde el acceso al material)
```

- El **docente no acepta ni rechaza**: lo asigna el responsable y la solicitud queda aceptada en un solo paso.
- **Academia personal (profesor solo):** el responsable es el propio docente, así que se asigna a sí mismo. No hace falta un caso especial.

## Cambios en las entidades

### `SolicitudClase`

| Campo | Antes | Después | Por qué |
|---|---|---|---|
| `DocenteId` | `int`, obligatorio | **`int?`, nullable** | Mientras está `PENDING` todavía no hay docente asignado |
| `Docente` | `PerfilDocente` | **`PerfilDocente?`** | Lo mismo |
| `Stat` | `PENDING, ACCEPTED, DECLINED, EXPIRED` | **+ `FINISHED`** al final (valor 4) | Para cerrar el contrato. Se agrega al final porque se guarda como `int` |

Regla: si `Status == ACCEPTED`, entonces `DocenteId != null`. La valida el endpoint.

### `Academia`

**No cambia.** `PricePerHour` y `PricePerCourse` pasan a ser **el** precio del servicio.

### `PerfilDocente.HourlyRate` y `DocenteMateria.HourlyRate`

**Se dejan en la base pero no se usan más** para mostrar ni calcular precios. Así no hace falta una migración y no se rompe el front. Se pueden borrar en una versión futura.

### `ClaseAgendada`

**No cambia.** Hoy se crea con `Price = 0`. Propuesta: tomar `Academia.PricePerHour × Duration / 60` cuando la academia tenga precio por hora, y si no, dejar 0.

### `PagoDocente`

**No se usa en la Expo**, porque los pagos van por fuera. Se deja como está.

### Migración necesaria

Una sola, chica: `SolicitudDocenteOpcional`, que hace nullable `SolicitudesClases.DocenteId`. Agregar `FINISHED` **no** necesita migración (es un valor nuevo del enum `int`).

## Endpoints afectados

### ⚠️ `SolicitudClaseController` (lo hizo otro compañero)

**`POST /api/SolicitudClase`** (PARENT)
- `CrearSolicitudDTO`: se **saca `DocenteId`**.
- Validar que la academia exista, esté activa y que **al menos un docente activo** de la academia dicte esa materia. Si no hay ninguno → 400.

**`PUT /api/SolicitudClase/{id}/estado`**
- Antes: solo el docente asignado (`TEACHER`).
- Ahora: solo el **responsable de esa academia** (`Academy.ResponsableId == userId`, sea `ACADEMY` o `TEACHER`).
- Body: `{ "status": "ACCEPTED" | "DECLINED", "docenteId": 5 }`. `docenteId` es obligatorio si es `ACCEPTED`.
- Validar que el docente sea **miembro activo** de la academia y **dicte la materia** de la solicitud.
- Si es `ACCEPTED`, asignar `DocenteId` y crear la `ClaseAgendada`, igual que hoy.
- De paso, corregir `parentName` y `docenteName`, que hoy vuelven vacíos porque falta `.ThenInclude(x => x.User)`.

**`PUT /api/SolicitudClase/{id}/finalizar`** (nuevo)
- Lo pueden usar el **padre** de la solicitud (`Parent.UserId == userId`) o el **responsable de la academia**.
- Solo desde `ACCEPTED` → `FINISHED`.
- Propuesta: las `ClasesAgendadas` futuras en `SCHEDULED` de esa solicitud pasan a `CANCELED`.

**`GET /api/SolicitudClase/docente/{docenteId}`**: sigue igual (el docente ve las que le asignaron).

**Nuevo, sugerido:** `GET /api/SolicitudClase/academia/{academyId}`, para que el responsable vea las pendientes y pueda asignarlas.

### `ContenidoController` (mío)

**No cambia el código.** La regla de acceso ya pide `Status == ACCEPTED`, así que cuando la solicitud pasa a `FINISHED` el padre pierde el acceso solo. Docente, responsable de la academia y padre ya están contemplados.

Sobre el admin: hoy tiene acceso total. Como el registro público no permite crear admins, no molesta. Se deja así hasta la versión futura.

### ⚠️ `DocenteController`

- **Búsqueda** (`GET /api/Docente/buscar`): los filtros `minPrecio`/`maxPrecio` y el orden por `precio` usan `PerfilDocente.HourlyRate`, que deja de ser el precio real. Propuesta: sacar el filtro y el orden por precio de la búsqueda de docentes, porque el precio se ve en la academia.
- **Detalle** (`GET /api/Docente/{id}`): no cambia. Ya lista las academias del docente.
- `PrecioPorHora` en el perfil: deja de mostrarse en el front.

### `AcademiaController`

- `GET /api/Academia/{id}`: mostrar `PricePerHour`, `PricePerCourse` y los docentes activos. Es lo que el padre ve antes de contratar.
- **Nuevo** `PUT /api/Academia/{id}/precios` (solo el responsable): `{ "pricePerHour": 5000, "pricePerCourse": 40000 }`. Corresponde a la tarjeta *"precios personalizados en la academia"*.

### `Seed`

- "Academia de Prueba" con `PricePerHour` y `PricePerCourse` cargados.
- Una solicitud `PENDING` sin docente y otra `ACCEPTED` con el profesor de prueba, para poder probar los dos casos.

### Subida de archivos

Se deja el límite por defecto del servidor (~30 MB por request), que es el menor cambio posible. Si un archivo lo supera, se sube el límite en `Program.cs`.

## Fuera de la Expo

- Dashboard.
- Rol admin (acceso y gestión).
- Pagos dentro de la app (`PagoDocente`).
- Borrar `HourlyRate` de `PerfilDocente` y `DocenteMateria`.

## Para decidir con el equipo

1. ¿Quién adapta `SolicitudClaseController`: el que lo hizo o yo? Es el cambio más grande.
2. ¿Sacamos el filtro por precio de la búsqueda de docentes o lo cambiamos por el precio de la academia?
3. ¿Al finalizar se cancelan las clases futuras?
4. ¿El precio de `ClaseAgendada` se calcula con el de la academia o queda en 0?
5. El front (Matías): el padre ya no elige docente; elige academia y materia.
