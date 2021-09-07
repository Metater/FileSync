using System;
using MetaMitStandard;
using System.IO.Compression;
using System.IO;
using System.Threading;

namespace FileSync
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = args[0];
            string ip = args[1];
            int port = int.Parse(args[2]);

            byte[] data = File.ReadAllBytes($@"{Directory.GetCurrentDirectory()}{path}");
            byte[] compressed = Compress(data);
            bool isDone = false;

            MetaMitClient client = new MetaMitClient();
            client.Connected += (object sender, MetaMitStandard.Client.ConnectedEventArgs e) =>
            {
                Console.WriteLine($"[File Sync] Connected to server, sending {compressed.Length} bytes!");
                client.Send(compressed);
            };
            client.DataSent += (object sender, MetaMitStandard.Client.DataSentEventArgs e) =>
            {
                client.Disconnect();
            };
            client.Disconnected += (object sender, MetaMitStandard.Client.DisconnectedEventArgs e) =>
            {
                isDone = true;
            };
            client.Connect(ip, port);

            while (!isDone)
            {
                client.PollEvents();
                Thread.Sleep(20);
            }
            Console.WriteLine("[File Sync] Done!");
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
