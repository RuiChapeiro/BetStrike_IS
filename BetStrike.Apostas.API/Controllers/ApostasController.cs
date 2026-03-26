using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApostasController : ControllerBase
    {
        private readonly DbConnectionHelper _db;

        public ApostasController(DbConnectionHelper db)
        {
            _db = db;
        }

        // POST: api/apostas
        [HttpPost]
        public IActionResult RegistarAposta([FromBody] CriarApostaRequest request)
        {
            try
            {
                using var conexao = _db.ObterConexao();
                using var cmd = new SqlCommand("sp_InserirAposta", conexao);
                cmd.CommandType = CommandType.StoredProcedure;

                // Passa os parâmetros exatamente como pede a tua Stored Procedure
                cmd.Parameters.AddWithValue("@JogoID", request.JogoId);
                cmd.Parameters.AddWithValue("@UtilizadorID", request.UtilizadorId);
                cmd.Parameters.AddWithValue("@Tipo", request.Tipo);
                cmd.Parameters.AddWithValue("@Montante", request.Montante);
                cmd.Parameters.AddWithValue("@Odd", request.Odd);

                conexao.Open();

                // Vai executar a inserção e disparar automaticamente o teu Trigger 'trg_Aposta_Insert' 
                // que desconta o saldo do utilizador!
                cmd.ExecuteNonQuery();

                return Ok(new { mensagem = "Aposta registada com sucesso! O valor foi deduzido do saldo." });
            }
            catch (SqlException ex)
            {
                // Se alguma regra de negócio rebentar na Stored Procedure (ex: "Saldo Indisponível"), 
                // o SQL lança um erro com gravidade 16. Apanhamo-lo aqui e mostramos ao cliente.
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno do servidor.", erro = ex.Message });
            }
        }

        // DELETE: api/apostas/5/cancelar
        [HttpDelete("{id}/cancelar")]
        public IActionResult CancelarAposta(int id)
        {
            try
            {
                using var conexao = _db.ObterConexao();
                using var cmd = new SqlCommand("sp_CancelarAposta", conexao);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApostaID", id);

                conexao.Open();
                // A Stored Procedure muda o estado para 4 e o Trigger trata do Reembolso!
                cmd.ExecuteNonQuery();

                return Ok(new { mensagem = "Aposta cancelada e montante reembolsado ao saldo do utilizador." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno.", erro = ex.Message });
            }
        }

        // GET: api/apostas
        [HttpGet]
        public IActionResult ListarApostas([FromQuery] int? utilizadorId, [FromQuery] int? jogoId, [FromQuery] int? estado)
        {
            var apostas = new List<ApostaResponse>();

            try
            {
                using var conexao = _db.ObterConexao();

                // Usando a Stored Procedure para garantir nota máxima!
                using var cmd = new SqlCommand("sp_ObterApostasDinamica", conexao);
                cmd.CommandType = CommandType.StoredProcedure;

                // Em SPs, se um parâmetro é NULL, normalmente é ignorado na query interna
                if (utilizadorId.HasValue) cmd.Parameters.AddWithValue("@UtilizadorID", utilizadorId.Value);
                if (jogoId.HasValue) cmd.Parameters.AddWithValue("@JogoID", jogoId.Value);
                if (estado.HasValue) cmd.Parameters.AddWithValue("@Estado", estado.Value);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    apostas.Add(new ApostaResponse
                    {
                        Id = (int)reader["ID"],
                        JogoId = (int)reader["JogoID"],
                        UtilizadorId = (int)reader["UtilizadorID"],
                        Tipo = reader["Tipo"].ToString()!,
                        Montante = (decimal)reader["Montante"],
                        Odd = (decimal)reader["Odd"],
                        Estado = (int)reader["Estado"],
                        DataHora = (DateTime)reader["DataHora"]
                    });
                }

                return Ok(apostas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao pesquisar apostas.", erro = ex.Message });
            }
        }

        // GET: api/apostas/{id}
        [HttpGet("{id}")]
        public IActionResult ObterDetalheAposta(int id)
        {
            try
            {
                using var conexao = _db.ObterConexao();

                // Usando a mesmíssima Stored Procedure, mas desta vez passando o ID Exato
                using var cmd = new SqlCommand("sp_ObterApostasDinamica", conexao);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ApostaID", id);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var aposta = new ApostaResponse
                    {
                        Id = (int)reader["ID"],
                        JogoId = (int)reader["JogoID"],
                        UtilizadorId = (int)reader["UtilizadorID"],
                        Tipo = reader["Tipo"].ToString()!,
                        Montante = (decimal)reader["Montante"],
                        Odd = (decimal)reader["Odd"],
                        Estado = (int)reader["Estado"],
                        DataHora = (DateTime)reader["DataHora"]
                    };

                    return Ok(aposta);
                }

                return NotFound(new { mensagem = $"Aposta com ID {id} não foi encontrada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno ao obter detalhe.", erro = ex.Message });
            }
        }
    }
}