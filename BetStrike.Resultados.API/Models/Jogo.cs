namespace BetStrike.Resultados.API.Models
{
    public class Jogo
    {
        public int Id { get; set; } // Identificador interno gerado pela API
        public string Codigo_Jogo { get; set; } = string.Empty; // Ex: FUT-2025-0103
        public int Jornada { get; set; }
        public DateTime DataHoraInicio { get; set; }
        public string EquipaCasa { get; set; } = string.Empty;
        public string EquipaFora { get; set; } = string.Empty;
        public int GolosCasa { get; set; } = 0;
        public int GolosFora { get; set; } = 0;
        public int Estado { get; set; } = 1; // 1-Agendado, 2-Em Curso, 3-Finalizado, 4-Cancelado, 5-Adiado
    }
}
