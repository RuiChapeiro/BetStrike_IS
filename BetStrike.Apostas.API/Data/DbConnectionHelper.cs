using Microsoft.Data.SqlClient;

namespace BetStrike.Apostas.API.Data
{
    public class DbConnectionHelper
    {
        private readonly string _connectionString;

        public DbConnectionHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Falha ao ler ConnectionString.");
        }

        public SqlConnection ObterConexao()
        {
            return new SqlConnection(_connectionString);
        }
    }
}