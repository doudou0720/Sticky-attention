using Microsoft.EntityFrameworkCore;
using StickyHomeworks.Core.Context;
using StickyHomeworks.Web.Services;

namespace StickyHomeworks.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 从配置中读取服务启用设置，默认启用
            var enableHttpServer = builder.Configuration.GetValue("EnableHttpServer", true);
            var enableGrpcService = builder.Configuration.GetValue("EnableGrpcService", true);
            var httpServerPort = builder.Configuration.GetValue("HttpServerPort", 5000);
            var grpcPort = builder.Configuration.GetValue("GrpcPort", 5001);

            // 配置Kestrel服务器端口
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (enableHttpServer)
                {
                    options.ListenAnyIP(httpServerPort);
                }
                
                if (enableGrpcService)
                {
                    options.ListenAnyIP(grpcPort, listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                    });
                }
            });

            // 条件性添加服务
            if (enableHttpServer)
            {
                // Add services to the container.
                builder.Services.AddControllersWithViews();
            }

            if (enableGrpcService)
            {
                // Add gRPC services
                builder.Services.AddGrpc();
                
                // Register HomeworkService
                builder.Services.AddScoped<HomeworkService>();
            }
            
            // Add DbContext (always needed)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=app.db"));

            var app = builder.Build();

            // 配置HTTP请求管道
            if (enableHttpServer)
            {
                if (!app.Environment.IsDevelopment())
                {
                }

                app.UseStaticFiles();
                app.UseRouting();
            }

            if (enableGrpcService)
            {
                // Enable gRPC-Web middleware
                app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
                
                // Map gRPC services
                app.MapGrpcService<HomeworkService>();
            }

            if (enableHttpServer)
            {
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                
                app.MapFallbackToFile("index.html");
            }

            app.Run();
        }
    }
}