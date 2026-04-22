namespace BetStrike.Apostas.API.Models
{
    public class ApostaResponse
    {
        public int Id { get; set; }
        public int JogoId { get; set; }
        public string CodigoJogo { get; set; } = string.Empty;
        public string EquipaCasa { get; set; } = string.Empty;
        public string EquipaFora { get; set; } = string.Empty;
        public int Jornada { get; set; }
        public int UtilizadorId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Montante { get; set; }
        public decimal Odd { get; set; }
        public int Estado { get; set; }
        public int EstadoJogo { get; set; }
        public string EstadoDescricao { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }

        // Requisito: O endpoint de detalhe deve calcular o prémio potencial
        public decimal PremioPotencial => Montante * Odd;
    }
}