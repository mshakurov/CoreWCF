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
    internal class StartupWebHTTP
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelWebServices();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseServiceModel(builder =>
            {
                builder.AddService<Services.VideoIntegrationService>();
                builder.AddServiceWebEndpoint<Services.VideoIntegrationService, ST.VideoIntegration.Server.IVideoIntegration>($"{ST.VideoIntegration.Server.Constants.MODULE_ADDRESS}/{nameof(ST.VideoIntegration.Server.IVideoIntegration)}");
            });
        }

    }
}
