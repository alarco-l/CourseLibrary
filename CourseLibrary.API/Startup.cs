using System;
using System.Runtime.InteropServices;
using AutoMapper;
using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;

namespace CourseLibrary.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
           services.AddControllers(options => {
               // If accept header not available, it will not be default and return 406 Not Acceptable status code
               options.ReturnHttpNotAcceptable = true;
               // The first ouput formatter in the list, is the default one.
           })
           .AddNewtonsoftJson(options => {
               options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
           })
           .AddXmlDataContractSerializerFormatters()
           .ConfigureApiBehaviorOptions(setupAction => {
               setupAction.InvalidModelStateResponseFactory = context =>
               {
                    // create problem details object
                    var problemDetailsFactory = context.HttpContext
                                                       .RequestServices
                                                       .GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory
                                            .CreateValidationProblemDetails(context.HttpContext, context.ModelState);

                    // add additional info not added by default
                    problemDetails.Detail = "See the errors field for details.";
                    problemDetails.Instance = context.HttpContext.Request.Path;

                    // find out which status code to use
                    var actionExecutingContext = context as ActionExecutingContext;

                    // if there are modelstate errors and all arguments were correctly
                    // found/parsed we're dealing with validation error
                    if (context.ModelState.ErrorCount > 0
                        && actionExecutingContext?.ActionArguments.Count == context.ActionDescriptor.Parameters.Count)
                        {
                            problemDetails.Type = "https://courselibrary.com/modelvalidationproblem";
                            problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                            problemDetails.Title = "One or more validation errors occured.";

                            return new UnprocessableEntityObjectResult(problemDetails)
                            {
                                ContentTypes = {"application/problem+json"}
                            };
                        }
                    // if one of the arguments wasn't correctly found/parsed
                    // we're dealing with null/unparseable input
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "One or more validation errors occured.";

                            return new UnprocessableEntityObjectResult(problemDetails)
                            {
                                ContentTypes = {"application/problem+json"}
                            };
               };
           });

           services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();

            services.AddDbContext<CourseLibraryContext>(options =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    options.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=courseLibrary;Trusted_Connection=True;");
                }
                else
                {
                    options.UseSqlite("Data Source=courseLibrary.db");
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later.");
                    });
                });
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
