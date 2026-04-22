USE Apostas;
GO

DELETE FROM dbo.Aposta;
DELETE FROM dbo.Resultado;
DELETE FROM dbo.Jogo;
GO

DBCC CHECKIDENT ('dbo.Aposta', RESEED, 0);
DBCC CHECKIDENT ('dbo.Jogo', RESEED, 0);
GO

USE Pagamentos;
GO

DELETE FROM dbo.Transacao;
DELETE FROM dbo.Saldo_Utilizador;
GO

DBCC CHECKIDENT ('dbo.Transacao', RESEED, 0);
GO