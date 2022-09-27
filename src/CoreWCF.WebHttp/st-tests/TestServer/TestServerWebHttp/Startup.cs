// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using CoreWCF.Configuration;
using CoreWCF;
using CoreWCF.Channels;

namespace TestServerWebHttp
{
    internal class Startup
    {
        private int _port;
        private int _httpsPort;

        public Startup(int port, int httpsPort)
        {
            _port = port;
            _httpsPort = httpsPort;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelWebServices()
                .AddServiceModelServices()
                .AddServiceModelMetadata()
                .AddSingleton(sp => new Services.ValueTypeService())
                .AddSingleton(sp => new Services.EntityTypeService())
                ;
        }

        public void Configure(IApplicationBuilder app)
        {
            var customBinding = new CustomBinding(new BasicHttpBinding());

            app.UseServiceModel(builder =>
            {
                builder.AddService<Services.ValueTypeService>();
                builder.AddServiceEndpoint<Services.ValueTypeService, ST.BusinessEntity.Server.IValueType>(customBinding, $"BusinessEntityServer/{nameof(ST.BusinessEntity.Server.IValueType)}");
                builder.AddServiceEndpoint<Services.ValueTypeService, ST.BusinessEntity.Server.IValueType2>(customBinding, $"BusinessEntityServer/{nameof(ST.BusinessEntity.Server.IValueType2)}");

                builder.AddService<Services.EntityTypeService>();
                builder.AddServiceEndpoint<Services.EntityTypeService, ST.BusinessEntity.Server.IEntityType>(customBinding, "BusinessEntityServer/IEntityType");
            });

            var serviceMetadataBehavior = app.ApplicationServices.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
            serviceMetadataBehavior.HttpGetEnabled = true;
            serviceMetadataBehavior.HttpsGetEnabled = true;
            // If we aren't testing an HTTP based binding, then need to explicitly set the WSDL url using the path
            //if (!Uri.UriSchemeHttp.Equals(customBinding.Scheme) && !Uri.UriSchemeHttps.Equals(customBinding.Scheme))
            {
                serviceMetadataBehavior.HttpGetUrl = new Uri($"http://0.0.0.0:{_port}/BusinessEntityServer");
                serviceMetadataBehavior.HttpsGetUrl = new Uri($"https://0.0.0.0:{_httpsPort}/BusinessEntityServer");
            }
        }

    }
}
