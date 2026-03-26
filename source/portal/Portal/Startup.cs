using System;
using System.Framework.Web;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using PHStatistics.Portal.Services;

namespace PHStatistics.Portal {
    public class Startup(IConfiguration configuration) {
        public IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services) {
            // 設定 CORS，允許帶 Cookie 的跨網域請求
            services.AddCors(options => options.AddPolicy("AllPassOrigins", builder => {
                builder.WithOrigins("http://npas.ckc-highspeed.com.tw")
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
            }));

            // 設定 MVC 與 JSON 序列化
            services.AddControllersWithViews()
                    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null)
                    .AddNewtonsoftJson(options => {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });

            // 報表匯出服務
            services.AddScoped<ReportExportService>();

            // 啟用 Session 設定
            services.AddSession(options => {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // 支援 HTTP
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.IdleTimeout = TimeSpan.FromMinutes(20);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseXssPrevention(new() {
                XContentTypeNoSniff = true,
                XFrame = XFrameOptions.SAMEORIGIN,
                XXssProtection = XXssProtection.Blocked,
                ContentSecurityPolicy = null
            });

            // 調整 Cookie Policy 支援非 HTTPS
            app.UseCookiePolicy(new CookiePolicyOptions {
                Secure = CookieSecurePolicy.SameAsRequest,
                MinimumSameSitePolicy = SameSiteMode.Lax,
                HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
            });

            app.UseSession();

            var contentTypeProvider = new FileExtensionContentTypeProvider {
                Mappings = { [".woff"] = "font/woff", [".woff2"] = "font/woff2" }
            };
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypeProvider });

            app.UseRouting();

            // 指定使用 CORS 政策
            app.UseCors("AllPassOrigins");

            app.UseAuthorization();

            app.UseRequestLocalization(options => {
                var supportedCultures = new CultureInfo[] { new("en-US"), new("zh-TW") };
                options.DefaultRequestCulture = new RequestCulture(supportedCultures[1]);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            app.UseEndpoints(configure => {
                // Admin Area 路由
                configure.MapControllerRoute(
                    name: "admin",
                    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
                    defaults: new { area = "Admin" }
                );
                configure.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                );
                configure.MapControllerRoute(
                    name: "custom",
                    pattern: "Custom/{*url}",
                    defaults: new { controller = "Custom", action = "HandleUnknownAction" }
                );
            });
        }
    }
}
