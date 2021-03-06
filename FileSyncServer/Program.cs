using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using MetaMitStandard;

namespace FileSyncServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args[0];
            string host = args[1];
            int port = int.Parse(args[2]);
            string script = args[3];

            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.UseShellExecute = false;
            processInfo.FileName = "sh";
            processInfo.Arguments = script;

            Process process = Process.Start(processInfo);

            MetaMitServer server = new MetaMitServer(port, 100, System.Net.IPAddress.Parse(host));
            server.ClientConnected += (object sender, MetaMitStandard.Server.ClientConnectedEventArgs e) =>
            {
                Console.WriteLine($"[File Sync] Client Connected: \n\tGuid: {e.guid}\n\tEndpoint: {e.ep}");
            };
            server.ClientDisconnected += (object sender, MetaMitStandard.Server.ClientDisconnectedEventArgs e) =>
            {
                Console.WriteLine($"[File Sync] Client Disconnected: \n\tGuid: {e.guid}");
            };
            server.DataReceived += (object sender, MetaMitStandard.Server.DataReceivedEventArgs e) =>
            {
                Console.WriteLine($"[File Sync] Received file of size {e.data.Length} bytes");
                byte[] data = Decompress(e.data);
                File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/{path}", data);
                File.WriteAllText($"{Directory.GetCurrentDirectory()}/quit.req", "");
                process.WaitForExit();
                process = Process.Start(processInfo);
            };
            server.Start();
            Console.WriteLine($"[File Sync] Listening on: {server.Ep}");
            while (true)
            {
                if (process.HasExited) break;
                server.PollEvents();
                Thread.Sleep(5);
            }
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }
    }
}
