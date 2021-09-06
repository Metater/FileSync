using System;
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
            if (args.Length != 1)
            {
                Console.WriteLine("No local file output name provided properly!");
                return;
            }
            string outputFileName = args[0];
            MetaMitServer server = new MetaMitServer(1744, 100, System.Net.IPAddress.Parse("192.168.1.84"));
            server.ClientConnected += (object sender, MetaMitStandard.Server.ClientConnectedEventArgs e) =>
            {
                Console.WriteLine("Client Connected: \n" + "\tGuid: " + e.guid + "\n" + "\tEndpoint: " + e.ep);
            };
            server.ClientDisconnected += (object sender, MetaMitStandard.Server.ClientDisconnectedEventArgs e) =>
            {
                Console.WriteLine("Client Disconnected: \n" + "\tGuid: " + e.guid);
            };
            server.DataReceived += (object sender, MetaMitStandard.Server.DataReceivedEventArgs e) =>
            {
                Console.WriteLine("Received file of size " + e.data.Length + " bytes");
                byte[] data = Decompress(e.data);
                File.WriteAllBytes(Directory.GetCurrentDirectory() + "/" + outputFileName, data);
            };
            server.Start();
            Console.WriteLine("Listening on: " + server.Ep);
            while (!Console.KeyAvailable)
            {
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
