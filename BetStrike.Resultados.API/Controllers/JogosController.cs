using BetStrike.Resultados.API.Data;
using BetStrike.Resultados.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Resultados.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JogosController : ControllerBase
    {
        private readonly ResultadoStore _store;

        public JogosController(ResultadoStore store)
        {
            _store = store;
        }

        // POST: api/jogos
        [HttpPost]
        public IActionResult InserirJogo([FromBody] Jogo novoJogo)
        {
            // Tenta inserir na store
            var jogoInserido = _store.InserirJogo(novoJogo);

            if (jogoInserido == null)
            {
                // Se retornou null, é porque o Codigo_Jogo já existia (Requisito obrigatório)
                return Conflict(new { mensagem = $"Já existe um jogo com o código {novoJogo.Codigo_Jogo}." });
            }

            // O Create devolve o Jogo com o Internal ID atribuído e o status 201 Created
            return CreatedAtAction(nameof(InserirJogo), new { id = jogoInserido.Id }, jogoInserido);
        }

        // PUT: api/jogos/{codigo}
        [HttpPut("{codigo}")]
        public IActionResult AtualizarJogo(string codigo, [FromBody] Jogo jogoAtualizado)
        {
            var jogoExistente = _store.ObterPorCodigo(codigo);
            if (jogoExistente == null)
            {
                return NotFound(new { mensagem = $"O jogo de código '{codigo}' não foi encontrado." });
            }

            // --- Validação de Transições de Estado Inválidas (Requisito) ---
            // 3: Finalizado, 4: Cancelado, 5: Adiado
            if (jogoExistente.Estado == 3 && jogoAtualizado.Estado == 2)
            {
                return BadRequest(new { mensagem = "Transição inválida: Um jogo finalizado não pode regressar a 'Em Curso'." });
            }
            if ((jogoExistente.Estado == 4 || jogoExistente.Estado == 5) && jogoExistente.Estado != jogoAtualizado.Estado)
            {
                return BadRequest(new { mensagem = "Transição inválida: Jogos Adiados ou Cancelados não podem mudar de estado." });
            }

            // Atualiza de facto
            jogoAtualizado.Id = jogoExistente.Id; // Fixa o mesmo Id que o antigo tinha
            jogoAtualizado.Codigo_Jogo = codigo;  // Evita que mandem um codigo_jogo diferente no body

            var sucesso = _store.AtualizarJogo(codigo, jogoAtualizado);

            if (!sucesso)
                return StatusCode(500, new { mensagem = "Falha interna ao atualizar jogo." });

            return Ok(jogoAtualizado); // Retorna Ok 200 com os dados
        }

        // GET: api/jogos
        [HttpGet]
        public IActionResult ObterJogos([FromQuery] DateTime? data = null, [FromQuery] int? estado = null)
        {
            var listaJogos = _store.ObterTodos();

            // Aplicar os filtros se os tiverem enviado via Query String na URL (?data=2025-05-10&estado=1)
            if (data.HasValue)
            {
                listaJogos = listaJogos.Where(j => j.DataHoraInicio.Date == data.Value.Date);
            }

            if (estado.HasValue)
            {
                listaJogos = listaJogos.Where(j => j.Estado == estado.Value);
            }

            return Ok(listaJogos);
        }

        // GET: api/jogos/{codigo}
        [HttpGet("{codigo}")]
        public IActionResult ObterJogoPorCodigo(string codigo)
        {
            var jogo = _store.ObterPorCodigo(codigo);
            if (jogo == null)
            {
                return NotFound(new { mensagem = $"Não existe nenhum jogo com o código '{codigo}'." });
            }

            return Ok(jogo);
        }

        // DELETE: api/jogos/{codigo}
        [HttpDelete("{codigo}")]
        public IActionResult RemoverJogo(string codigo)
        {
            var jogoExistente = _store.ObterPorCodigo(codigo);
            if (jogoExistente == null)
            {
                return NotFound(new { mensagem = $"O jogo '{codigo}' não foi encontrado." });
            }

            // Requisito: Só é permitido para jogos no estado Agendado (1)
            if (jogoExistente.Estado != 1)
            {
                return BadRequest(new { mensagem = "Apenas jogos no estado 'Agendado' (1) podem ser removidos do sistema." });
            }

            var removido = _store.RemoverJogo(codigo);
            if (!removido)
                return StatusCode(500, new { mensagem = "Falhou a tentativa de remover jogo." });

            return NoContent(); // Status 204
        }
    }
}
