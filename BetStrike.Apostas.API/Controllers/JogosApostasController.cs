using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/jogos")]
    [ApiController]
    public class JogosApostasController : ControllerBase
    {
        private readonly DbConnectionHelper _db;

        public JogosApostasController(DbConnectionHelper db)
        {
            _db = db;
        }

        // POST: api/jogos
        [HttpPost]
        public IActionResult InserirJogo([FromBody] CriarJogoRequest request)
        {
            try
            {
                using var conexao = _db.ObterConexao();
                using var cmd = new SqlCommand("sp_InserirJogo", conexao);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Codigo", request.Codigo);
                cmd.Parameters.AddWithValue("@DataHoraInicio", request.DataHoraInicio);
                cmd.Parameters.AddWithValue("@EquipaCasa", request.EquipaCasa);
                cmd.Parameters.AddWithValue("@EquipaFora", request.EquipaFora);
                cmd.Parameters.AddWithValue("@Competicao", request.Competicao);

                conexao.Open();

                // O ExecuteScalar retorna o ID do novo jogo que nós devolvemos com SCOPE_IDENTITY lá na BD
                var novoId = Convert.ToInt32(cmd.ExecuteScalar());

                return Ok(new { mensagem = "Jogo sincronizado com sucesso.", jogoId = novoId });
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

        // PUT: api/jogos/{id}/estado
        [HttpPut("{id}/estado")]
        public IActionResult AtualizarEstadoJogo(int id, [FromBody] AtualizarEstadoRequest request)
        {
            try
            {
                using var conexao = _db.ObterConexao();

                // === ATUALIZAR O ESTADO ===
                using var cmdEstado = new SqlCommand("sp_AtualizarEstadoJogo", conexao);
                cmdEstado.CommandType = CommandType.StoredProcedure;
                cmdEstado.Parameters.AddWithValue("@JogoID", id);
                cmdEstado.Parameters.AddWithValue("@NovoEstado", request.NovoEstado);

                conexao.Open();
                cmdEstado.ExecuteNonQuery();

                // === SE PASSOU A FINALIZADO, RESOLVER AS APOSTAS (Requisito Crítico) ===
                if (request.NovoEstado == 3) // 3 = Finalizado
                {
                    using var cmdResolver = new SqlCommand("sp_ResolverApostas", conexao);
                    cmdResolver.CommandType = CommandType.StoredProcedure;
                    cmdResolver.Parameters.AddWithValue("@JogoID", id);
                    cmdResolver.ExecuteNonQuery();
                }

                return Ok(new { mensagem = "Estado do jogo atualizado." + (request.NovoEstado == 3 ? " Apostas também foram processadas e saldos atualizados!" : "") });
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

        // GET: api/jogos
        [HttpGet]
        public IActionResult ConsultarJogos([FromQuery] DateTime? data = null, [FromQuery] int? estado = null)
        {
            var jogos = new List<JogoResponse>();

            try
            {
                using var conexao = _db.ObterConexao();
                using var cmd = new SqlCommand("sp_ConsultarJogos", conexao);
                cmd.CommandType = CommandType.StoredProcedure;

                if (data.HasValue) cmd.Parameters.AddWithValue("@DataInicio", data.Value);
                if (estado.HasValue) cmd.Parameters.AddWithValue("@Estado", estado.Value);

                conexao.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    jogos.Add(new JogoResponse
                    {
                        Id = (int)reader["ID"],
                        Codigo = reader["Codigo"].ToString()!,
                        DataHoraInicio = (DateTime)reader["DataHoraInicio"],
                        EquipaCasa = reader["EquipaCasa"].ToString()!,
                        EquipaFora = reader["EquipaFora"].ToString()!,
                        Competicao = reader["Competicao"].ToString()!,
                        Estado = (int)reader["Estado"]
                    });
                }

                return Ok(jogos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao consultar jogos.", erro = ex.Message });
            }
        }
    }
}