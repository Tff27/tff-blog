using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Tff.Blog.Api;

class Program
{
    static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .Build();

        await host.RunAsync();
    }
}