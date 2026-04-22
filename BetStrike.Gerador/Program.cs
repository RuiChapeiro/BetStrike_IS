using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class JogoParaAPI
{
    public string Codigo_Jogo { get; set; } = string.Empty;
    public int Jornada { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public string EquipaCasa { get; set; } = string.Empty;
    public string EquipaFora { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public int Estado { get; set; }
}

class Program
{
    private static readonly string ApiBaseUrl = "http://localhost:5238/api/jogos";
    private static readonly HttpClient _httpClient = new HttpClient();
    private static readonly Random _random = new Random();
    private static readonly string JornadaFilePath = Path.Combine(AppContext.BaseDirectory, "jornada.txt");

    private static readonly List<string> EquipasOrdenadasPorClassificacao = new()
    {
        "FC Porto", "SL Benfica", "Sporting CP", "SC Braga", "FC Famalicão" ,"Gil Vicente FC",
        "Moreirense FC", "Vitória SC", "Estoril Praia", "FC Arouca", "FC Alverca", "Rio Ave FC",
        "Santa Clara", "CD Nacional", "Estrela da Amadora", "Casa Pia AC", "CD Tondela", "AFS"
    };

    private static readonly Dictionary<string, double> ForcaPorEquipa = CriarForcaPorEquipa(EquipasOrdenadasPorClassificacao);

    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine(" BETSTRIKE - SIMULADOR DE LIGA (APP GERADORA) ");
        Console.WriteLine("==================================================");

        var jornadaAtual = ObterProximaJornada();
        var jogos = EmparelharEquipas(jornadaAtual);

        Console.WriteLine($"\n[JORNADA {jornadaAtual}] [CRIADOS {jogos.Count} JOGOS]. A enviar para Plataforma de Resultados...");

        foreach (var jogo in jogos)
        {
            await InserirJogoNaAPI(jogo);
        }

        Console.WriteLine("\nTodos os jogos foram Agendados (Estado 1) na Plataforma.");
        Console.WriteLine("A simulação (Estado 2 -> 3) vai iniciar dentro de 5 segundos...\n");
        await Task.Delay(5000);

        List<Task> tarefasDeSimulacao = new List<Task>();

        foreach (var jogo in jogos)
        {
            tarefasDeSimulacao.Add(Task.Run(() => SimularJogoAsync(jogo)));
        }

        await Task.WhenAll(tarefasDeSimulacao);

        Console.WriteLine("\n[FINAL DA JORNADA] Todos os jogos terminaram.");
        Console.ReadLine();
    }

    static int ObterProximaJornada()
    {
        var jornadaAtual = 0;

        if (File.Exists(JornadaFilePath) && int.TryParse(File.ReadAllText(JornadaFilePath), out var ultimaJornada))
        {
            jornadaAtual = ultimaJornada;
        }

        jornadaAtual++;
        File.WriteAllText(JornadaFilePath, jornadaAtual.ToString());
        return jornadaAtual;
    }

    static List<JogoParaAPI> EmparelharEquipas(int jornadaAtual)
    {
        var equipas = new List<string>(EquipasOrdenadasPorClassificacao);

        for (int i = 0; i < equipas.Count; i++)
        {
            int randomIndex = _random.Next(i, equipas.Count);
            (equipas[i], equipas[randomIndex]) = (equipas[randomIndex], equipas[i]);
        }

        var jogos = new List<JogoParaAPI>();
        string anoAtual = DateTime.Now.Year.ToString();

        for (int i = 0; i < equipas.Count; i += 2)
        {
            int numJogoNaJornada = (i / 2) + 1;
            string codigoGerado = $"FUT-{anoAtual}-{jornadaAtual:D2}{numJogoNaJornada:D2}";

            jogos.Add(new JogoParaAPI
            {
                Codigo_Jogo = codigoGerado,
                Jornada = jornadaAtual,
                DataHoraInicio = DateTime.Now.AddMinutes(5),
                EquipaCasa = equipas[i],
                EquipaFora = equipas[i + 1],
                GolosCasa = 0,
                GolosFora = 0,
                Estado = 1
            });
        }

        return jogos;
    }

    static async Task SimularJogoAsync(JogoParaAPI jogo)
    {
        var forcaCasa = ObterForcaEquipa(jogo.EquipaCasa);
        var forcaFora = ObterForcaEquipa(jogo.EquipaFora);

        jogo.Estado = 2;
        await AtualizarJogoNaAPI(jogo);
        Console.WriteLine($"[{jogo.Codigo_Jogo}] APITO INICIAL! {jogo.EquipaCasa} vs {jogo.EquipaFora}");

        for (int minuto = 1; minuto <= 9; minuto++)
        {
            await Task.Delay(10000);

            bool houveGolo = false;
            var probabilidadeCasa = Math.Clamp(0.15 * forcaCasa, 0.02, 0.45);
            var probabilidadeFora = Math.Clamp(0.10 * forcaFora, 0.02, 0.45);

            if (_random.NextDouble() < probabilidadeCasa) { jogo.GolosCasa++; houveGolo = true; }
            if (_random.NextDouble() < probabilidadeFora) { jogo.GolosFora++; houveGolo = true; }

            if (houveGolo)
            {
                await AtualizarJogoNaAPI(jogo);
                Console.WriteLine($"[GOLO {jogo.Codigo_Jogo}!] {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");
            }
        }

        jogo.Estado = 3;
        await AtualizarJogoNaAPI(jogo);
        Console.WriteLine($"[{jogo.Codigo_Jogo}] FIM DE JOGO! Resultado Final: {jogo.GolosCasa}-{jogo.GolosFora}");
    }

    static Dictionary<string, double> CriarForcaPorEquipa(List<string> equipasOrdenadas)
    {
        var escaloes = new[] { 1.3, 1.2, 1.1, 1.0, 0.9, 0.8 };
        var forcaPorEquipa = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < equipasOrdenadas.Count; i++)
        {
            int indiceEscalao = Math.Min(i / 3, escaloes.Length - 1);
            forcaPorEquipa[equipasOrdenadas[i]] = escaloes[indiceEscalao];
        }

        return forcaPorEquipa;
    }

    static double ObterForcaEquipa(string equipa)
    {
        return ForcaPorEquipa.TryGetValue(equipa, out var forca) ? forca : 1.0;
    }

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
        catch (HttpRequestException)
        {
            Console.WriteLine("❌ ERRO: A Plataforma de Resultados (API) não parece estar ligada.");
        }
    }

    static async Task AtualizarJogoNaAPI(JogoParaAPI jogo)
    {
        try
        {
            await _httpClient.PutAsJsonAsync($"{ApiBaseUrl}/{jogo.Codigo_Jogo}", jogo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falha na sync {jogo.Codigo_Jogo}: {ex.Message}");
        }
    }
}