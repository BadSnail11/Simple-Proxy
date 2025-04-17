using proxy;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    public static async Task Main(string[] args)
    {
        var proxyPort = 8888;
        var proxyServer = new SimpleProxyServer(proxyPort);

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            proxyServer.Stop();
        };

        try
        {
            await proxyServer.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Proxy server error: {ex.Message}");
            proxyServer.Stop();
        }
    }
}