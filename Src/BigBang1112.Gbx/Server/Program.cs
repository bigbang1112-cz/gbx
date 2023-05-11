using BigBang1112;
using BigBang1112.Gbx.Server;
using BigBang1112.Gbx.Server.Options;
using Serilog;

GBX.NET.Lzo.SetLzo(typeof(GBX.NET.LZO.MiniLZO));

var builder = WebApplication.CreateBuilder(args);

var options = new AppOptions
{
    Title = "Gbx",
    Assembly = typeof(Program).Assembly
};

builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console();

    var seqOptions = context.Configuration.GetSection(Constants.Seq).Get<SeqOptions>();

    if (!string.IsNullOrEmpty(seqOptions.Url))
    {
        config.WriteTo.Seq(seqOptions.Url);
    }
    
    config.ReadFrom.Configuration(context.Configuration);
});


// Add services to the container.

App.Services(builder.Services, options, builder.Configuration);
GbxServerApp.Services(builder.Services, builder.Configuration);

var app = builder.Build();

GbxServerApp.Middleware(app);
App.Middleware(app, options);

app.Run();
