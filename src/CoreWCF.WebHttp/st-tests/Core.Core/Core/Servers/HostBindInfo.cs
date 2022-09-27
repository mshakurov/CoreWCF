using CoreWCF.Channels;
using CoreWCF.Description;

namespace ST.Core
{
  public class HostBindInfo
  {
    public string Name { get; internal set; }
    public string SchemeName { get; internal set; }
    public int Port { get; internal set; }
    public Binding Binding { get; internal set; }
    public Type[] Interfaces {  get; internal set; }
    public Action<Type, Binding, ServiceEndpoint, IServiceProvider> ConfigureEndpoint { get; internal set; }
    public Action<Type, Binding, WebHttpBehavior, IServiceProvider> ConfigureWebEndpoint { get; internal set; }
  }
}