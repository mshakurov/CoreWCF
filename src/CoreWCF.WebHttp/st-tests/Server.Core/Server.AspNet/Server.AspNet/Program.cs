using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(( context, options ) =>
{
  options.AllowSynchronousIO = true;
});
// Add WSDL support
builder.Services.AddServiceModelServices().AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddSingleton(new TestService123 { _initial = 123 });

Server.Runner.ServerRunner.CreateServices(builder.Services);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

Server.Runner.ServerRunner.PrepareServices(app);

app.Run();


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