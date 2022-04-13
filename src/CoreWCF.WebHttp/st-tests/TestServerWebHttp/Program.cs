
using System;

using Helpers;

using Microsoft.AspNetCore.Hosting;

namespace TestServerWebHttp
{

    public partial class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = ServiceHelper.CreateWebHostBuilder<Startup>().Build();
            using (host)
            {
                var runTask = host.RunAsync();

                //System.Threading.Tasks.Task.Factory.StartNew(async () =>
                //{
                //    try
                //    {
                //        (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("BusinessEntityServerWeb/IValueType/GetValueTypeList");
                //        System.IO.File.WriteAllText(@".\log.Http.IValueType.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}");
                //        ST.BusinessEntity.Server.ValueTypeData[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.ValueTypeData[]>(content);

                //        Console.WriteLine($":Http.IValueType. statusCode is OK: {System.Net.HttpStatusCode.OK == statusCode}");
                //        Console.WriteLine($":Http.IValueType. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine($"# :Http.IValueType: {ex.Message}");
                //    }
                //});

                //System.Threading.Tasks.Task.Factory.StartNew(async () =>
                //{
                //    try
                //    {
                //        (System.Net.HttpStatusCode statusCode, string content) = await HttpHelpers.GetAsync("BusinessEntityServerWeb/IEntityType/GetEntityTypeList");
                //        System.IO.File.WriteAllText(@".\log.Http.IEntityType.log", $"statusCode: {statusCode}\r\ncontent:\r\n{content}");
                //        ST.BusinessEntity.Server.EntityType[] responseData = SerializationHelpers.DeserializeJson<ST.BusinessEntity.Server.EntityType[]>(content);

                //        Console.WriteLine($":Http.IEntityType. statusCode is OK: {System.Net.HttpStatusCode.OK == statusCode}");
                //        Console.WriteLine($":Http.IEntityType. responseData is not null not empty: {responseData != null && responseData.Length > 0}, Count: {responseData?.Length}");
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine($"# :Http.IEntityType: {ex.Message}");
                //    }
                //});

                runTask.Wait();
            }


        }
    }
}
