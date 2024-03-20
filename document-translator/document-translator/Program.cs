using Aspose.Cells.Charts;
using document_translator.Components;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ITranslatorService,TranslatorService >();
builder.Services.AddScoped<IConverterService, ConverterService>();
builder.Services.AddScoped<NotificationService>();
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


//using document_translator.Components;
//using Microsoft.Extensions.DependencyInjection;

//var builder = WebApplication.CreateBuilder(args);

//// Combine services configuration from both files
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//// Add HttpClient configuration from Startup.cs
//builder.Services.AddHttpClient<IApiService>(client =>
//{
//    client.BaseAddress = new Uri("https://api.example.com");
//});


//var app = builder.Build();

//// Configure request pipeline, combining logic from both files
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseAntiforgery();
//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();
