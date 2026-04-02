using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// ==== CLASSES DE MODELO ==== 
// Têm de ter a mesma estrutura do que a API está à espera!
public class JogoParaAPI
{
    public string Codigo_Jogo { get; set; } = string.Empty;
    public DateTime DataHoraInicio { get; set; }
    public string EquipaCasa { get; set; } = string.Empty;
    public string EquipaFora { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public int Estado { get; set; } // 1-Agendado, 2-Em Curso, 3-Finalizado
}

class Program
{
    // ==========================================
    // CONFIGURAÇÕES DA API E SIMULAÇÃO
    // ==========================================
    // IMPORTANTE: Tu deves alterar a Porta (5238) para a porta que a tua API Resultados usa quando lhe dás Play!
    private static readonly string ApiBaseUrl = "http://localhost:5238/api/jogos";
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly Random _random = new Random();

    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" BETSTRIKE - SIMULADOR DE LIGA (APP GERADORA) ");
        Console.WriteLine("==================================================");

        var jogos = EmparelharEquipas();

        Console.WriteLine($"\n[CRIADOS {jogos.Count} JOGOS]. A enviar para Plataforma de Resultados...");

        // 1. Fase: Mandar POST para a API com o Estado 1 (Agendado)
        foreach (var jogo in jogos)
        {
            await InserirJogoNaAPI(jogo);
        }

        Console.WriteLine("\nTodos os jogos foram Agendados (Estado 1) na Plataforma.");
        Console.WriteLine("A simulação (Estado 2 -> 3) vai iniciar dentro de 5 segundos...\n");
        await Task.Delay(5000);

        // 2. Fase: Simular os Jogos em Paralelo usando Tasks
        List<Task> tarefasDeSimulacao = new List<Task>();

        foreach (var jogo in jogos)
        {
            // Dispara uma thread paralela isolada para cada jogo, todas correm ao mesmo tempo!
            tarefasDeSimulacao.Add(Task.Run(() => SimularJogoAsync(jogo)));
        }

        // Fica à espera que TODAS as Tasks paralalelas de todos os jogos acabem
        await Task.WhenAll(tarefasDeSimulacao);

        Console.WriteLine("\n[FINAL DA JORNADA] Todos os jogos terminaram.");
        Console.ReadLine();
    }

    // ==========================================
    // MÉTODO: EMPARELHAMENTO E ID AUTOMÁTICO
    // ==========================================
    static List<JogoParaAPI> EmparelharEquipas()
    {
        var equipas = new List<string> {
            "Sporting CP", "FC Porto", "Benfica", "SC Braga", "Vitória SC", "Moreirense",
            "Farense", "Arouca", "Famalicão", "Gil Vicente", "Boavista", "Estoril Praia",
            "Rio Ave", "Estrela da Amadora", "Portimonense", "Casa Pia", "Farense", "Chaves"
        };

        // Baralhar a lista de equipas aleatoriamente
        for (int i = 0; i < equipas.Count; i++)
        {
            int randomIndex = _random.Next(i, equipas.Count);
            (equipas[i], equipas[randomIndex]) = (equipas[randomIndex], equipas[i]);
        }

        var jogos = new List<JogoParaAPI>();
        int jornadaAtual = 3; // Valor inventado, a simular a jornada 3
        string anoAtual = DateTime.Now.Year.ToString();

        // Agrupar aos pares (2 a 2). Como 18 equipas dão 9 jogos:
        for (int i = 0; i < equipas.Count; i += 2)
        {
            int numJogoNaJornada = (i / 2) + 1; // 1, 2, 3... 9

            // Formato FUT-AAAA-JJNN (ex: FUT-2025-0301)
            string codigoGerado = $"FUT-{anoAtual}-{jornadaAtual:D2}{numJogoNaJornada:D2}";

            jogos.Add(new JogoParaAPI
            {
                Codigo_Jogo = codigoGerado,
                DataHoraInicio = DateTime.Now.AddMinutes(5), // Começa "daqui a 5 minutos"
                EquipaCasa = equipas[i],
                EquipaFora = equipas[i + 1],
                GolosCasa = 0,
                GolosFora = 0,
                Estado = 1 // Agendado
            });
        }
        return jogos;
    }

    // ==========================================
    // MÉTODO: SIMULADOR DE MOTOR (THE MAGIC)
    // ==========================================
    static async Task SimularJogoAsync(JogoParaAPI jogo)
    {
        // O jogo muda o estado para "Em Curso"
        jogo.Estado = 2;

        // Chamada HTTP PUT API: Atualiza que começou a rolar a bola
        await AtualizarJogoNaAPI(jogo);
        Console.WriteLine($"[{jogo.Codigo_Jogo}] APITO INICIAL! {jogo.EquipaCasa} vs {jogo.EquipaFora}");

        // Simulação do tempo: Vamos fazer 9 ciclos (representam 90 mins). 
        // Cada ciclo demora 10 segundos, no total a thread de cada jogo fica viva 90s.
        for (int minuto = 1; minuto <= 9; minuto++)
        {
            await Task.Delay(10000); // 10 segundos = 10 minutos simulados

            bool houveGolo = false;

            // Algoritmo de Golo: Sorteio Aleatório (A média do enunciado pede 2 a 3 golos).
            // Damos 15% de probabilidade por cada 10 mins p/ casa e 10% p/ fora.
            if (_random.NextDouble() < 0.15) { jogo.GolosCasa++; houveGolo = true; }
            if (_random.NextDouble() < 0.10) { jogo.GolosFora++; houveGolo = true; }

            // IMPORTANTE DO ENUNCIADO: Só mandar PUT se houver mudança de estado ou golo!
            if (houveGolo)
            {
                await AtualizarJogoNaAPI(jogo);
                Console.WriteLine($"[GOLOOO {jogo.Codigo_Jogo}!] {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");
            }
        }

        // Fim dos 90 minutos.
        jogo.Estado = 3; // Finalizado
        await AtualizarJogoNaAPI(jogo);
        Console.WriteLine($"[{jogo.Codigo_Jogo}] FIM DE JOGO! Resultado Final: {jogo.GolosCasa}-{jogo.GolosFora}");
    }

    // ==========================================
    // COMUNICAÇÕES HTTP COM A API RESULTADOS
    // ==========================================
    static async Task InserirJogoNaAPI(JogoParaAPI jogo)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBaseUrl, jogo);
            if (!response.IsSuccessStatusCode)
            {
                var info = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro ao Inserir {jogo.Codigo_Jogo}: {info}");
            }
        }
        catch (HttpRequestException) { Console.WriteLine("❌ ERRO: A Plataforma de Resultados (API) não parece estar ligada."); }
    }

    static async Task AtualizarJogoNaAPI(JogoParaAPI jogo)
    {
        try
        {
            await _httpClient.PutAsJsonAsync($"{ApiBaseUrl}/{jogo.Codigo_Jogo}", jogo);
        }
        catch (Exception ex) { Console.WriteLine($"Falha na sync {jogo.Codigo_Jogo}: {ex.Message}"); }
    }
}