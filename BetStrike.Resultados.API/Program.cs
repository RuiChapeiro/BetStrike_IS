var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// O teu serviço Singleton para a memória dos jogos
builder.Services.AddSingleton<BetStrike.Resultados.API.Data.ResultadoStore>();

// Adicionar a geração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Ativa a interface visual do Swagger apenas em modo de desenvolvimento
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();