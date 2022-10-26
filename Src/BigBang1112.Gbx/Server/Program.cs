using BigBang1112;
using BigBang1112.Gbx.Server.Extensions;

var options = new AppOptions
{
    Title = "Gbx",
    Assembly = typeof(Program).Assembly
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

App.Services(builder.Services, options);

builder.Services.AddEndpoints();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

App.Middleware(app, options);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseEndpoints();
app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
