using BigBang1112;
using BigBang1112.Gbx.Server;

GBX.NET.Lzo.SetLzo(typeof(GBX.NET.LZO.MiniLZO));

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

GbxServerApp.Middleware(app);
App.Middleware(app, options);

app.Run();
