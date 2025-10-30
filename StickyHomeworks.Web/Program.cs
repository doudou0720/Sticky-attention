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
            var enableGrpcService = builder.Configuration.GetValue("EnableGrpcService", true);
            var grpcPort = builder.Configuration.GetValue("GrpcPort", 5001);

            // 配置Kestrel服务器端口
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (enableGrpcService)
                {
                    options.ListenAnyIP(grpcPort, listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                    });
                }
            });

            // Add gRPC services
            if (enableGrpcService)
            {
                builder.Services.AddGrpc();
                
                // Register HomeworkService
                builder.Services.AddScoped<HomeworkService>();
            }
            
            // Add DbContext (always needed)
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=app.db"));

            var app = builder.Build();

            if (enableGrpcService)
            {
                // Enable gRPC-Web middleware
                app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
                
                // Map gRPC services
                app.MapGrpcService<HomeworkService>();
            }

            app.Run();
        }
    }
}