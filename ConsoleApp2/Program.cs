using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient("192.168.1.11", 8500);
            var stream = client.GetStream();
            client.ReceiveTimeout = 1000;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("BC,CM\r");
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent");
            data = new Byte[1024 * 1024];

            // String to store the response ASCII representation.
            String responseData = String.Empty;
            var file = System.IO.File.Create("1.bmp");
            // Read the first batch of the TcpServer response bytes.
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        int current = 0;
                        int count = 0;
                        while (true)
                        {
                            Int32 bytes = stream.Read(data, 0, data.Length);
                            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                            if (responseData.StartsWith("BC,"))
                            {
                                var str = responseData.Split(',');
                                count = Convert.ToInt32(str[1]) - 1;
                                continue;
                            }
                            if (current + bytes > count)
                            {
                                break;
                                bytes = count - current;
                            }
                            file.Write(data, 0, bytes);
                            current += bytes;
                            
                            //Console.WriteLine("Received: {0}", responseData);
                        }
                    }
                    finally
                    {
                        // Close everything.
                        file.Close();
                        stream.Close();
                        client.Close();
                    }
                });
            Console.ReadKey();
        }
    }
}
