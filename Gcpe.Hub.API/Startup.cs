﻿using System.Collections.Generic;
using System.Data.SqlClient;
using AutoMapper;
using Gcpe.Hub.API.Helpers;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

namespace Gcpe.Hub.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private IConfiguration Configuration { get; }
        private IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));

            services.AddDbContext<HubDbContext>(options => options.UseSqlServer(Configuration["HubDbContext"])
                .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning)));

            if (Configuration["AuthType"] == "AzureAD")
            {
                this.ConfigureAzureAuth(services);
            }
            else
            {
                this.ConfigureKeycloakAuth(services);
            }
            this.ConfigureAuthorizationPolicies(services);

            services.AddMvc(opt =>
            {
                opt.EnableEndpointRouting = false;
            })
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);


            services.AddSwaggerGen(setupAction =>
            {
                setupAction.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "Alpha",
                    Title = "BC Gov Hub API service",
                    Description = "The .Net Core API for the Hub"
                });
                setupAction.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        Implicit = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = Configuration["AuthType"] == "AzureAD" ?
                        new Uri(Configuration["AzureAD:AuthorizationUrl"]) :
                        new Uri(Configuration["Keycloak:Instance"] + Configuration["Keycloak:AuthorizationPath"]),

                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "openid login scope" },
                                { "profile", "profile scope" },
                                { "email", "email scope" },
                            }

                        }
                    },

                });
                setupAction.OperationFilter<SecurityRequirementsOperationFilter>();
                setupAction.OperationFilter<OperationIdCorrectionFilter>();
            });

            services.AddHealthChecks()
                .AddCheck("sql", () =>
                {
                    using (var connection = new SqlConnection(Configuration["HubDbContext"]))
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (SqlException)
                        {
                            return HealthCheckResult.Unhealthy();
                        }

                        return HealthCheckResult.Healthy();
                    }
                })
                .AddCheck("Webserver is running", () => HealthCheckResult.Healthy("Ok"));

            services.AddCors();
        }

        public virtual void ConfigureAzureAuth(IServiceCollection services)
        {
            services.AddAuthentication(AzureADDefaults.BearerAuthenticationScheme)
                .AddAzureADBearer(options => Configuration.Bind("AzureAD", options));

            services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, options =>
            {
                options.Authority = options.Authority + "/v2.0/";
                options.TokenValidationParameters.ValidAudiences = new string[] { options.Audience, $"api://{options.Audience}" };
                options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.ValidateAadIssuer;
            });
        }

        public virtual void ConfigureKeycloakAuth(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.Authority = Configuration["Keycloak:Instance"] + Configuration["Keycloak:AuthorityPath"];
                o.Audience = Configuration["Keycloak:Audience"];
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "user_roles"
                };
            });
        }

        public virtual void ConfigureAuthorizationPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ReadAccess", policy => policy.RequireRole("Viewer", "Contributor"));
                options.AddPolicy("WriteAccess", policy => policy.RequireRole("Contributor"));
            });
        }

        private class OperationIdCorrectionFilter : IOperationFilter
        { // GetActivity() instead of ApiActivitiesByIdGet()
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                {
                    operation.OperationId = actionDescriptor.ActionName;
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // app.UseHsts();
            }

            app.UseHealthChecks("/hc", new HealthCheckOptions { AllowCachingResponses = false });

            // app.UseHttpsRedirection();

            // temporary CORS fix
            app.UseCors(opts => opts.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed((host) => true).AllowCredentials());

            app.UseAuthentication();

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.OAuthClientId(Configuration["AuthType"] == "AzureAD" ? Configuration["AzureAD:ClientId"] : Configuration["Keycloak:Audience"]);
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BC Gov Hub API service");
            });
        }
    }
}
