using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UtilizadoresController : ControllerBase
    {
        private readonly DbConnectionHelper _db;

        public UtilizadoresController(DbConnectionHelper db)
        {
            _db = db;
        }

        // POST: api/utilizadores/registar
        [HttpPost("registar")]
        public IActionResult RegistarUtilizador([FromBody] CriarUtilizadorRequest request)
        {
            try
            {
                using (var conexao = _db.ObterConexao())
                {
                    conexao.Open();

                    // Utilizamos uma transação para garantir a Atomicidade exigida no Ponto 4.
                    // Se a operação na Pagamentos falhar, não fica em estado inconsistente.
                    using (var transacao = conexao.BeginTransaction())
                    {
                        try
                        {
                            // Como a criação do utilizador na BD Apostas depende do vosso modelo mental de utilizadores, 
                            // a exigência principal é o Saldo_Utilizador em 'Pagamentos'.
                            // Vamos inserir diretamente na Base de Dados Pagamentos com 50.00€ (Promoção)!

                            var cmdPagamentos = new SqlCommand(@"
                                INSERT INTO Pagamentos.dbo.Saldo_Utilizador (UtilizadorID, SaldoAtual, DataHoraAtualizacao)
                                VALUES (@uid, 50.00, GETDATE());
                            ", conexao, transacao);

                            cmdPagamentos.Parameters.AddWithValue("@uid", request.UtilizadorId);
                            cmdPagamentos.ExecuteNonQuery();

                            // Tudo correu bem, 'Commit' confirma a transação
                            transacao.Commit();

                            return Ok(new
                            {
                                mensagem = "Utilizador registado com sucesso!",
                                id = request.UtilizadorId,
                                saldoInicial = "50.00€"
                            });
                        }
                        catch (SqlException ex)
                        {
                            transacao.Rollback();
                            // Código de Erro 2627 é Violação de Chave Primária (Utilizador já existe)
                            if (ex.Number == 2627)
                                return Conflict(new { mensagem = "Já existe um utilizador com este ID." });

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Ocorreu um erro interno no servidor.", erro = ex.Message });
            }


        }

        // POST: api/utilizadores/{id}/deposito
        // Requisito: Ferramenta de Testes: Endpoint para Depósito de dinheiro fictício em Pagamentos.
        [HttpPost("{id}/deposito")]
        public IActionResult DepositarDinheiroFicticio(int id, [FromBody] decimal valor)
        {
            if (valor <= 0) return BadRequest(new { mensagem = "O valor depositado tem de ser superior a zero." });

            try
            {
                using var conexao = _db.ObterConexao();
                using var cmd = new SqlCommand(@"
                    BEGIN TRANSACTION;
                    
                    INSERT INTO Pagamentos.dbo.Transacao (UtilizadorID, Tipo, Valor, DataHora, Estado)
                    VALUES (@uid, 'DE', @val, GETDATE(), 'Processada');
                    
                    UPDATE Pagamentos.dbo.Saldo_Utilizador
                    SET SaldoAtual = SaldoAtual + @val, DataHoraAtualizacao = GETDATE()
                    WHERE UtilizadorID = @uid;
                    
                    COMMIT;
                ", conexao);

                cmd.Parameters.AddWithValue("@uid", id);
                cmd.Parameters.AddWithValue("@val", valor);

                conexao.Open();
                int linhas = cmd.ExecuteNonQuery();

                if (linhas == 0) return NotFound(new { mensagem = "Utilizador não encontrado na BD Pagamentos." });

                return Ok(new { mensagem = $"Foram depositados {valor}€ na conta do utilizador {id}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno.", erro = ex.Message });
            }
        }
    }
}