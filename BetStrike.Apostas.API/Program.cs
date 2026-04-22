var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<BetStrike.Apostas.API.Data.DbConnectionHelper>();
builder.Services.AddHttpClient();

// 1. Dizer à API para gerar o mapeamento dos endpoints
builder.Services.AddEndpointsApiExplorer();
// 2. Registar o gerador do Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 3. Ativar o uso dos ficheiros gerados (.json)
    app.UseSwagger();
    // 4. Ativar a tal interface gráfica bonita (que dá o erro 404 sem isto arrancar)
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();