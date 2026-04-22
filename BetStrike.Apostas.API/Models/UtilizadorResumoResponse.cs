namespace BetStrike.Apostas.API.Models
{
    public class UtilizadorResumoResponse
    {
        public int UtilizadorId { get; set; }
        public decimal SaldoAtual { get; set; }
        public DateTime DataHoraAtualizacao { get; set; }
    }
}
