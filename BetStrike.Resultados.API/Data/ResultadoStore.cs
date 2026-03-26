using System.Collections.Concurrent;
using BetStrike.Resultados.API.Models;

namespace BetStrike.Resultados.API.Data
{
    public class ResultadoStore
    {
        // Uma lista segura para acessos paralelos
        private readonly ConcurrentDictionary<string, Jogo> _jogos = new();
        private int _currentId = 1;

        public Jogo? InserirJogo(Jogo novoJogo)
        {
            // Valida se o código já existe
            if (_jogos.ContainsKey(novoJogo.Codigo_Jogo)) return null;

            novoJogo.Id = Interlocked.Increment(ref _currentId); // ID auto-incremento seguro

            _jogos.TryAdd(novoJogo.Codigo_Jogo, novoJogo);
            return novoJogo;
        }

        // Métodos que vamos usar nos próximos endpoints (fica já preparado):
        public IEnumerable<Jogo> ObterTodos() => _jogos.Values;
        public Jogo? ObterPorCodigo(string codigo) => _jogos.GetValueOrDefault(codigo);
        public bool AtualizarJogo(string codigo, Jogo jogoAtualizado) => _jogos.TryUpdate(codigo, jogoAtualizado, _jogos[codigo]);
        public bool RemoverJogo(string codigo) => _jogos.TryRemove(codigo, out _);
    }
}