using Kw.Micro;
using Kw.Micro.Aspects;
using Kw.Micro.Communications;
using Kw.Micro.Logging;
using Kw.Micro.Rmq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Serilog;
using Serilog.Extensions.Logging;
using System.Net.Sockets;
using System.Reflection;
using AutoMapper;
using Demo4.Core;
using Microsoft.EntityFrameworkCore;

using static Kw.Micro.ServiceEnvironment;

namespace Demo4.Worker
{
    [CompileDateTime(nameof(CompiledAt))]
    internal class Setup
    {
        ILogger<Setup> logger = null!;

        public DateTime CompiledAt { get; set; } // время компиляции сборки

        public async Task<IHost> ComposeApplication()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();

            ////
            //// Логирование
            ////

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(
                    new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build())
                .CreateLogger();

            ServiceEnvironment.LoggerFactory = new SerilogLoggerFactory(Log.Logger);
            
            builder.UseSerilog(Log.Logger);
            
            logger = CreateLogger<Setup>()!;

            logger.Write(LL.I, $"Starting {Application} build time {CompiledAt}");

            ////
            //// Конфигурация
            ////

            Configuration = new ConfigurationManager();

            Configuration
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables();

            builder.ConfigureHostConfiguration(x =>
            {
                x.AddConfiguration(Configuration);
            });

            ////
            //// Службы
            ////

            builder.ConfigureServices((services) =>
            {
                services.AddDbContext<DocumentContext>();
                services.AddScoped<DocumentContext, DocumentContext>();
                services.AddAutoMapper(x =>
                {
                    x.AllowNullCollections = true;
                    x.LicenseKey = AutoMapperLicense.DATA;
                    x.AddMaps(Assembly.GetExecutingAssembly());
                });
            });

            ////
            //// RabbitMQ и сервер
            ////

            if (Assembly.GetEntryAssembly()!.GetName().Name! != "ef") // не создавать при запуске миграций из студии
            {
                ConnectionFactory.DefaultAddressFamily = AddressFamily.InterNetwork;

                RmqConfig config = Configuration.GetSection("Communications").Get<RmqConfig>()!;

                RmqCommunicator communicator = new(config);
                
                logger.Write(LL.I, $"Created an RMQ communicator at {config.Host}:{config.Port}");

                builder.ConfigureServices((services) =>
                {
                    services.AddSingleton((ICommunicator)communicator);
                    services.AddSingleton<Server, Server>();
                });
            }

            ////
            //// Приложение
            ////

            IHost host = builder.Build();

            ServiceEnvironment.ServiceProvider = host.Services.GetRequiredService<IServiceProvider>();
            ServiceEnvironment.Mapper = host.Services.GetRequiredService<IMapper>();

            var hal = host.Services.GetRequiredService<IHostApplicationLifetime>();

            hal.ApplicationStarted.Register(OnStartup);
            hal.ApplicationStopping.Register(OnShutdown);

            ////
            //// Применение миграций
            ////

            await host.Services.CreateScope().ServiceProvider.GetRequiredService<DocumentContext>().Database.MigrateAsync();

            return host;
        }
    }
}
