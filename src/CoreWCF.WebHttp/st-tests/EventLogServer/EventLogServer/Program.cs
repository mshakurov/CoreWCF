using System.Diagnostics;
using System.Net;

using CoreWCF;
using CoreWCF.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var port = (ushort)(55555 + ST.Utils.Wcf.WcfProtocolType.BasicHttp);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
    options.Listen(IPAddress.Any, port, listenOptions =>
    {
        if (Debugger.IsAttached
#if DEBUG
    || true
#endif
        )
        {
            listenOptions.UseConnectionLogging();
        }
    });
});

var prepareServer = new ST.Core.BaseServer();
var evLogServer = prepareServer.GetService<ST.EventLog.Server.EventLog>();


builder.Services.AddServiceModelServices()
    .AddServiceModelMetadata()
    .AddSingleton(evLogServer)
    //.AddSingleton(typeof(ST.EventLog.Server.IEventLog), evLogServer)
    //.AddSingleton(typeof(ST.EventLog.Server.IEventLogManager), evLogServer)
    ;

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseServiceModel(builder =>
{
    var pathEventLog = "EventLogServer" /*nameof(ST.EventLog.Server.EventLog)*/;

    var uri = new Uri("http" + "://0.0.0.0:" + port + "/" + pathEventLog);

    Console.WriteLine($"Uri: {uri}");

    var binding = new BasicHttpBinding()
    {
        Namespace = ST.Utils.Constants.BASE_NAMESPACE,
        ReceiveTimeout = TimeSpan.MaxValue,
        SendTimeout = TimeSpan.MaxValue,
        MaxBufferSize = 134217728,
        MaxReceivedMessageSize = int.MaxValue,
        ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas()
        {
            MaxArrayLength = int.MaxValue,
            MaxBytesPerRead = int.MaxValue,
            MaxDepth = int.MaxValue,
            MaxNameTableCharCount = int.MaxValue,
            MaxStringContentLength = int.MaxValue,
        },
    };

    builder
    .AddService<ST.EventLog.Server.EventLog>()
    .AddServiceEndpoint<ST.EventLog.Server.EventLog, ST.EventLog.Server.IEventLog>(binding, pathEventLog + "/" + nameof(ST.EventLog.Server.IEventLog))
    .AddServiceEndpoint<ST.EventLog.Server.EventLog, ST.EventLog.Server.IEventLogManager>(binding, pathEventLog + "/" + nameof(ST.EventLog.Server.IEventLogManager))
    ;


    var serviceMetadataBehavior = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
    serviceMetadataBehavior.HttpsGetEnabled = true;

    serviceMetadataBehavior.HttpGetUrl = uri;
});

app.Run();
