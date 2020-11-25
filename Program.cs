using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using twitchBot.Utils;

namespace twitchBot
{
    class Program
    {
        private static IConfigurationRoot _configuration;

        static void Main(string[] args)
        {
            try
            {   
                // Create service collection
                Console.WriteLine("Creating service collection");
                ServiceCollection serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                Bot bot = new Bot(_configuration);

                while (true)
                {
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(_configuration);
        }
    }
}
