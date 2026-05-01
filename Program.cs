using LocalGovProcessor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<DocxParserService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();