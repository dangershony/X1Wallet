﻿using Microsoft.Extensions.DependencyInjection;
using Obsidian.Features.X1Wallet.SecureApi;

namespace Obsidian.x1d.Api
{
    public static class ControllerRegistration
    {
        public static IMvcBuilder AddSecureApi(this IMvcBuilder builder, IServiceCollection services)
        {
            builder.AddApplicationPart(typeof(SecureApiControllerBase).Assembly);
            builder.AddControllersAsServices();
            return builder;
        }
    }
}
