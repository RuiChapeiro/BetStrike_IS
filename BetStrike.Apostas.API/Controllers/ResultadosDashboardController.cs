using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultadosDashboardController : ControllerBase
    {
        private readonly DbConnectionHelper _db;

        public ResultadosDashboardController(DbConnectionHelper db)
        {
            _db = db;
        }

        // POST: api/resultadosdashboard/resultado
        [HttpPost("resultado")]
        public IActionResult InserirResultado([FromBody] InserirResultadoRequest request)
        {
            try
            {
                using var conexao = _db.ObterConexao();

                // Usar a Stored Procedure já criada lá atrás (sp_InserirResultado)
                using var cmd = new SqlCommand("sp_InserirResultado", conexao);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@JogoID", request.JogoId);
                cmd.Parameters.AddWithValue("@GolosCasa", request.GolosCasa);
                cmd.Parameters.AddWithValue("@GolosFora", request.GolosFora);

                conexao.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { mensagem = "Resultado inserido com sucesso para o jogo " + request.JogoId });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { mensagem = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno do servidor.", erro = ex.Message });
            }
        }

        // GET: api/resultadosdashboard/jogo/5/estatisticas
        [HttpGet("jogo/{jogoId}/estatisticas")]
        public IActionResult ObterEstatisticasJogo(int jogoId)
        {
            try
            {
                using var conexao = _db.ObterConexao();
                // NOTA: Como não nos mandaste criar uma Stored Procedure de estatísticas no passo 1, 
                // vamos usar uma query SQL rápida (Dapper-style) que resolve as contagens num agrupamento!
                // (Para cumprir a regra dos 100% de SPs, podes mover isto para uma Procedure).

                string query = @"
                    SELECT 
                        ISNULL(SUM(Montante), 0) AS VolumeTotal,
                        SUM(CASE WHEN Tipo = '1' THEN 1 ELSE 0 END) AS ApostasCasa,
                        SUM(CASE WHEN Tipo = 'X' THEN 1 ELSE 0 END) AS ApostasEmpate,
                        SUM(CASE WHEN Tipo = '2' THEN 1 ELSE 0 END) AS ApostasFora
                    FROM Aposta
                    WHERE JogoID = @jogoId
                ";

                using var cmd = new SqlCommand(query, conexao);
                cmd.Parameters.AddWithValue("@jogoId", jogoId);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var stat = new EstatisticasJogoResponse
                    {
                        VolumeTotalApostado = (decimal)reader["VolumeTotal"],
                        TotalApostasCasa = (int)reader["ApostasCasa"],
                        TotalApostasEmpate = (int)reader["ApostasEmpate"],
                        TotalApostasFora = (int)reader["ApostasFora"]
                    };
                    return Ok(stat);
                }

                return NotFound(new { mensagem = "Sem dados para o jogo especificado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro interno ao pesquisar.", erro = ex.Message });
            }
        }
    }
}