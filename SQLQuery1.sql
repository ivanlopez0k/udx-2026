-- 1. Crear un usuario docente
INSERT INTO Usuarios (Email, Password, Role, Name, Phone, CreatedAt)
VALUES ('docente.prueba@test.com', '123456', 1, 'Juan Perez', '3511234567', GETDATE());

-- 2. Buscar el Id que le asignó (para usarlo abajo)
DECLARE @UserId INT = (SELECT Id FROM Usuarios WHERE Email = 'docente.prueba@test.com');

-- 3. Crear el perfil de docente asociado a ese usuario
INSERT INTO PerfilesDocentes (Credentials, HourlyRate, Location, RatingAvg, RatinCount, Availability, Verified, UserId, AcademyId)
VALUES ('Profesor de Matemática, 10 años de experiencia', 2000, 'Cordoba', 0, 0, 'Lunes a Viernes', 1, @UserId, NULL);

-- 4. Buscar el Id del perfil docente recién creado
DECLARE @TeacherId INT = (SELECT Id FROM PerfilesDocentes WHERE UserId = @UserId);

-- 5. Crear la materia
INSERT INTO Materias (Name, Grade, Category)
VALUES ('Matematica', 'Secundario', 'Ciencias Exactas');

-- 6. Buscar el Id de la materia
DECLARE @MateriaId INT = (SELECT Id FROM Materias WHERE Name = 'Matematica');

-- 7. Conectar al docente con la materia
INSERT INTO DocentesMaterias (Level, TeacherId, MateriaId)
VALUES ('General', @TeacherId, @MateriaId);