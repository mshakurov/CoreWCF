
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
                host.Run();
            }


        }
    }
}
