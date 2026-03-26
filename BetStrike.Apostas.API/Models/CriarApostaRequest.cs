namespace BetStrike.Apostas.API.Models
{
    public class CriarApostaRequest
    {
        public int JogoId { get; set; }
        public int UtilizadorId { get; set; }
        public string Tipo { get; set; } = string.Empty; // "1", "X", "2"
        public decimal Montante { get; set; }
        public decimal Odd { get; set; }
    }
}