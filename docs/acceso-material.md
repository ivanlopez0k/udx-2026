# Acceso a material (Contenido)

## Dónde se guardan los archivos

- Los archivos subidos se guardan **fuera de `wwwroot`**, en `Storage/contenidos` dentro del content root del server
  (`Path.Combine(env.ContentRootPath, "Storage", "contenidos")`). La carpeta se crea sola en la primera subida.
- El nombre en disco es `Guid + extensión` (ej. `3f2c...e1.pdf`).
- `Contenido.FilePath` guarda **solo el nombre del archivo**, no una URL pública. No hay forma de acceder al archivo
  sin pasar por `GET /api/Contenido/{id}/archivo`, que valida permisos.
- `UDX.Server/UDX.Server/Storage/` está en `.gitignore`.

> Material viejo: las filas creadas antes de este cambio tienen `FilePath = "/uploads/contenidos/..."` (en `wwwroot`)
> y `AcademyId = null`. El endpoint de descarga busca el archivo en `Storage/contenidos`, así que para esas filas
> devuelve 404 salvo que se mueva el archivo a esa carpeta. Además, al no tener academia, solo las ven el docente que
> las subió y ADMIN.

## Regla de acceso

Un usuario puede ver/descargar un contenido si se cumple **alguna** de estas condiciones:

1. Es **ADMIN**.
2. Es el **docente que lo subió** (`Contenido.TeacherId == userId`).
3. Es el **responsable de la academia** del contenido (`Academia.ResponsableId == userId`).
4. Es el **padre del alumno** (`Contenido.ParentId == userId`) **y** existe una `SolicitudClase` con
   `Status = ACCEPTED` de su `PerfilPadre` con el `PerfilDocente` del docente que lo subió, en la misma academia
   (`SolicitudClase.AcademyId == Contenido.AcademyId`).

Si no cumple ninguna → **403**. No hay vista previa: solo metadatos y descarga.

La regla está implementada una sola vez en `ContenidoController` (`FiltrarAccesibles`) y se reutiliza tanto para el
listado como para `PuedeAcceder` (detalle y descarga).

## Endpoints

Todos requieren estar logueado (`[Authorize]`). El POST además requiere rol `TEACHER`.

### `POST /api/Contenido` (TEACHER) — subir material

`multipart/form-data`:

| Campo       | Tipo   | Obligatorio |
|-------------|--------|-------------|
| `Title`     | string | sí          |
| `Archivo`   | file   | sí          |
| `MateriaId` | int    | sí          |
| `HijoId`    | int    | sí          |
| `AcademyId` | int    | **sí (nuevo)** |

Validaciones, en este orden y todas **antes** de guardar el archivo:

| # | Validación | Respuesta si falla |
|---|------------|--------------------|
| a | Existe el perfil docente del usuario | 404 `No se encontró el perfil docente.` |
| b | El docente da esa materia | 400 `El docente no está asociado a esa materia.` |
| c | El docente es miembro **activo** de esa academia (`DocenteAcademia.IsActive`) | 400 `No sos miembro activo de esa academia.` |
| d | El hijo existe | 404 `No se encontró el alumno.` |
| e | El padre del hijo tiene una `SolicitudClase` ACCEPTED con ese docente en esa academia | 400 `El alumno no está inscripto con vos en esa academia.` |
| f | Llegó un archivo no vacío | 400 `No se recibió ningún archivo.` |

Si falta `AcademyId` (o es 0) se responde 400 por validación del modelo.

Respuesta OK: `200` sin cuerpo.

### `GET /api/Contenido` — listar material accesible

Query opcionales: `materiaId`, `hijoId`, `academyId`.

Devuelve lo que el usuario puede ver según la regla de acceso, ordenado por fecha (más nuevo primero):

- **PARENT**: material de sus hijos que cumpla la regla (inscripción ACCEPTED con el docente en esa academia).
- **TEACHER**: lo que subió, y si es responsable de alguna academia, también el material de esa academia.
- **ACADEMY**: el material de las academias donde es responsable.
- **ADMIN**: todo.

Respuesta `200`: `ContenidoResponseDTO[]`.

### `GET /api/Contenido/{id}` — detalle

- `200`: `ContenidoResponseDTO`
- `404`: no existe el material
- `403`: no tiene acceso

### `GET /api/Contenido/{id}/archivo` — descargar

- `200`: el archivo, con `Content-Type` según la extensión (`FileExtensionContentTypeProvider`, por defecto
  `application/octet-stream`) y nombre de descarga `Title + extensión`.
- `404`: no existe el registro o no existe el archivo en disco
- `403`: no tiene acceso

Como requiere el token JWT, el front tiene que descargarlo con `HttpClient` (con el header `Authorization`) y no con un
`<a href>` directo.

### `ContenidoResponseDTO`

```json
{
  "id": 12,
  "title": "Guía de fracciones",
  "createdAt": "2026-10-21T18:30:00Z",
  "materiaId": 3,
  "materiaName": "Matemática",
  "hijoId": 7,
  "hijoName": "Juan",
  "academyId": 2,
  "academyName": "Academia Norte",
  "teacherName": "Laura Pérez",
  "downloadUrl": "/api/Contenido/12/archivo"
}
```

Nunca se expone la ruta física del archivo.

## Cambios necesarios en el front

- El formulario de carga de material **tiene que mandar `AcademyId`** en el `multipart/form-data`. Sin eso el POST
  responde 400.
- Conviene que el front ofrezca solo las academias donde el docente es miembro activo y, dentro de ellas, solo los
  alumnos inscriptos (ACCEPTED) con él, para no chocar con las validaciones c y e.
- Para mostrar/descargar material usar los nuevos `GET` en lugar de armar URLs a `/uploads/contenidos/...`.
