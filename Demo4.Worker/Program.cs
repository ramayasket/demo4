using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Demo4.Worker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = await new Setup().ComposeApplication();
            
            host.Services.GetRequiredService<Server>().Listen();

            await host.RunAsync();
        }
    }
}
