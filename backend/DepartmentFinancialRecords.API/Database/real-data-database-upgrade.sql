-- Run once against an existing database after legacy-schema-compatibility.sql.
-- Fresh databases created by the current API already include these objects.

CREATE TABLE IF NOT EXISTS Courses (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(120) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_Courses_Name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS YearLevels (
    Id INT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(80) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY IX_YearLevels_Name (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS DepartmentBills (
    Id INT NOT NULL AUTO_INCREMENT,
    Category VARCHAR(160) NOT NULL,
    Amount DECIMAL(65,30) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    IsActive TINYINT(1) NOT NULL,
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE Students
    ADD COLUMN CourseId INT NULL,
    ADD COLUMN YearLevelId INT NULL;

INSERT INTO Courses (Name)
SELECT DISTINCT TRIM(Course)
FROM Students
WHERE TRIM(COALESCE(Course, '')) <> ''
  AND NOT EXISTS (SELECT 1 FROM Courses WHERE Courses.Name = TRIM(Students.Course));

INSERT INTO YearLevels (Name)
SELECT DISTINCT TRIM(YearLevel)
FROM Students
WHERE TRIM(COALESCE(YearLevel, '')) <> ''
  AND NOT EXISTS (SELECT 1 FROM YearLevels WHERE YearLevels.Name = TRIM(Students.YearLevel));

UPDATE Students AS student
JOIN Courses AS course ON course.Name = TRIM(student.Course)
SET student.CourseId = course.Id
WHERE student.CourseId IS NULL;

UPDATE Students AS student
JOIN YearLevels AS yearLevel ON yearLevel.Name = TRIM(student.YearLevel)
SET student.YearLevelId = yearLevel.Id
WHERE student.YearLevelId IS NULL;

ALTER TABLE Students
    ADD CONSTRAINT FK_Students_Courses_CourseId
        FOREIGN KEY (CourseId) REFERENCES Courses (Id) ON DELETE RESTRICT,
    ADD CONSTRAINT FK_Students_YearLevels_YearLevelId
        FOREIGN KEY (YearLevelId) REFERENCES YearLevels (Id) ON DELETE RESTRICT;

ALTER TABLE Collectibles
    ADD COLUMN DepartmentBillId INT NULL,
    ADD CONSTRAINT FK_Collectibles_DepartmentBills_DepartmentBillId
        FOREIGN KEY (DepartmentBillId) REFERENCES DepartmentBills (Id) ON DELETE SET NULL;

ALTER TABLE AttendanceEvents
    ADD COLUMN SessionType LONGTEXT NOT NULL DEFAULT 'Log In',
    ADD COLUMN OpenAt DATETIME(6) NULL,
    ADD COLUMN LateAt DATETIME(6) NULL,
    ADD COLUMN CloseAt DATETIME(6) NULL,
    ADD COLUMN AbsentFine DECIMAL(65,30) NOT NULL DEFAULT 0,
    ADD COLUMN ClosedAt DATETIME(6) NULL,
    ADD COLUMN AbsentProcessed TINYINT(1) NOT NULL DEFAULT 0;