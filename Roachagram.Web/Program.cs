using Roachagram.Web.Components;
using Roachagram.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRoachagramAPIService, RoachagramAPIService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddServerSideBlazor()
        .AddCircuitOptions(options => { options.DetailedErrors = true; });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/sitemap.xml", async context =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var urls = new[] { "/", /* add other static routes here */ };

    var sb = new System.Text.StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
    foreach (var u in urls)
    {
        sb.Append("<url>");
        sb.Append($"<loc>{System.Net.WebUtility.HtmlEncode(baseUrl + u)}</loc>");
        sb.Append($"<changefreq>weekly</changefreq>");
        sb.Append($"<priority>0.5</priority>");
        sb.Append("</url>");
    }
    sb.Append("</urlset>");

    context.Response.ContentType = "application/xml";
    // cache for a bit
    context.Response.Headers["Cache-Control"] = "public, max-age=3600";
    await context.Response.WriteAsync(sb.ToString());
});


app.Run();
