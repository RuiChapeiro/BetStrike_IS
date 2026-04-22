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
-- TRIGGER 2: Ao ATUALIZAR aposta
-- Apenas trata cancelamentos. O pagamento das ganhas é feito
-- pela stored procedure sp_ResolverApostas quando o jogo termina.
-- =========================================================
CREATE OR ALTER TRIGGER trg_Aposta_Update
ON Aposta
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Apostas anuladas (Estado passou de 1 para 4) - Tipo RE
    INSERT INTO Pagamentos.dbo.Transacao (RefAposta, UtilizadorID, Tipo, Valor, DataHora, Estado)
    SELECT 
        i.ID, 
        i.UtilizadorID, 
        'RE', 
        i.Montante,
        GETDATE(), 
        'Processada'
    FROM inserted i
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 4;

    UPDATE s
    SET s.SaldoAtual = s.SaldoAtual + i.Montante,
        s.DataHoraAtualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador s
    INNER JOIN inserted i ON s.UtilizadorID = i.UtilizadorID
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE d.Estado = 1 AND i.Estado = 4;
END
GO

-- =========================================================
-- STORED PROCEDURE: Resolver apostas quando o jogo termina
-- Estado da aposta:
--   1 = Pendente
--   2 = Ganha
--   4 = Cancelada
-- =========================================================
CREATE OR ALTER PROCEDURE sp_ResolverApostas
    @JogoID INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @GolosCasa INT,
            @GolosFora INT,
            @EstadoJogo INT;

    SELECT 
        @GolosCasa = ISNULL(r.GolosCasa, 0),
        @GolosFora = ISNULL(r.GolosFora, 0),
        @EstadoJogo = j.Estado
    FROM Jogo j
    LEFT JOIN Resultado r ON r.JogoID = j.ID
    WHERE j.ID = @JogoID;

    IF @EstadoJogo <> 3
        RETURN;

    DECLARE @Vencedor VARCHAR(1);

    SET @Vencedor = CASE 
        WHEN @GolosCasa > @GolosFora THEN '1'
        WHEN @GolosCasa < @GolosFora THEN '2'
        ELSE 'X'
    END;

    ;WITH ApostasParaResolver AS
    (
        SELECT 
            a.ID,
            a.UtilizadorID,
            a.Montante,
            a.Odd,
            a.Tipo
        FROM Aposta a
        WHERE a.JogoID = @JogoID
          AND a.Estado = 1
    )
    UPDATE a
    SET a.Estado = CASE WHEN a.Tipo = @Vencedor THEN 2 ELSE 3 END
    FROM Aposta a
    INNER JOIN ApostasParaResolver r ON r.ID = a.ID;

    -- Creditar apenas as apostas vencedoras
    INSERT INTO Pagamentos.dbo.Transacao (RefAposta, UtilizadorID, Tipo, Valor, DataHora, Estado)
    SELECT 
        a.ID,
        a.UtilizadorID,
        'PG',
        (a.Montante * a.Odd),
        GETDATE(),
        'Processada'
    FROM Aposta a
    WHERE a.JogoID = @JogoID
      AND a.Estado = 2;

    UPDATE s
    SET s.SaldoAtual = s.SaldoAtual + (a.Montante * a.Odd),
        s.DataHoraAtualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador s
    INNER JOIN Aposta a ON a.UtilizadorID = s.UtilizadorID
    WHERE a.JogoID = @JogoID
      AND a.Estado = 2;
END
GO