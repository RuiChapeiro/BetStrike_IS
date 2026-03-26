-- Garantir que continuamos a trabalhar dentro da base de dados "Apostas"
USE Apostas;
GO

-- 1. Criar a tabela "Resultado"
CREATE TABLE Resultado (
    -- Como a relação é de 1-para-1 (cada jogo só tem um resultado),
    -- usamos o ID do Jogo como a nossa Chave Primária nesta tabela.
    JogoID INT PRIMARY KEY, 
    
    -- Golos: Números inteiros. Começam a zero por defeito se nada for indicado.
    GolosCasa INT NOT NULL DEFAULT 0,
    GolosFora INT NOT NULL DEFAULT 0,
    
    -- Data/Hora em que o resultado foi mexido pela última vez
    DataHoraAtualizacao DATETIME NOT NULL,
    
    -- CONSTRAINT (Restrição): Aqui criamos a "Chave Estrangeira" (Foreign Key).
    -- Isto diz ao SQL: "Este JogoID tem de existir na tabela Jogo".
    -- ON DELETE CASCADE: Significa que se o Jogo original for apagado, este resultado também apaga automaticamente para não ficarem "restos" na base de dados.
    CONSTRAINT FK_Resultado_Jogo FOREIGN KEY (JogoID) REFERENCES Jogo(ID) ON DELETE CASCADE
);
GO

-- 2. Criar a tabela "Aposta"
CREATE TABLE Aposta (
    -- ID único de cada aposta, gerado automaticamente
    ID INT IDENTITY(1,1) PRIMARY KEY,
    
    -- Liga a aposta ao jogo
    JogoID INT NOT NULL,
    
    -- Identifica quem fez a aposta
    UtilizadorID INT NOT NULL,
    
    -- CHAR(1) significa que só aceita um caractere.
    -- O CHECK garante que o sistema só aceita os valores '1', 'X' ou '2'.
    Tipo CHAR(1) NOT NULL CHECK (Tipo IN ('1', 'X', '2')),
    
    -- DECIMAL(18,2) permite guardar valores monetários com cêntimos (ex: 15.50).
    -- O CHECK garante que não se podem apostar valores negativos nem 0.
    Montante DECIMAL(18,2) NOT NULL CHECK (Montante > 0),
    
    -- Guarda a Odd no momento da aposta.
    -- Tem de ser obrigatoriamente superior a 1.0 (não faz sentido odds de retorno nulo).
    Odd DECIMAL(18,2) NOT NULL CHECK (Odd > 1.0),
    
    -- Estado: 1=Pendente, 2=Ganha, 3=Perdida, 4=Anulada
    Estado INT NOT NULL CHECK (Estado BETWEEN 1 AND 4),
    
    -- GETDATE() preenche automaticamente a data e hora do momento da aposta.
    DataHora DATETIME NOT NULL DEFAULT GETDATE(),
    
    -- Liga a aposta ao jogo de forma segura
    CONSTRAINT FK_Aposta_Jogo FOREIGN KEY (JogoID) REFERENCES Jogo(ID)
);
GO