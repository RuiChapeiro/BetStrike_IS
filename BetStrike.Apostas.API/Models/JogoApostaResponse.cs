namespace BetStrike.Apostas.API.Models
{
    public class JogoApostaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int Jornada { get; set; }
        public DateTime DataHoraInicio { get; set; }
        public string EquipaCasa { get; set; } = string.Empty;
        public string EquipaFora { get; set; } = string.Empty;
        public string Competicao { get; set; } = string.Empty;
        public int Estado { get; set; }
        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
        public int MinutoJogo { get; set; }
    }
}
