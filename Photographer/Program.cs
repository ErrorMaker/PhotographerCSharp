using System;
using System.Net.Sockets;
using Photographer.Crypto;
using Photographer.Network.Protocol;

namespace Photographer
{
    class Program
    {
        static void Main(string[] args)
        {
            //HKeyExchange server = new HKeyExchange(65537, "0cac31b429a5be4f51e91807d2fe2ee82e1c9fb9257060537af75a3522e1ff9831d0c8ba6d9a448f71bb3e9a22006fb4b783e4e3b996f783bab76062dccb22280b020b8c490b26ea7e3d2c8d44950bcfbcfeacf0e6b2eb29e9721ea8dfe121efcee4fa82e3ef3e49d3df17677ee80504cf9d37f9a5e924012507a94dfd089aac5", "2be4e9db05c0f7f9fb5fa5c2650d97825b9f58dac33142b14e73d7df2d9d893eac20a2e8fca35083b7a4bf9773f9da3d62a590eb82c8d3cf3353a5934249766aff4a1da0eb4060ce0a650e6211efb2551e732f5a29e6cc903b3220d2e4a1975352917d836aa18312b04ffd26e424c0826b17d71dab4a8d8952854a0101752f1");
            var client = new HKeyExchange(65537, "0cac31b429a5be4f51e91807d2fe2ee82e1c9fb9257060537af75a3522e1ff9831d0c8ba6d9a448f71bb3e9a22006fb4b783e4e3b996f783bab76062dccb22280b020b8c490b26ea7e3d2c8d44950bcfbcfeacf0e6b2eb29e9721ea8dfe121efcee4fa82e3ef3e49d3df17677ee80504cf9d37f9a5e924012507a94dfd089aac5");

            var socket = new TcpClient(AddressFamily.InterNetwork);
            socket.ConnectAsync("game-us.habbo.com", 38101);
            while (!socket.Connected) ;

            NetworkStream stream = socket.GetStream();
            var msg = new HMessage(4000, "PRODUCTION-201703211204-245648941", "FLASH", 1, 0);
            SendMessage(stream, msg);
            msg = new HMessage(3618);
            SendMessage(stream, msg);

            var data = new Byte[2048];
            Int32 bytes = stream.Read(data, 0, data.Length);
            var responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received: {0}", responseData);

            while (true) ;
        }

        static void SendMessage(NetworkStream stream, HMessage msg)
        {
            stream.Write(msg.ToBytes(), 0, msg.ToBytes().Length);
        }
    }
}