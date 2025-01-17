﻿// Licensed to the .NET Foundation under one or more agreements.
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
        builder.AddService<Services.ValueTypeService>();
        builder.AddServiceWebEndpoint<Services.ValueTypeService, ST.BusinessEntity.Server.IValueType>("BusinessEntityServer/IValueType");

        builder.AddService<Services.EntityTypeService>();
        builder.AddServiceWebEndpoint<Services.EntityTypeService, ST.BusinessEntity.Server.IEntityType>("BusinessEntityServer/IEntityType");
      });
    }

  }
}
