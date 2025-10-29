using NBT;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));

var apiKey = builder.Configuration["App:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
    throw new InvalidOperationException("API key is missing from configuration.");

var fileDirectory = builder.Configuration["App:FileDirectory"];
if (string.IsNullOrEmpty(fileDirectory))
    throw new InvalidOperationException("File Directory is missing from configuration.");

builder.Services.AddRazorPages();

builder.Services.AddHttpClient<GeminiImagePromptClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.UseAuthorization();

app.MapRazorPages();

app.Run();
