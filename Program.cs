using LocalGovProcessor.Data;
using LocalGovProcessor.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<LocalGovDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LocalGovProcessor")));
builder.Services.AddScoped<DocxParserService>();
builder.Services.AddScoped<PdfParserService>();
builder.Services.AddScoped<DocumentPersistenceService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
