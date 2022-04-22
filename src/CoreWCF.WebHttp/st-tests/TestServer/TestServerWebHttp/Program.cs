
using System;

using Helpers;

using Microsoft.AspNetCore.Hosting;

namespace TestServerWebHttp
{

  public partial class Program
  {
    public static void Main(string[] args)
    {
      IWebHost hostWcf = ServiceHelper.CreateWebHostBuilder<Startup>(8080)
        .UseStartup(webHostBuilderContext => new Startup(8080, 8043))
        .Build();
      using (hostWcf)
      {
        var runTaskWcf = hostWcf.RunAsync();

        IWebHost hostWeb = ServiceHelper.CreateWebHostBuilder<Startup>(8081)
          .UseStartup<StartupWebHTTP>()
          .Build();

        using (hostWeb)
        {

          var runTaskWeb = hostWeb.RunAsync();

          System.Threading.Tasks.Task.Factory.StartNew(async () =>
          {
            try
            {
              (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("BusinessEntityServer/IValueType/GetValueTypeList", 8081);
              Console.WriteLine($":Http.IValueType.GetValueTypeList. statusCode: {statusCode}");

              try { System.IO.File.WriteAllText(@".\log.Http.IValueType.GetValueTypeList.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}"); }
              catch (Exception ex) { Console.WriteLine($":Http.IValueType.GetValueTypeList. Warning: can't log to file: {ex.Message}"); }

              if (statusCode == System.Net.HttpStatusCode.OK)
              {
                ST.BusinessEntity.Server.ValueTypeData[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.ValueTypeData[]>(content);

                Console.WriteLine($":Http.IValueType.GetValueTypeList. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"# :Http.IValueType.GetValueTypeList: {ex.Message}");
            }
          });

          System.Threading.Tasks.Task.Factory.StartNew(async () =>
          {
            try
            {
              (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.PostAsync("BusinessEntityServer/IEntityType/GetEntityTypeList", 8081);
              Console.WriteLine($":Http.IEntityType.GetEntityTypeList. statusCode: {statusCode}");

              try { System.IO.File.WriteAllText(@".\log.Http.IEntityType.GetEntityTypeList.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}"); }
              catch (Exception ex) { Console.WriteLine($"Http.IEntityType.GetEntityTypeList. Warning: can't log to file: {ex.Message}"); }

              if (statusCode == System.Net.HttpStatusCode.OK)
              {
                ST.BusinessEntity.Server.EntityType[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.EntityType[]>(content);

                Console.WriteLine($":Http.IEntityType.GetEntityTypeList. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"# :Http.IEntityType.GetEntityTypeList: {ex.Message}");
            }
          });

          System.Threading.Tasks.Task.WaitAll(runTaskWcf, runTaskWeb);
        }
      }


    }

    public static void Main1(string[] args)
    {
      IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>().Build();
      using (host)
      {
        var runTask = host.RunAsync();

        System.Threading.Tasks.Task.Factory.StartNew(async () =>
        {
          try
          {
            (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("BusinessEntityServer/IValueType/GetValueTypeList", 8080);
            System.IO.File.WriteAllText(@".\log.Http.IValueType.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}");
            ST.BusinessEntity.Server.ValueTypeData[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.ValueTypeData[]>(content);

            Console.WriteLine($":Http.IValueType. statusCode is OK: {System.Net.HttpStatusCode.OK == statusCode}");
            Console.WriteLine($":Http.IValueType. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
          }
          catch (Exception ex)
          {
            Console.WriteLine($"# :Http.IValueType: {ex.Message}");
          }
        });

        System.Threading.Tasks.Task.Factory.StartNew(async () =>
        {
          try
          {
            (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("BusinessEntityServer/IEntityType/GetEntityTypeList", 8080);
            System.IO.File.WriteAllText(@".\log.Http.IEntityType.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}");
            ST.BusinessEntity.Server.EntityType[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.EntityType[]>(content);

            Console.WriteLine($":Http.IEntityType. statusCode is OK: {System.Net.HttpStatusCode.OK == statusCode}");
            Console.WriteLine($":Http.IEntityType. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
          }
          catch (Exception ex)
          {
            Console.WriteLine($"# :Http.IEntityType: {ex.Message}");
          }
        });

        runTask.Wait();
      }


    }
  }
}
