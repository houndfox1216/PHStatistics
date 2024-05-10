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

namespace EmptyProject.Portal {
    public class Startup(IConfiguration configuration) {
        public IConfiguration Configuration { get; } = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddCors(options => options.AddPolicy("AllPassOrigins", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            services.AddControllersWithViews().AddNewtonsoftJson(options => { // for circular json
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            });
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // 僅接受 HTTPS，HTTP 將自動導向至 HTTPS
            app.UseHttpsRedirection();

            // 繼續傳遞 Forwarded Headers，適用於之後有 proxy 或 load balance 的架構下
            app.UseForwardedHeaders(new() {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            // 使用 XSS 防護．包含 CSP
            app.UseXssPrevention(new() {
                XContentTypeNoSniff = true,
                XFrame = XFrameOptions.SAMEORIGIN,
                XXssProtection = XXssProtection.Blocked,
                ContentSecurityPolicy = null,
                //ContentSecurityPolicy = new() {
                //    DefaultSource = "'self'",
                //    ImgSource = "'self' data: blob:",
                //    FrameSource = "'self' https://www.google.com",
                //    GoogleFontEnabled = true,
                //    //NonceEnabled = true,
                //    //ScriptSource = "'self'",
                //    //ScriptInlineEnabled = false,
                //    //UnsafeEvalEnabled = false,
                //    //StyleSource = "'self'",
                //    //StyleInlineEnabled = false,
                //    //FormAction = "'self'",
                //    //GoogleAnalyticsEnabled = false,
                //    //FacebookShareButtonEnabled = false,
                //}
            });

            // 要求 Cookie 的最低安全等級，避免出現中低嚴重程度漏洞
            app.UseCookiePolicy(new CookiePolicyOptions {
                Secure = CookieSecurePolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.Strict,
                HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
            });

            // 使用 Session
            app.UseSession();


            // 使用靜態檔案，並指定 Mime Types
            var contentTypeProvider = new FileExtensionContentTypeProvider {
                Mappings = { [".woff"] = "font/woff", [".woff2"] = "font/woff2" }
            };
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypeProvider });

            // 使用路由屬性
            app.UseRouting();

            // 允許跨域請求
            app.UseCors();

            // 使用授權功能
            app.UseAuthorization();

            // 將請求進行本地化轉換，沒被語系支援的請求將已預設語系處理
            app.UseRequestLocalization(options => {
                var supportedCultures = new CultureInfo[] { new("en-US"), new("zh-TW") };
                options.DefaultRequestCulture = new RequestCulture(supportedCultures[1]);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            // 使用端點規則作為路由使用
            app.UseEndpoints(configure => {
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
