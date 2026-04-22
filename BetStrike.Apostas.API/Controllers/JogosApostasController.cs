using BetStrike.Apostas.API.Data;
using BetStrike.Apostas.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BetStrike.Apostas.API.Controllers
{
    [Route("api/jogos")]
    [ApiController]
    public class JogosApostasController : ControllerBase
    {
        private static readonly Regex CodigoJornadaRegex = new(@"^FUT-\d{4}-(\d{2})\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly DbConnectionHelper _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public JogosApostasController(DbConnectionHelper db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
                conexao.Open();

                var estadoAnterior = ObterEstadoJogoAtual(conexao, id);

                using var cmdEstado = new SqlCommand("sp_AtualizarEstadoJogo", conexao);
                cmdEstado.CommandType = CommandType.StoredProcedure;
                cmdEstado.Parameters.AddWithValue("@JogoID", id);
                cmdEstado.Parameters.AddWithValue("@NovoEstado", request.NovoEstado);
                cmdEstado.ExecuteNonQuery();

                SincronizarEstadoApostasDoJogo(conexao, id, estadoAnterior, request.NovoEstado);

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
        public async Task<IActionResult> ConsultarJogos([FromQuery] DateTime? data = null, [FromQuery] int? estado = null)
        {
            try
            {
                var jogos = ObterJogosPorStoredProcedure(data, estado);

                if (jogos.Count == 0)
                {
                    await SincronizarJogosDaApiResultadosAsync();
                    jogos = ObterJogosPorStoredProcedure(data, estado);
                }

                return Ok(jogos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao consultar jogos.", erro = ex.Message });
            }
        }

        // GET: api/jogos/disponiveis-aposta
        [HttpGet("disponiveis-aposta")]
        public async Task<IActionResult> ObterJogosDisponiveisParaAposta()
        {
            try
            {
                var jogosExternos = await ObterJogosDaApiResultadosAsync();

                if (jogosExternos.Count > 0)
                {
                    await SincronizarJogosDaApiResultadosAsync(jogosExternos);
                }

                var jogos = ObterJogosDisponiveis();

                if (jogosExternos.Count > 0)
                {
                    AtualizarGolosEEstadoEmTempoReal(jogos, jogosExternos);

                    var externosAtivos = jogosExternos
                        .Where(j => j.Estado == 1 || j.Estado == 2)
                        .ToDictionary(j => j.CodigoJogo, StringComparer.OrdinalIgnoreCase);

                    jogos = jogos
                        .Where(j => externosAtivos.ContainsKey(j.Codigo))
                        .OrderBy(j => j.DataHoraInicio)
                        .ThenBy(j => j.Id)
                        .ToList();
                }
                else
                {
                    if (jogos.Count == 0)
                    {
                        await SincronizarJogosDaApiResultadosAsync();
                        jogos = ObterJogosDisponiveis();
                    }

                    jogos = jogos.Where(j => j.Estado == 1 || j.Estado == 2).ToList();
                }

                return Ok(jogos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensagem = "Erro ao consultar jogos disponíveis para aposta.", erro = ex.Message });
            }
        }

        private List<JogoResponse> ObterJogosPorStoredProcedure(DateTime? data, int? estado)
        {
            var jogos = new List<JogoResponse>();

            using var conexao = _db.ObterConexao();
            using var cmd = new SqlCommand("sp_ConsultarJogos", conexao);
            cmd.CommandType = CommandType.StoredProcedure;

            if (data.HasValue) cmd.Parameters.AddWithValue("@DataInicio", data.Value);
            if (estado.HasValue) cmd.Parameters.AddWithValue("@Estado", estado.Value);

            conexao.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var codigo = reader["Codigo"].ToString()!;

                jogos.Add(new JogoResponse
                {
                    Id = (int)reader["ID"],
                    Codigo = codigo,
                    Jornada = ExtrairJornadaDoCodigo(codigo),
                    DataHoraInicio = (DateTime)reader["DataHoraInicio"],
                    EquipaCasa = reader["EquipaCasa"].ToString()!,
                    EquipaFora = reader["EquipaFora"].ToString()!,
                    Competicao = reader["Competicao"].ToString()!,
                    Estado = (int)reader["Estado"]
                });
            }

            return jogos;
        }

        private List<JogoApostaResponse> ObterJogosDisponiveis()
        {
            var jogos = new List<JogoApostaResponse>();

            using var conexao = _db.ObterConexao();
            using var cmd = new SqlCommand(@"
                    SELECT
                        j.ID,
                        j.Codigo,
                        j.DataHoraInicio,
                        j.EquipaCasa,
                        j.EquipaFora,
                        j.Competicao,
                        j.Estado,
                        ISNULL(r.GolosCasa, 0) AS GolosCasa,
                        ISNULL(r.GolosFora, 0) AS GolosFora
                    FROM Apostas.dbo.Jogo j
                    LEFT JOIN Apostas.dbo.Resultado r ON r.JogoID = j.ID
                    WHERE j.Estado IN (1, 2)
                    ORDER BY j.DataHoraInicio, j.ID;", conexao);

            conexao.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var estado = (int)reader["Estado"];
                var inicio = (DateTime)reader["DataHoraInicio"];
                var minuto = estado == 2 ? Math.Clamp((int)(DateTime.Now - inicio).TotalMinutes, 0, 90) : 0;
                var codigo = reader["Codigo"].ToString()!;

                jogos.Add(new JogoApostaResponse
                {
                    Id = (int)reader["ID"],
                    Codigo = codigo,
                    Jornada = ExtrairJornadaDoCodigo(codigo),
                    DataHoraInicio = inicio,
                    EquipaCasa = reader["EquipaCasa"].ToString()!,
                    EquipaFora = reader["EquipaFora"].ToString()!,
                    Competicao = reader["Competicao"].ToString()!,
                    Estado = estado,
                    GolosCasa = (int)reader["GolosCasa"],
                    GolosFora = (int)reader["GolosFora"],
                    MinutoJogo = minuto
                });
            }

            return jogos;
        }

        private void AtualizarGolosEEstadoEmTempoReal(List<JogoApostaResponse> jogos, List<JogoResultadosDto> jogosExternos)
        {
            if (jogos.Count == 0 || jogosExternos.Count == 0)
            {
                return;
            }

            var externosPorCodigo = jogosExternos.ToDictionary(j => j.CodigoJogo, StringComparer.OrdinalIgnoreCase);

            foreach (var jogo in jogos)
            {
                if (!externosPorCodigo.TryGetValue(jogo.Codigo, out var externo))
                {
                    continue;
                }

                jogo.Estado = externo.Estado;
                jogo.GolosCasa = externo.GolosCasa;
                jogo.GolosFora = externo.GolosFora;
                jogo.Jornada = externo.Jornada > 0 ? externo.Jornada : ExtrairJornadaDoCodigo(externo.CodigoJogo);

                if (externo.DataHoraInicio != default)
                {
                    jogo.DataHoraInicio = externo.DataHoraInicio;
                }

                jogo.MinutoJogo = jogo.Estado == 2
                    ? Math.Clamp((int)(DateTime.Now - jogo.DataHoraInicio).TotalMinutes, 0, 90)
                    : 0;
            }
        }

        private async Task SincronizarJogosDaApiResultadosAsync()
        {
            var jogosExternos = await ObterJogosDaApiResultadosAsync();
            if (jogosExternos.Count == 0)
            {
                return;
            }

            await SincronizarJogosDaApiResultadosAsync(jogosExternos);
        }

        private async Task SincronizarJogosDaApiResultadosAsync(List<JogoResultadosDto> jogosExternos)
        {
            if (jogosExternos.Count == 0)
            {
                return;
            }

            foreach (var jogoExterno in jogosExternos)
            {
                try
                {
                    using var conexao = _db.ObterConexao();
                    conexao.Open();

                    var jogoId = ObterJogoIdPorCodigo(conexao, jogoExterno.CodigoJogo);

                    if (!jogoId.HasValue)
                    {
                        using var cmdInserir = new SqlCommand("sp_InserirJogo", conexao);
                        cmdInserir.CommandType = CommandType.StoredProcedure;
                        cmdInserir.Parameters.AddWithValue("@Codigo", jogoExterno.CodigoJogo);
                        cmdInserir.Parameters.AddWithValue("@DataHoraInicio", jogoExterno.DataHoraInicio);
                        cmdInserir.Parameters.AddWithValue("@EquipaCasa", jogoExterno.EquipaCasa);
                        cmdInserir.Parameters.AddWithValue("@EquipaFora", jogoExterno.EquipaFora);
                        cmdInserir.Parameters.AddWithValue("@Competicao", "Liga BetStrike");

                        jogoId = Convert.ToInt32(cmdInserir.ExecuteScalar());
                    }
                    else
                    {
                        using var cmdUpdateBase = new SqlCommand(@"
                            UPDATE Apostas.dbo.Jogo
                            SET DataHoraInicio = @DataHoraInicio,
                                EquipaCasa = @EquipaCasa,
                                EquipaFora = @EquipaFora,
                                Competicao = @Competicao
                            WHERE ID = @JogoID;", conexao);

                        cmdUpdateBase.Parameters.AddWithValue("@JogoID", jogoId.Value);
                        cmdUpdateBase.Parameters.AddWithValue("@DataHoraInicio", jogoExterno.DataHoraInicio);
                        cmdUpdateBase.Parameters.AddWithValue("@EquipaCasa", jogoExterno.EquipaCasa);
                        cmdUpdateBase.Parameters.AddWithValue("@EquipaFora", jogoExterno.EquipaFora);
                        cmdUpdateBase.Parameters.AddWithValue("@Competicao", "Liga BetStrike");
                        cmdUpdateBase.ExecuteNonQuery();
                    }

                    var estadoAnterior = ObterEstadoJogoAtual(conexao, jogoId.Value);

                    using var cmdEstado = new SqlCommand("sp_AtualizarEstadoJogo", conexao);
                    cmdEstado.CommandType = CommandType.StoredProcedure;
                    cmdEstado.Parameters.AddWithValue("@JogoID", jogoId.Value);
                    cmdEstado.Parameters.AddWithValue("@NovoEstado", jogoExterno.Estado);
                    cmdEstado.ExecuteNonQuery();

                    SincronizarEstadoApostasDoJogo(conexao, jogoId.Value, estadoAnterior, jogoExterno.Estado);
                }
                catch
                {
                    // Se falhar sincronização de um jogo, continua os restantes.
                }
            }

            await Task.CompletedTask;
        }

        private async Task<List<JogoResultadosDto>> ObterJogosDaApiResultadosAsync()
        {
            try
            {
                var resultadosBaseUrl = _configuration["ResultadosApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5238";
                var client = _httpClientFactory.CreateClient();

                var jogosExternos = await client.GetFromJsonAsync<List<JogoResultadosDto>>($"{resultadosBaseUrl}/api/jogos");
                return jogosExternos ?? new List<JogoResultadosDto>();
            }
            catch
            {
                return new List<JogoResultadosDto>();
            }
        }

        private static int? ObterJogoIdPorCodigo(SqlConnection conexao, string codigo)
        {
            using var cmd = new SqlCommand("SELECT ID FROM Apostas.dbo.Jogo WHERE Codigo = @Codigo", conexao);
            cmd.Parameters.AddWithValue("@Codigo", codigo);

            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(value);
        }

        private static int ObterEstadoJogoAtual(SqlConnection conexao, int jogoId)
        {
            using var cmd = new SqlCommand("SELECT Estado FROM Apostas.dbo.Jogo WHERE ID = @JogoID", conexao);
            cmd.Parameters.AddWithValue("@JogoID", jogoId);

            var value = cmd.ExecuteScalar();
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static void SincronizarEstadoApostasDoJogo(SqlConnection conexao, int jogoId, int estadoAnterior, int novoEstado)
        {
            if (novoEstado == 2)
            {
                using var cmdAtualizarEmCurso = new SqlCommand(@"
                    UPDATE Apostas.dbo.Aposta
                    SET Estado = 2
                    WHERE JogoID = @JogoID AND Estado = 1;", conexao);

                cmdAtualizarEmCurso.Parameters.AddWithValue("@JogoID", jogoId);
                cmdAtualizarEmCurso.ExecuteNonQuery();
                return;
            }

            if (novoEstado == 3 && estadoAnterior != 3)
            {
                using var cmdResolver = new SqlCommand("sp_ResolverApostas", conexao);
                cmdResolver.CommandType = CommandType.StoredProcedure;
                cmdResolver.Parameters.AddWithValue("@JogoID", jogoId);
                cmdResolver.ExecuteNonQuery();
            }
        }

        private class JogoResultadosDto
        {
            [JsonPropertyName("codigo_Jogo")]
            public string CodigoJogo { get; set; } = string.Empty;

            [JsonPropertyName("jornada")]
            public int Jornada { get; set; }

            [JsonPropertyName("dataHoraInicio")]
            public DateTime DataHoraInicio { get; set; }

            [JsonPropertyName("equipaCasa")]
            public string EquipaCasa { get; set; } = string.Empty;

            [JsonPropertyName("equipaFora")]
            public string EquipaFora { get; set; } = string.Empty;

            [JsonPropertyName("golosCasa")]
            public int GolosCasa { get; set; }

            [JsonPropertyName("golosFora")]
            public int GolosFora { get; set; }

            [JsonPropertyName("estado")]
            public int Estado { get; set; }
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