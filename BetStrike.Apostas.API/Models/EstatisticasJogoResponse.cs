namespace BetStrike.Apostas.API.Models
{
    public class EstatisticasJogoResponse
    {
        public decimal VolumeTotalApostado { get; set; }
        public int TotalApostasCasa { get; set; }
        public int TotalApostasEmpate { get; set; }
        public int TotalApostasFora { get; set; }
        // Num cenário real teríamos cálculos para Margem de Risco com base nas Odds, 
        // mas para esta mock-up devolvemos a contagem bruta.
    }
}