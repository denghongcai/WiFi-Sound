using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WiFi_SoundUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                Int32 port = 14000;
                IPAddress localAddr = IPAddress.Parse("0.0.0.0");
                server = new TcpListener(localAddr, port);
                server.Start();

                Byte[] bytes = new Byte[256];

                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected");

                    NetworkStream stream = client.GetStream();
                    
                    int i;
                    while((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                             
                    }
                    client.Close();
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
