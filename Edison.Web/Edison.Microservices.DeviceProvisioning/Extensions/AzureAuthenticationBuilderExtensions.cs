﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AzureAdServiceCollectionExtensions
    {
        public static AuthenticationBuilder AddAzureAdBearer(this AuthenticationBuilder builder, IConfiguration configuration)
        {
            builder.AddJwtBearer("AzureAd", options =>
            {
                options.Audience = configuration["AzureAd:ClientId"];
                options.TokenValidationParameters.ValidIssuers = new List<string>()
                {
                    $"{configuration["AzureAd:Instance"]}{configuration["AzureAd:TenantId"]}/v2.0" //For Issuer validation for MSAL v2
                };
                options.Authority = $"{configuration["AzureAd:Instance"]}{configuration["AzureAd:TenantId"]}";
            });
            return builder;
        }
    }
}
