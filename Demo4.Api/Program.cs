namespace Demo4.Api
{
    public class Program
    {
        public static async Task Main()
        {
            WebApplication app = await new Setup().ComposeApplication();

            app.Run();
        }
    }
}
