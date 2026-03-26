USE Apostas;
GO

-- =========================================================
-- 4. SP: Inserir Aposta (ALTER para substituir e com correção de Coluna)
-- =========================================================
-- Se por acaso a procedure sp_InserirAposta nunca chegou a ser criada por causa do erro, 
-- corre primeiro só este Drop:
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_InserirAposta')
    DROP PROCEDURE sp_InserirAposta
GO

CREATE PROCEDURE sp_InserirAposta
    @JogoID INT,
    @UtilizadorID INT,
    @Tipo CHAR(1),
    @Montante DECIMAL(18,2),
    @Odd DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @EstadoJogo INT;
    SELECT @EstadoJogo = Estado FROM Jogo WHERE ID = @JogoID;
    
    -- O estado do jogo não pode ser Finalizado (3), Cancelado (4) ou Adiado (5)
    IF @EstadoJogo IN (3, 4, 5)
    BEGIN
        RAISERROR('Não é permitido registar apostas neste jogo, pois já terminou, foi cancelado ou adiado.', 16, 1);
        RETURN;
    END

    -- Validar Tipo
    IF @Tipo NOT IN ('1', 'X', '2')
    BEGIN
        RAISERROR('Tipo de aposta inválido. Use 1, X ou 2.', 16, 1);
        RETURN;
    END

    -- Validar o Montante
    IF @Montante <= 0
    BEGIN
        RAISERROR('O valor apostado deve ser maior que zero.', 16, 1);
        RETURN;
    END

    -- Validar Odd
    IF @Odd <= 1.0
    BEGIN
        RAISERROR('A odd tem de ser maior que 1.0.', 16, 1);
        RETURN;
    END

    -- Validação do Saldo: Como estão no mesmo servidor, podemos consultar a outra BD diretamente
    DECLARE @SaldoAtual DECIMAL(18,2);
    -- ATENÇÂO: Estou a assumir que a coluna na DB Pagamentos se chama UtilizadorID. Se não se chamar, muda aqui abaixo o 'UtilizadorID=@UtilizadorID' para o que for!
    SELECT @SaldoAtual = SaldoAtual FROM Pagamentos.dbo.Saldo_Utilizador WHERE UtilizadorID = @UtilizadorID;
    
    IF @SaldoAtual IS NULL OR @SaldoAtual < @Montante
    BEGIN
         RAISERROR('Saldo insuficiente para realizar a aposta.', 16, 1);
         RETURN;
    END

    -- Se passar tudo, insere (Estado 1 = Pendente)
    INSERT INTO Aposta (JogoID, UtilizadorID, Tipo, Montante, Odd, Estado)
    VALUES (@JogoID, @UtilizadorID, @Tipo, @Montante, @Odd, 1);
END
GO

-- =========================================================
-- 5. SP: Cancelar Aposta
-- =========================================================
ALTER PROCEDURE sp_CancelarAposta
    @ApostaID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @JogoID INT;
    DECLARE @EstadoAposta INT;
    DECLARE @EstadoJogo INT;

    SELECT @JogoID = JogoID, @EstadoAposta = Estado FROM Aposta WHERE ID = @ApostaID;
    SELECT @EstadoJogo = Estado FROM Jogo WHERE ID = @JogoID;

    IF @EstadoAposta <> 1
    BEGIN
        RAISERROR('Só pode cancelar apostas pendentes.', 16, 1);
        RETURN;
    END

    IF @EstadoJogo <> 1
    BEGIN
        RAISERROR('Só pode cancelar a aposta enquanto o jogo ainda estiver Agendado.', 16, 1);
        RETURN;
    END

    -- Muda o estado da aposta para Anulada (4). O Trigger tratará da devolução do dinheiro.
    UPDATE Aposta SET Estado = 4 WHERE ID = @ApostaID;
END
GO

-- =========================================================
-- 6. SP: Resolver Apostas (Chamada ao finalizar um jogo)
-- =========================================================
ALTER PROCEDURE sp_ResolverApostas
    @JogoID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @GolosCasa INT = 0;
    DECLARE @GolosFora INT = 0;
    
    SELECT @GolosCasa = GolosCasa, @GolosFora = GolosFora FROM Resultado WHERE JogoID = @JogoID;
    
    DECLARE @Vencedor CHAR(1);
    
    IF @GolosCasa > @GolosFora SET @Vencedor = '1';
    ELSE IF @GolosCasa = @GolosFora SET @Vencedor = 'X';
    ELSE SET @Vencedor = '2';
    
    -- Atualiza as apostas ganhas
    UPDATE Aposta 
    SET Estado = 2 
    WHERE JogoID = @JogoID AND Estado = 1 AND Tipo = @Vencedor;
    
    -- Atualiza as apostas perdidas
    UPDATE Aposta 
    SET Estado = 3 
    WHERE JogoID = @JogoID AND Estado = 1 AND Tipo <> @Vencedor;
END
GO

-- =========================================================
-- 7. SPs de Consultas Básicas (Exemplos)
-- =========================================================
ALTER PROCEDURE sp_ConsultarJogos
    @DataInicio DATETIME = NULL,
    @Estado INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Jogo
    WHERE (@DataInicio IS NULL OR CAST(DataHoraInicio AS DATE) = CAST(@DataInicio AS DATE))
      AND (@Estado IS NULL OR Estado = @Estado);
END
GO

ALTER PROCEDURE sp_ConsultarApostasUtilizador
    @UtilizadorID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Aposta WHERE UtilizadorID = @UtilizadorID;
END
GO