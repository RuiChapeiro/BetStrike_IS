-- 1. Cria a base de dados chamada "Apostas"
CREATE DATABASE Apostas;
GO

-- 2. Diz ao sistema para usar a base de dados que acabamos de criar
USE Apostas;
GO

-- 3. Cria a primeira tabela, a tabela "Jogo"
CREATE TABLE Jogo (
    -- ID: Número inteiro (INT). 
    -- IDENTITY(1,1) significa que o próprio SQL vai preencher isto automaticamente (1, 2, 3...)
    -- PRIMARY KEY significa que é o identificador principal da tabela.
    ID INT IDENTITY(1,1) PRIMARY KEY, 
    
    -- Codigo: Texto com até 15 caracteres. 
    -- NOT NULL significa que é obrigatório preencher. 
    -- UNIQUE garante que não podem existir dois jogos com o mesmo código (FUT-AAAA-JJNN).
    Codigo VARCHAR(15) NOT NULL UNIQUE, 
    
    -- DataHoraInicio: Guarda a data e a hora do jogo. É obrigatório (NOT NULL).
    DataHoraInicio DATETIME NOT NULL, 
    
    -- EquipaCasa e EquipaFora: Texto até 100 caracteres. Obrigatório.
    EquipaCasa VARCHAR(100) NOT NULL, 
    EquipaFora VARCHAR(100) NOT NULL, 
    
    -- Competicao: Texto até 100 caracteres. Obrigatório.
    Competicao VARCHAR(100) NOT NULL, 
    
    -- Estado: Número inteiro. 
    -- CHECK (Estado BETWEEN 1 AND 5) é uma regra que obriga a que o estado só possa ser 1, 2, 3, 4 ou 5.
    Estado INT NOT NULL CHECK (Estado BETWEEN 1 AND 5) 
);
GO