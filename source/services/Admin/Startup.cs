using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;

namespace PHStatistics.Services.Admin {
    /// <summary>
    /// 啟動
    /// </summary>
    public class Startup {
        /// <summary>
        /// 系統組態
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) => Configuration = configuration;

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">服務集</param>
        public void ConfigureServices(IServiceCollection services) {
            services.AddCors(options => options.AddPolicy("AllPassOrigins", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            services.AddControllers().AddNewtonsoftJson(options => { // for circular json
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Version = "v1",
                    Title = "PHStatistics Admin API",
                    Description = "提供 PHStatistics 後台使用的 API"
                });

                # region 如果專案 XML 文件存在時，引用 XML 註解

                var path = Path.Combine(AppContext.BaseDirectory, "J.Framework.xml");
                if (File.Exists(path)) c.IncludeXmlComments(path);
                path = Path.Combine(AppContext.BaseDirectory, "J.Framework.Core.xml");
                if (File.Exists(path)) c.IncludeXmlComments(path);
                path = Path.Combine(AppContext.BaseDirectory, "PHStatistics.xml");
                if (File.Exists(path)) c.IncludeXmlComments(path);
                path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                if (File.Exists(path)) c.IncludeXmlComments(path);

                #endregion

                //c.CustomSchemaIds(e => e.FullName);
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">建造者</param>
        /// <param name="env">環境</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin Service v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
