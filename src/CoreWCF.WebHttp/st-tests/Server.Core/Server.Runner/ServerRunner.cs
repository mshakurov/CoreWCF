using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ST.Core;
using ST.Server;
using ST.Utils;

namespace Server.Runner
{
  public class TestService123 : ITestService123
  {
    public int _initial;

    int ITestService123.Get() => _initial;
  }

  [ServiceContract(Name = "TestService123", Namespace = "http://www.space-team.com/TestService123")]
  public interface ITestService123
  {
    [OperationContract]
    int Get();
  }

  public class ServerRunner
  {
    private static object _server;
    private static object[] _modules = Array.Empty<object>();
    private static TestService123 _testService123;

    public static void Configure( IWebHostBuilder builder )
    {
      builder.ConfigureKestrel(( context, options ) =>
      {
        options.AllowSynchronousIO = true;
      })
#if DEBUG
      .ConfigureLogging(( ILoggingBuilder logging ) =>
      {
        logging.AddConsole();
        logging.AddFilter("Default", LogLevel.Debug);
        logging.AddFilter("Microsoft", LogLevel.Debug);
        logging.SetMinimumLevel(LogLevel.Debug);
      })
#endif // DEBUG
      .UseKestrel(options =>
      {
        options.AllowSynchronousIO = true;
        options.Listen(System.Net.IPAddress.Any, 55564, listenOptions =>
        {
          if (System.Diagnostics.Debugger.IsAttached)
          {
            listenOptions.UseConnectionLogging();
          }
        });
        options.Listen(System.Net.IPAddress.Any, 55565, listenOptions =>
        {
          listenOptions.UseHttps();
          if (System.Diagnostics.Debugger.IsAttached)
          {
            listenOptions.UseConnectionLogging();
          }
        });
      })
      ;

      #region создание файла конфигурации ApplicationServer
      // создание файла конфигурации ApplicationServer
      if (1 == 0)
      {
        var cfg = ApplicationServer.GetConfig();
        cfg.GetType().GetField("_isFile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(cfg, true);
        cfg.HttpEnabled = true;
        cfg.JsonEnabled = true;
        cfg.OpenHttpEnabled = true;
        cfg.OpenJsonByIpEnabled = true;
        cfg.OpenJsonByIpEnabled = true;
        cfg.ProductName = "Тестовы серверочек";
        cfg.TcpEnabled = false;
        cfg.WindowsAuthenticationEnabled = false;
        cfg.CustomBindings = new ST.Utils.Wcf.CustomBindingItem[] { new ST.Utils.Wcf.CustomBindingItem { Port = 55580, Name = "55580", AuthenticationType = ST.Utils.Wcf.BindingHelper.AuthenticationType.None, ContextUserType = ST.Utils.Wcf.BindingHelper.ContextUserType.None, ProtocoType = ST.Utils.Wcf.BindingHelper.ProtocolType.Soap, TransferType = ST.Utils.Wcf.BindingHelper.TransferType.Http, UseTransportLevelSecurity = false, ZippedType = ST.Utils.Wcf.BindingHelper.ZippedType.None } };
        BaseServer.SetConfig(cfg, ServerType.ApplicationServer);
      }
      #endregion

    }

    public static void CreateServices( IServiceCollection services )
    {
      services
        .AddServiceModelServices()
        .AddServiceModelWebServices()
        .AddServiceModelMetadata()
        ;
      services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

      ApplicationServer server = new();
      _server = server;
      server.Start();
      services.AddSingleton(server);

      _testService123 = new TestService123 { _initial = 777 };
      services.AddSingleton(_testService123);

      (_modules = server.GetModules()).ForEach(m => services.AddSingleton(m));
    }

    public static void PrepareServices( IApplicationBuilder appBilder )
    {
      appBilder.UseServiceModel(builder =>
      {

        if (_server != null)
          AddWCFServices(_server, appBilder, builder);

        _modules.ForEach(m => AddWCFServices(m, appBilder, builder));

        if (_testService123 != null)
        {
          builder.AddService<TestService123>(( serviceOptions ) => { })
          // Add a BasicHttpBinding at a specific endpoint
          .AddServiceEndpoint<TestService123, ITestService123>(new BasicHttpBinding(), "/TestService123/basichttp")
          //.AddServiceEndpoint<TestService123, ITestService123>(new WSHttpBinding(SecurityMode.Transport), "/TestService123/WSHttps")
          ;

          //builder.AddService(typeof(TestService123), ( serviceOptions ) => { });
          //var addressUrl = $"http://0.0.0.0:12345/{nameof(ITestService123)}";
          //Console.WriteLine($"{nameof(TestService123)} - - Url: {addressUrl}");
          //builder.AddServiceEndpoint<TestService123, ITestService123>(new CoreWCF.BasicHttpBinding() { Namespace = "http://www.space-team.com" }, addressUrl);
        }


        var serviceMetadataBehavior = appBilder.ApplicationServices.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
        //var serviceMetadataBehavior = serviceProvider.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
        serviceMetadataBehavior.HttpGetEnabled = true;
        serviceMetadataBehavior.HttpsGetEnabled = true;

        //serviceMetadataBehavior.HttpGetUrl = new Uri($"http://0.0.0.0:55580/Server");

      });
    }

    private static void AddWCFServices( object server, IApplicationBuilder appBilder, IServiceBuilder builder )
    {
      Console.WriteLine($">>> {server.GetType().FullName}");

      builder.AddService(server.GetType(), ( serviceOptions ) => { });

      IEnumerable<HostBindInfo> FilterHosts( IEnumerable<HostBindInfo> _hostst )
      {
        return _hostst.Where(h => h.Port.In(55564, 55565)).OrderBy(hi => hi.Port == 55564 ? 1 : 2).ToArray();
      }

      var infos = FilterHosts((_server as ApplicationServer).GetHostInfos(server));

      if (infos != null)
      {
        foreach (var hi in infos)
        {
          Console.WriteLine($"  - {hi.Name}, {hi.SchemeName}:{hi.Port} ({hi.Binding.GetType().FullName})");
          foreach (var interfaceType in hi.Interfaces)
            Console.WriteLine($"  - - {interfaceType.FullName}");
        }

        foreach (var hi in infos)
        {
          foreach (var interfaceType in hi.Interfaces)
          {
            //var addressUrl = $"{hi.schemeName}://0.0.0.0:{hi.port}/{hi.name}/{interfaceType.Name}";
            if (hi.Port == 55564)
            {
                var addressUrl = new Uri($"/{hi.Name}/{interfaceType.Name}", UriKind.Relative);
                Console.WriteLine($" - - Url: {addressUrl}");

              builder.AddServiceEndpoint(server.GetType(), interfaceType, hi.Binding, addressUrl, addressUrl, se => hi.ConfigureEndpoint(interfaceType, hi.Binding, se, appBilder.ApplicationServices));
            }
            else
            if (hi.Port == 55565)
            {
              var addressUrl = $"/{hi.Name}_web/{interfaceType.Name}";
              Console.WriteLine($" - - Url: {addressUrl}");

              builder.AddServiceWebEndpoint<ApplicationServer>(interfaceType, new WebHttpBinding(), addressUrl, se => hi.ConfigureWebEndpoint(interfaceType, hi.Binding, se, appBilder.ApplicationServices));
            }
          }
        }
      }

      Console.WriteLine($"<<< {server.GetType().FullName}");
    }

  }
}
