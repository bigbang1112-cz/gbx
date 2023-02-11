using BigBang1112;
using BigBang1112.Gbx.Server;

var builder = WebApplication.CreateBuilder(args);

var options = new AppOptions
{
    Title = "Gbx",
    Assembly = typeof(Program).Assembly
};

// Add services to the container.

App.Services(builder.Services, options, builder.Configuration);
GbxServerApp.Services(builder.Services, builder.Configuration);

var app = builder.Build();

App.Middleware(app, options);
GbxServerApp.Middleware(app);

app.Run();
