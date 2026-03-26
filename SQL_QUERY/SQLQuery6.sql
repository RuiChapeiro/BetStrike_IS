USE Apostas;
GO

-- =========================================================
-- TRIGGER 1: Ao CRIAR aposta (Débito da Aposta - AP)
-- =========================================================
CREATE OR ALTER TRIGGER trg_Aposta_Insert
ON Aposta
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Inserir a transação do tipo 'AP' (Débito de Aposta) na BD de Pagamentos
    -- Os valores da aposta recém-criada vêm da tabela virtual especial chamada "inserted"
    INSERT INTO Pagamentos.dbo.Transacao (RefAposta, UtilizadorID, Tipo, Valor, DataHora, Estado)
    SELECT 
        i.ID, 
        i.UtilizadorID, 
        'AP', 
        i.Montante, 
        GETDATE(), 
        'Processada'
    FROM inserted i;

    -- 2. Atualizar (Subtrair) valor apostado no saldo do utilizador
    UPDATE s
    SET s.SaldoAtual = s.SaldoAtual - i.Montante,
        s.DataHoraAtualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador s
    INNER JOIN inserted i ON s.UtilizadorID = i.UtilizadorID;
END
GO

-- =========================================================
-- TRIGGER 2: Ao ATUALIZAR aposta (Ganhou - PG / Anulou - RE)
-- =========================================================
CREATE OR ALTER TRIGGER trg_Aposta_Update
ON Aposta
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- No SQL, quando se atualiza uma linha, os dados antigos ficam na tabela "deleted"
    -- e os dados novos ficam na "inserted". O trigger compara o "antes" e "depois".

    -- ========================================================
    -- A) APOSTAS GANHAS (Estado passou de 1 para 2) - Tipo PG
    -- ========================================================
    INSERT INTO Pagamentos.dbo.Transacao (RefAposta, UtilizadorID, Tipo, Valor, DataHora, Estado)
    SELECT 
        i.ID, 
        i.UtilizadorID, 
        'PG', 
        (i.Montante * i.Odd), -- Calcula o Prémio com base na Odd guardada
        GETDATE(), 
        'Processada'
    FROM inserted i
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 2; -- Assegura que só dispara se a transição foi de Pendente para Ganha

    -- Atualizar saldo (Adicionar o Prémio aos fundos do utilizador)
    UPDATE s
    SET s.SaldoAtual = s.SaldoAtual + (i.Montante * i.Odd),
        s.DataHoraAtualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador s
    INNER JOIN inserted i ON s.UtilizadorID = i.UtilizadorID
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 2;

    -- ========================================================
    -- B) APOSTAS ANULADAS (Estado passou de 1 para 4) - Tipo RE
    -- ========================================================
    INSERT INTO Pagamentos.dbo.Transacao (RefAposta, UtilizadorID, Tipo, Valor, DataHora, Estado)
    SELECT 
        i.ID, 
        i.UtilizadorID, 
        'RE', 
        i.Montante, -- É Devolvido exatamente o mesmo valor apostado
        GETDATE(), 
        'Processada'
    FROM inserted i
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 4;

    -- Atualizar saldo (Devolver o montante apostado)
    UPDATE s
    SET s.SaldoAtual = s.SaldoAtual + i.Montante,
        s.DataHoraAtualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador s
    INNER JOIN inserted i ON s.UtilizadorID = i.UtilizadorID
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 4;

END
GO