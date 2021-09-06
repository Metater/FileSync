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
            if (args.Length != 1)
            {
                Console.WriteLine("[File Sync] No local file path provided properly!");
                return;
            }
            string localPath = args[0];
            byte[] data = File.ReadAllBytes($@"{Directory.GetCurrentDirectory()}{localPath}");
            //byte[] data = new byte[]{ 0x00 };
            byte[] compressed = Compress(data);
            bool isDone = false;

            MetaMitClient client = new MetaMitClient();
            client.Connected += (object sender, MetaMitStandard.Client.ConnectedEventArgs e) =>
            {
                Console.WriteLine("[File Sync] Connected to server, sending data!");
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
            client.Connect("192.168.1.84", 1744);
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
