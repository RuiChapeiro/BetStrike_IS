-- 1. Cria a base de dados "Pagamentos"
CREATE DATABASE Pagamentos;
GO

-- 2. Passamos a usar a nova base de dados "Pagamentos"
USE Pagamentos;
GO

-- 3. Criar a tabela "Saldo_Utilizador"
CREATE TABLE Saldo_Utilizador (
    -- ID do Utilizador não tem IDENTITY porque vem diretamente da criação na plataforma.
    -- Tem de ser a chave principal (único).
    UtilizadorID INT PRIMARY KEY,
    
    -- SaldoAtual: Dinheiro na conta. 
    -- DEFAULT 0.00 como segurança inicial, mas será inserido 50.00 via código depois.
    -- CHECK (SaldoAtual >= 0): REGRA MUITO IMPORTANTE! "O saldo nunca pode ser negativo". 
    SaldoAtual DECIMAL(18,2) NOT NULL DEFAULT 0.00 CHECK (SaldoAtual >= 0),
    
    -- Quando foi a última mexida no saldo
    DataHoraAtualizacao DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- 4. Criar a tabela "Transacao"
CREATE TABLE Transacao (
    -- ID único e automático da transação
    ID INT IDENTITY(1,1) PRIMARY KEY,
    
    -- RefAposta pode ser NULL porque um depósito (DE) não tem aposta associada.
    RefAposta INT NULL,
    
    -- É obrigatório saber de quem foi o dinheiro movimentado
    UtilizadorID INT NOT NULL,
    
    -- Tipo da Transação
    -- AP (Débito Aposta), PG (Pagamento Prémio), RE (Reembolso), DE (Depósito), LV (Levantamento)
    Tipo CHAR(2) NOT NULL CHECK (Tipo IN ('AP', 'PG', 'RE', 'DE', 'LV')),
    
    -- Valor envolvido
    Valor DECIMAL(18,2) NOT NULL CHECK (Valor > 0),
    
    -- Data e hora em que ocorreu
    DataHora DATETIME NOT NULL DEFAULT GETDATE(),
    
    -- Estado da transação
    -- Pendente, Processada, Falhada, Reembolsada
    Estado VARCHAR(20) NOT NULL CHECK (Estado IN ('Pendente', 'Processada', 'Falhada', 'Reembolsada')),
    
    -- Chave estrangeira ligando ao UtilizadorID da tabela Saldo_Utilizador
    CONSTRAINT FK_Transacao_Utilizador FOREIGN KEY (UtilizadorID) REFERENCES Saldo_Utilizador(UtilizadorID)
);
GO