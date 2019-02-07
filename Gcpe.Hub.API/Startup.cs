﻿using System.Data.SqlClient;
using AutoMapper;
using Gcpe.Hub.Data.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
using Gcpe.Hub.API.Helpers;
using System.Collections.Generic;

namespace Gcpe.Hub.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper();

            services.AddDbContext<HubDbContext>(options => options.UseSqlServer(Configuration["HubDbContext"])
                .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning)));

            services.AddMvc()
                .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            this.ConfigureAuth(services);

            services.AddSwaggerGen(setupAction =>
            {
                setupAction.SwaggerDoc("v1", new Info
                {
                    Version = "Alpha",
                    Title = "BC Gov Hub API service",
                    Description = "The .Net Core API for the Hub"
                });
                setupAction.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = Configuration["Jwt:AuthorizationUrl"]
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

        public virtual void ConfigureAuth(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.Authority = Configuration["Jwt:Authority"];
                o.Audience = Configuration["Jwt:Audience"];
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ReadAccess", policy => policy.RequireClaim("user_roles", new string[] { "Administrators", "Contributors", "Viewers" }));
                options.AddPolicy("WriteAccess", policy => policy.RequireClaim("user_roles", new string[] { "Administrators", "Contributors" }));
            });
        }

        private class OperationIdCorrectionFilter : IOperationFilter
        { // GetActivity() instead of ApiActivitiesByIdGet()
            public void Apply(Operation operation, OperationFilterContext context)
            {
                if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                {
                    operation.OperationId = actionDescriptor.ActionName;
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BC Gov Hub API service");
            });
        }
    }
}
