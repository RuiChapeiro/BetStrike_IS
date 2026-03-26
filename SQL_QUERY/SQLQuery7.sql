USE Apostas;
GO

-- =========================================================
-- SP Nova: Obter Apostas de forma Dinâmica (Pode procurar tudo ou por filtros)
-- =========================================================
CREATE OR ALTER PROCEDURE sp_ObterApostasDinamica
    @UtilizadorID INT = NULL,
    @JogoID INT = NULL,
    @Estado INT = NULL,
    @ApostaID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ID, JogoID, UtilizadorID, Tipo, Montante, Odd, Estado, DataHora
    FROM Aposta
    WHERE (@ApostaID IS NULL OR ID = @ApostaID)
      AND (@UtilizadorID IS NULL OR UtilizadorID = @UtilizadorID)
      AND (@JogoID IS NULL OR JogoID = @JogoID)
      AND (@Estado IS NULL OR Estado = @Estado);
END
GO