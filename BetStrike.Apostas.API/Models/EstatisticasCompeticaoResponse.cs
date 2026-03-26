namespace BetStrike.Apostas.API.Models
{
    public class EstatisticasCompeticaoResponse
    {
        public string Competicao { get; set; } = string.Empty;
        public decimal VolumeTotalApostado { get; set; }
        public double MediaGolosPorJogo { get; set; }
    }
}