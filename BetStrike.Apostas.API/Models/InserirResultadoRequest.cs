namespace BetStrike.Apostas.API.Models
{
    public class InserirResultadoRequest
    {
        public int JogoId { get; set; }
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
    }
}
