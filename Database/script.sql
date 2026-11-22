PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Tutores (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    CPF TEXT NOT NULL,
    Telefone TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Especies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Racas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    EspecieId INTEGER NOT NULL,
    FOREIGN KEY (EspecieId) REFERENCES Especies(Id)
);

CREATE TABLE IF NOT EXISTS Pets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    DataNascimento TEXT NOT NULL,
    Peso REAL NOT NULL,
    Sexo INTEGER NOT NULL,
    TutorId INTEGER NOT NULL,
    EspecieId INTEGER NOT NULL,
    RacaId INTEGER NOT NULL,
    FOREIGN KEY (TutorId) REFERENCES Tutores(Id),
    FOREIGN KEY (EspecieId) REFERENCES Especies(Id),
    FOREIGN KEY (RacaId) REFERENCES Racas(Id)
);

CREATE TABLE IF NOT EXISTS Funcionarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Tipo TEXT NOT NULL,
    Nome TEXT NOT NULL,
    Cpf TEXT NOT NULL,
    Email TEXT NOT NULL,
    Telefone TEXT NOT NULL,
    Endereco TEXT NOT NULL,
    Salario REAL NOT NULL,
    NivelAcesso INTEGER NOT NULL,
    Turno TEXT NULL,
    Crmv TEXT NULL,
    Especialidade TEXT NULL,
    Ativo INTEGER NOT NULL DEFAULT 1,
    CHECK (Tipo IN ('Recepcionista', 'Veterinario', 'Administrador')),
    CHECK (Ativo IN (0, 1)),
    CHECK (
        (Tipo = 'Recepcionista' AND NivelAcesso = 1) OR
        (Tipo = 'Veterinario' AND NivelAcesso = 2) OR
        (Tipo = 'Administrador' AND NivelAcesso = 3)
    ),
    CHECK (
        (Tipo = 'Recepcionista' AND Turno IS NOT NULL AND Crmv IS NULL AND Especialidade IS NULL) OR
        (Tipo = 'Veterinario' AND Turno IS NULL AND Crmv IS NOT NULL AND Especialidade IS NOT NULL) OR
        (Tipo = 'Administrador' AND Turno IS NULL AND Crmv IS NULL AND Especialidade IS NULL)
    )
);

-- INICIO DDL CONSULTAS
CREATE TABLE Consultas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PetId INTEGER NOT NULL,
    VeterinarioId INTEGER NOT NULL,
    DataHora TEXT NOT NULL,
    Status INTEGER NOT NULL CHECK (Status IN (0, 1, 2)),
    QueixaInicial TEXT NOT NULL,
    Urgencia INTEGER NOT NULL CHECK (Urgencia IN (0, 1)),
    Diagnostico TEXT NULL,
    Procedimentos TEXT NULL,
    Prescricao TEXT NULL,
    FOREIGN KEY (PetId) REFERENCES Pets(Id),
    FOREIGN KEY (VeterinarioId) REFERENCES Funcionarios(Id)
);

CREATE UNIQUE INDEX UX_Consultas_Veterinario_DataHora_Agendada
ON Consultas (VeterinarioId, DataHora)
WHERE Status = 0;
-- FIM DDL CONSULTAS

INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 1, 'Ana Ribeiro', '12345678901', '(16) 99111-0001'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 1);
INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 2, 'Bruno Almeida', '23456789012', '(16) 99111-0002'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 2);
INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 3, 'Carla Mendes', '34567890123', '(16) 99111-0003'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 3);
INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 4, 'Diego Ferreira', '45678901234', '(16) 99111-0004'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 4);
INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 5, 'Elisa Costa', '56789012345', '(16) 99111-0005'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 5);
INSERT INTO Tutores (Id, Nome, CPF, Telefone)
SELECT 6, 'Felipe Rocha', '67890123456', '(16) 99111-0006'
WHERE NOT EXISTS (SELECT 1 FROM Tutores WHERE Id = 6);

INSERT INTO Especies (Id, Nome)
SELECT 1, 'Cachorro'
WHERE NOT EXISTS (SELECT 1 FROM Especies WHERE Id = 1);
INSERT INTO Especies (Id, Nome)
SELECT 2, 'Gato'
WHERE NOT EXISTS (SELECT 1 FROM Especies WHERE Id = 2);
INSERT INTO Especies (Id, Nome)
SELECT 3, 'Ave'
WHERE NOT EXISTS (SELECT 1 FROM Especies WHERE Id = 3);
INSERT INTO Especies (Id, Nome)
SELECT 4, 'Coelho'
WHERE NOT EXISTS (SELECT 1 FROM Especies WHERE Id = 4);
INSERT INTO Especies (Id, Nome)
SELECT 5, 'Roedor'
WHERE NOT EXISTS (SELECT 1 FROM Especies WHERE Id = 5);

INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 1, 'Labrador Retriever', 1
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 1);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 2, 'Poodle', 1
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 2);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 3, 'Siamês', 2
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 3);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 4, 'Persa', 2
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 4);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 5, 'Calopsita', 3
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 5);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 6, 'Mini Lop', 4
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 6);
INSERT INTO Racas (Id, Nome, EspecieId)
SELECT 7, 'Hamster Sírio', 5
WHERE NOT EXISTS (SELECT 1 FROM Racas WHERE Id = 7);

INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 1, 'Thor', '2020-03-15', 28.5, 1, 1, 1, 1
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 1);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 2, 'Mel', '2021-07-10', 6.2, 2, 1, 1, 2
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 2);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 3, 'Luna', '2019-11-02', 4.1, 2, 2, 2, 3
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 3);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 4, 'Simba', '2018-05-20', 5.0, 1, 3, 2, 4
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 4);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 5, 'Pipoca', '2022-01-12', 0.09, 2, 4, 3, 5
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 5);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 6, 'Bolinha', '2021-09-30', 1.8, 1, 4, 4, 6
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 6);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 7, 'Nina', '2023-02-18', 0.12, 2, 5, 5, 7
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 7);
INSERT INTO Pets (Id, Nome, DataNascimento, Peso, Sexo, TutorId, EspecieId, RacaId)
SELECT 8, 'Rex', '2017-08-05', 30.0, 1, 6, 1, 1
WHERE NOT EXISTS (SELECT 1 FROM Pets WHERE Id = 8);

INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 1, 'Recepcionista', 'Marina Souza', '11122233344', 'marina.souza@vetcare.test', '(16) 99222-0001', 'Rua das Acacias, 100', 2800.0, 1, 'Manha', NULL, NULL, 1
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 1);
INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 2, 'Recepcionista', 'Paulo Lima', '22233344455', 'paulo.lima@vetcare.test', '(16) 99222-0002', 'Rua das Palmeiras, 200', 2700.0, 1, 'Tarde', NULL, NULL, 1
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 2);
INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 3, 'Veterinario', 'Renata Alves', '33344455566', 'renata.alves@vetcare.test', '(16) 99222-0003', 'Avenida Central, 300', 6500.0, 2, NULL, 'CRMV-SP 12345', 'Clinica Geral', 1
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 3);
INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 4, 'Veterinario', 'Carlos Nogueira', '44455566677', 'carlos.nogueira@vetcare.test', '(16) 99222-0004', 'Rua do Comercio, 400', 7200.0, 2, NULL, 'CRMV-SP 67890', 'Dermatologia', 1
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 4);
INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 5, 'Administrador', 'Juliana Prado', '55566677788', 'juliana.prado@vetcare.test', '(16) 99222-0005', 'Rua das Flores, 500', 8000.0, 3, NULL, NULL, NULL, 1
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 5);
INSERT INTO Funcionarios (Id, Tipo, Nome, Cpf, Email, Telefone, Endereco, Salario, NivelAcesso, Turno, Crmv, Especialidade, Ativo)
SELECT 6, 'Administrador', 'Ricardo Teixeira', '66677788899', 'ricardo.teixeira@vetcare.test', '(16) 99222-0006', 'Avenida Brasil, 600', 7800.0, 3, NULL, NULL, NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM Funcionarios WHERE Id = 6);

INSERT INTO Consultas (PetId, VeterinarioId, DataHora, Status, QueixaInicial, Urgencia,
                       Diagnostico, Procedimentos, Prescricao)
VALUES
    (1, 3, '2026-11-10 09:00:00', 2, 'Avaliação de rotina.', 0,
     'Sem alterações no exame clínico.', 'Exame físico e pesagem.', NULL),
    (3, 4, '2026-11-12 14:30:00', 2, 'Coceira recorrente.', 0,
     'Irritação cutânea leve.', 'Exame dermatológico.', 'Reavaliação em sete dias.'),
    (2, 3, '2026-11-18 10:00:00', 1, 'Retorno para revisão.', 0, NULL, NULL, NULL),
    (4, 3, '2026-12-01 08:30:00', 0, 'Diminuição do apetite.', 1, NULL, NULL, NULL),
    (5, 3, '2026-12-01 09:00:00', 0, 'Avaliação de rotina.', 0, NULL, NULL, NULL),
    (8, 4, '2026-12-01 14:30:00', 0, 'Descamação na pele.', 0, NULL, NULL, NULL);
