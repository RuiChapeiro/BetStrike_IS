USE Apostas;
GO

-- =========================================================
-- 1. SP: Inserir Jogo
-- =========================================================
CREATE PROCEDURE sp_InserirJogo
    @Codigo VARCHAR(15),
    @DataHoraInicio DATETIME,
    @EquipaCasa VARCHAR(100),
    @EquipaFora VARCHAR(100),
    @Competicao VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Validação: Garantir que o formato é FUT-AAAA-JJNN
    IF @Codigo NOT LIKE 'FUT-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9]'
    BEGIN
        RAISERROR('Formato de código inválido. O formato deve ser FUT-AAAA-JJNN.', 16, 1);
        RETURN;
    END

    -- Validação: Garantir que o código não existe (Unicidade)
    IF EXISTS (SELECT 1 FROM Jogo WHERE Codigo = @Codigo)
    BEGIN
        RAISERROR('Já existe um jogo registado com esse código.', 16, 1);
        RETURN;
    END

    -- Se passou as validações, insere o Jogo com Estado = 1 (Agendado)
    INSERT INTO Jogo (Codigo, DataHoraInicio, EquipaCasa, EquipaFora, Competicao, Estado)
    VALUES (@Codigo, @DataHoraInicio, @EquipaCasa, @EquipaFora, @Competicao, 1);

    -- Devolve o ID do jogo acabado de criar
    SELECT SCOPE_IDENTITY() AS NovoJogoID;
END
GO


-- =========================================================
-- 2. SP: Inserir Resultado de um Jogo
-- =========================================================
CREATE PROCEDURE sp_InserirResultado
    @JogoID INT,
    @GolosCasa INT,
    @GolosFora INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EstadoAtual INT;

    SELECT @EstadoAtual = Estado FROM Jogo WHERE ID = @JogoID;

    -- Só se pode inserir resultado se o jogo já estiver no estado 3 (Finalizado)
    IF @EstadoAtual <> 3
    BEGIN
        RAISERROR('Só pode inserir o resultado num jogo que esteja Finalizado.', 16, 1);
        RETURN;
    END

    -- Valida se o jogo já tem um resultado associado
    IF EXISTS (SELECT 1 FROM Resultado WHERE JogoID = @JogoID)
    BEGIN
        RAISERROR('Este jogo já tem um resultado registado.', 16, 1);
        RETURN;
    END

    INSERT INTO Resultado (JogoID, GolosCasa, GolosFora, DataHoraAtualizacao)
    VALUES (@JogoID, @GolosCasa, @GolosFora, GETDATE());
END
GO


-- =========================================================
-- 3. SP: Atualizar Estado do Jogo (e gerar Resultado Fantasma se Finalizado)
-- =========================================================
CREATE PROCEDURE sp_AtualizarEstadoJogo
    @JogoID INT,
    @NovoEstado INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EstadoAtual INT;

    SELECT @EstadoAtual = Estado FROM Jogo WHERE ID = @JogoID;

    -- Validações de Transição de Estado pedidas no enunciado
    IF @EstadoAtual = 3 AND @NovoEstado = 2
    BEGIN
        RAISERROR('Um jogo Finalizado não pode voltar a estar Em Curso.', 16, 1);
        RETURN;
    END

    IF @EstadoAtual IN (4, 5)
    BEGIN
        RAISERROR('Um jogo Cancelado ou Adiado não pode mudar de estado.', 16, 1);
        RETURN;
    END

    -- Atualiza o estado
    UPDATE Jogo SET Estado = @NovoEstado WHERE ID = @JogoID;

    -- Se mudou para Finalizado (3), verificar se existe resultado.
    -- Se não existir, criar resultado a zeros (Desconhecido).
    IF @NovoEstado = 3
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Resultado WHERE JogoID = @JogoID)
        BEGIN
            INSERT INTO Resultado (JogoID, GolosCasa, GolosFora, DataHoraAtualizacao)
            VALUES (@JogoID, 0, 0, GETDATE());
        END
        
        -- Daqui a pouco vamos criar a SP para resolver as apostas ganhas/perdidas, e é aqui que a chamaríamos.
    END

    -- Se mudar para Cancelado (4) ou Adiado (5), tratamos de anular as respetivas apostas pendentes (Estado 1)
    IF @NovoEstado IN (4, 5)
    BEGIN
        UPDATE Aposta SET Estado = 4 WHERE JogoID = @JogoID AND Estado = 1;
    END
END
GO