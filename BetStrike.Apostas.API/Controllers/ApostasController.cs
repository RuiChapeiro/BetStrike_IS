using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApostasController : ControllerBase
    {
        private static readonly Regex CodigoJornadaRegex = new(@"^FUT-\d{4}-(\d{2})\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
                cmd.Parameters.AddWithValue("@JogoID", request.JogoId);
                cmd.Parameters.AddWithValue("@UtilizadorID", request.UtilizadorId);
                cmd.Parameters.AddWithValue("@Tipo", request.Tipo);
                cmd.Parameters.AddWithValue("@Montante", request.Montante);
                cmd.Parameters.AddWithValue("@Odd", request.Odd);

                conexao.Open();
                cmd.ExecuteNonQuery();

                return Ok(new { mensagem = "Aposta registada com sucesso! O valor foi deduzido do saldo." });
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
                using var cmd = new SqlCommand(@"
                    SELECT a.ID, a.JogoID, j.Codigo AS CodigoJogo, j.EquipaCasa, j.EquipaFora, a.UtilizadorID, a.Tipo, a.Montante, a.Odd, a.Estado, j.Estado AS EstadoJogo, a.DataHora
                    FROM Aposta a
                    INNER JOIN Jogo j ON j.ID = a.JogoID
                    WHERE (@UtilizadorID IS NULL OR a.UtilizadorID = @UtilizadorID)
                      AND (@JogoID IS NULL OR a.JogoID = @JogoID)
                      AND (@Estado IS NULL OR a.Estado = @Estado)
                    ORDER BY a.DataHora DESC, a.ID DESC;", conexao);

                cmd.Parameters.AddWithValue("@UtilizadorID", (object?)utilizadorId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@JogoID", (object?)jogoId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Estado", (object?)estado ?? DBNull.Value);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var codigoJogo = reader["CodigoJogo"].ToString()!;
                    var estadoAposta = (int)reader["Estado"];
                    var estadoJogo = (int)reader["EstadoJogo"];

                    apostas.Add(new ApostaResponse
                    {
                        Id = (int)reader["ID"],
                        JogoId = (int)reader["JogoID"],
                        CodigoJogo = codigoJogo,
                        EquipaCasa = reader["EquipaCasa"].ToString()!,
                        EquipaFora = reader["EquipaFora"].ToString()!,
                        Jornada = ExtrairJornadaDoCodigo(codigoJogo),
                        UtilizadorId = (int)reader["UtilizadorID"],
                        Tipo = reader["Tipo"].ToString()!,
                        Montante = (decimal)reader["Montante"],
                        Odd = (decimal)reader["Odd"],
                        Estado = estadoAposta,
                        EstadoJogo = estadoJogo,
                        EstadoDescricao = ObterDescricaoEstadoAposta(estadoAposta, estadoJogo),
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
                using var cmd = new SqlCommand(@"
                    SELECT a.ID, a.JogoID, j.Codigo AS CodigoJogo, j.EquipaCasa, j.EquipaFora, a.UtilizadorID, a.Tipo, a.Montante, a.Odd, a.Estado, j.Estado AS EstadoJogo, a.DataHora
                    FROM Aposta a
                    INNER JOIN Jogo j ON j.ID = a.JogoID
                    WHERE a.ID = @ApostaID;", conexao);

                cmd.Parameters.AddWithValue("@ApostaID", id);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var codigoJogo = reader["CodigoJogo"].ToString()!;
                    var estadoAposta = (int)reader["Estado"];
                    var estadoJogo = (int)reader["EstadoJogo"];

                    var aposta = new ApostaResponse
                    {
                        Id = (int)reader["ID"],
                        JogoId = (int)reader["JogoID"],
                        CodigoJogo = codigoJogo,
                        EquipaCasa = reader["EquipaCasa"].ToString()!,
                        EquipaFora = reader["EquipaFora"].ToString()!,
                        Jornada = ExtrairJornadaDoCodigo(codigoJogo),
                        UtilizadorId = (int)reader["UtilizadorID"],
                        Tipo = reader["Tipo"].ToString()!,
                        Montante = (decimal)reader["Montante"],
                        Odd = (decimal)reader["Odd"],
                        Estado = estadoAposta,
                        EstadoJogo = estadoJogo,
                        EstadoDescricao = ObterDescricaoEstadoAposta(estadoAposta, estadoJogo),
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

        private static string ObterDescricaoEstadoAposta(int estadoAposta, int estadoJogo)
        {
            if (estadoJogo == 2)
            {
                return "A decorrer...";
            }

            if (estadoJogo == 1)
            {
                return "Aguardando início";
            }

            if (estadoJogo == 5)
            {
                return "Adiada";
            }

            if (estadoJogo == 3)
            {
                if (estadoAposta == 3)
                {
                    return "Finalizada (Acerto)";
                }

                if (estadoAposta == 4)
                {
                    return "Finalizada (Erro)";
                }

                return "Finalizada";
            }

            if (estadoAposta == 4)
            {
                return "Cancelada";
            }

            return "Pendente";
        }

        private static int ExtrairJornadaDoCodigo(string codigoJogo)
        {
            if (string.IsNullOrWhiteSpace(codigoJogo))
            {
                return 0;
            }

            var match = CodigoJornadaRegex.Match(codigoJogo);
            if (!match.Success)
            {
                return 0;
            }

            return int.TryParse(match.Groups[1].Value, out var jornada) ? jornada : 0;
        }
    }
}