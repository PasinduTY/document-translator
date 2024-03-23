using Aspose.Cells.Charts;
using document_translator.Components;
using Radzen;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITranslatorService,TranslatorService >();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ISynchronousTranslationService,SynchronousTranslationService>();
builder.Services.AddScoped<ITextTranslateService,TextTranslateSerrvice>();

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("log.txt") // Specify the log file name
    .CreateLogger();

// Set up Serilog as the logging provider
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();



