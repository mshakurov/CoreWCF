
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

                    //System.Threading.Tasks.Task.Factory.StartNew(async () =>
                    //{
                    //    try
                    //    {
                    //        var fullPath = $"{ST.VideoIntegration.Server.Constants.MODULE_ADDRESS}/{nameof(ST.VideoIntegration.Server.IVideoIntegration)}/{nameof(ST.VideoIntegration.Server.IVideoIntegration.GetOnlineVideoUrl)}";

                    //        (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.PostAsync(fullPath, 8081);
                    //        Console.WriteLine($":Http.IEntityType.GetEntityTypeList. statusCode: {statusCode}");

                    //        try { System.IO.File.WriteAllText(@".\log.Http.IValueType.GetValueTypeList.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}"); }
                    //        catch (Exception ex) { Console.WriteLine($":Http.IValueType.GetValueTypeList. Warning: can't log to file: {ex.Message}"); }

                    //        if (statusCode == System.Net.HttpStatusCode.OK)
                    //        {
                    //            ST.BusinessEntity.Server.ValueTypeData[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.ValueTypeData[]>(content);

                    //            Console.WriteLine($":Http.IValueType.GetValueTypeList. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine($"# :Http.IValueType.GetValueTypeList: {ex.Message}");
                    //    }
                    //});

                    System.Threading.Tasks.Task.WaitAll(runTaskWcf, runTaskWeb);
                }
            }


        }

    }
}
