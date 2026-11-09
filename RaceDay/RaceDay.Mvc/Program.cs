using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// MVC session: remembers who is logged in on the website
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Mvc.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// One named HttpClient that always points at the RaceDay API
builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is missing.");
    client.BaseAddress = new Uri(baseUrl);
})
// We forward the API's session cookie by hand, so turn off automatic cookie handling
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false });

builder.Services.AddScoped<RaceDayApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();            // must come after UseRouting and before the endpoints
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();