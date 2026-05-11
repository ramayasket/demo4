using Kw.Micro;
using RabbitMQ.Client;
using Serilog;
using System.Net.Sockets;
using Kw.Micro.Aspects;
using Kw.Micro.Communications;
using Kw.Micro.Logging;
using Kw.Micro.Rmq;
using Serilog.Extensions.Logging;

using static Kw.Micro.ServiceEnvironment;

namespace Demo4.Api
{
    [CompileDateTime(nameof(CompiledAt))]
    public class Setup
    {
        ILogger<Setup> logger;

        public DateTime CompiledAt { get; set; } // время компиляции сборки

        public async Task<WebApplication> ComposeApplication()
        {
            var builder = WebApplication.CreateBuilder();

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

            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger);

            logger = CreateLogger<Setup>()!;

            logger.Write(LL.I, $"Starting {Application} build time {CompiledAt}");

            ////
            //// Конфигурация
            ////

            builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            });

            Configuration = builder.Configuration;

            ////
            //// Службы
            ////

            builder.Services.AddSingleton<Client, Client>();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

            ////
            //// RabbitMQ
            ////

            ConnectionFactory.DefaultAddressFamily = AddressFamily.InterNetwork;

            RmqConfig config = Configuration.GetSection("Communications").Get<RmqConfig>()!;

            RmqCommunicator communicator = new(config);

            logger.Write(LL.I, $"Created an RMQ communicator at {config.Host}:{config.Port}");

            builder.Services.AddSingleton((ICommunicator)communicator);

            ////
            //// Приложение
            ////

            WebApplication app = builder.Build();

            app.UseHttpsRedirection();
            app.MapControllers();
            app.UseSwagger();
            app.UseSwaggerUI();

            ServiceEnvironment.ServiceProvider = app.Services.GetRequiredService<IServiceProvider>();

            IHostApplicationLifetime hal = app.Lifetime;

            hal.ApplicationStarted.Register(OnStartup);
            hal.ApplicationStopping.Register(OnShutdown);

            return app;
        }
    }
}
