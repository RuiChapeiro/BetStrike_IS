namespace BetStrike.Apostas.API.Models
{
    public class ApostaResponse
    {
        public int Id { get; set; }
        public int JogoId { get; set; }
        public int UtilizadorId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Montante { get; set; }
        public decimal Odd { get; set; }
        public int Estado { get; set; }
        public DateTime DataHora { get; set; }

        // Requisito: O endpoint de detalhe deve calcular o prémio potencial
        public decimal PremioPotencial => Montante * Odd;
    }
}