using LocalGovProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Register DocxParserService for dependency injection (scoped per request)
builder.Services.AddScoped<DocxParserService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();