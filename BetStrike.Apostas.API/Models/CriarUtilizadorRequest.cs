namespace BetStrike.Apostas.API.Models
{
    public class CriarUtilizadorRequest
    {
        public int? UtilizadorId { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}