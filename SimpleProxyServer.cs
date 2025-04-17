using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace proxy
{
    public class SimpleProxyServer
    {
        private readonly int _listenPort;
        private TcpListener _listener;
        private bool _isRunning;

        public SimpleProxyServer(int port)
        {
            _listenPort = port;
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _listenPort);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"Proxy server started on port {_listenPort}");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("Proxy server stopped");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var clientStream = client.GetStream())
            {
                try
                {
                    var buffer = new byte[4096];
                    var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                    var requestString = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (string.IsNullOrEmpty(requestString))
                    {
                        return;
                    }

                    var firstLine = requestString.Substring(0, requestString.IndexOf('\n')).Trim();
                    var parts = firstLine.Split(' ');
                    if (parts.Length < 3)
                    {
                        return;
                    }

                    var method = parts[0];
                    var fullUrl = parts[1];
                    var protocolVersion = parts[2];

                    Uri uri;
                    try
                    {
                        uri = new Uri(fullUrl);
                    }
                    catch (UriFormatException)
                    {
                        uri = new Uri("http://" + fullUrl);
                    }

                    var targetHost = uri.Host;
                    var targetPort = uri.Port;
                    var path = uri.PathAndQuery;

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] REQ: {method} {fullUrl}");

                    using (var targetClient = new TcpClient(targetHost, targetPort))
                    using (var targetStream = targetClient.GetStream())
                    {
                        var modifiedRequest = requestString.Replace(
                            $"{method} {fullUrl}",
                            $"{method} {path}");

                        var requestBytes = Encoding.ASCII.GetBytes(modifiedRequest);
                        await targetStream.WriteAsync(requestBytes, 0, requestBytes.Length);

                        var responseBuffer = new byte[4096];
                        var responseBytesRead = await targetStream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                        await clientStream.WriteAsync(responseBuffer, 0, responseBytesRead);

                        var responseString = Encoding.ASCII.GetString(responseBuffer, 0, responseBytesRead);
                        var responseFirstLine = responseString.Substring(0, responseString.IndexOf('\n')).Trim();
                        var responseParts = responseFirstLine.Split(' ');
                        var statusCode = responseParts.Length >= 2 ? responseParts[1] : "Unknown";

                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] RES: {statusCode} for {fullUrl}");

                        while (responseBytesRead > 0)
                        {
                            responseBytesRead = await targetStream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                            if (responseBytesRead > 0)
                            {
                                await clientStream.WriteAsync(responseBuffer, 0, responseBytesRead);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                }
            }
        }
    }
}
