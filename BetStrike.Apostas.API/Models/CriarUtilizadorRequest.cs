namespace BetStrike.Apostas.API.Models
{
    public class CriarUtilizadorRequest
    {
        // Num cenário real teríamos nome, email, password, etc.
        // Para este exercício, vamos receber um ID ou gerá-lo, mas para simular vamos pedir um ID descritivo ou Nome
        public int UtilizadorId { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}