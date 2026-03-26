namespace BetStrike.Apostas.API.Models
{
    public class JogoResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public DateTime DataHoraInicio { get; set; }
        public string EquipaCasa { get; set; } = string.Empty;
        public string EquipaFora { get; set; } = string.Empty;
        public string Competicao { get; set; } = string.Empty;
        public int Estado { get; set; }
    }
}