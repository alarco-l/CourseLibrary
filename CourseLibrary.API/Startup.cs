using System.Runtime.InteropServices;
using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
           }).AddXmlDataContractSerializerFormatters();

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

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
