using System;
using System.IO.Ports;
using NModbus.Serial;
using NModbus;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace Modbus
{
    public class Program
    {
        public static async Task SendPLC(ushort pt, ushort address, int value)
        {
            using (SerialPort port = new SerialPort("COM" + pt))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.Open();

                var adapter = new SerialPortAdapter(port);
                // create modbus master
                var factory = new ModbusFactory();

                IModbusMaster master = factory.CreateRtuMaster(adapter);

                byte slaveId = 1;
                //ushort startAddress = 100;
                ////ushort[] registers = new ushort[] { 1, 2, 3 };
                //ushort register = 1;
                ushort[] result = new ushort[]{
                    BitConverter.ToUInt16(BitConverter.GetBytes(value), 2),
                BitConverter.ToUInt16(BitConverter.GetBytes(value), 0) };
                // write three registers
                await master.WriteMultipleRegistersAsync(slaveId, address, result);
            }
        }

        public static async Task CaptureSinglePhotoAsync(string dataFolder)
        {
            TcpClient client = new TcpClient("192.168.1.20", 8500);
            var stream = client.GetStream();
            stream.ReadTimeout = 100000;
            client.ReceiveTimeout = 10000;
            Byte[] data = Encoding.ASCII.GetBytes("BC,CM\r");
            stream.Write(data, 0, data.Length);
            data = new Byte[1024 * 1024];

            // String to store the response ASCII representation.
            String responseData = String.Empty;
            var file = System.IO.File.Create(FileGen(0, "ss", dataFolder));
            // Read the first batch of the TcpServer response bytes.
            await Task.Run(
                () =>
                {
                    try
                    {
                        int current = 0;
                        int count = 0;
                        while (true)
                        {
                            int index = 0;
                            Int32 bytes = stream.Read(data, 0, data.Length);
                            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                            if (responseData.StartsWith("BC,"))
                            {
                                var str = responseData.Split(',');
                                count = Convert.ToInt32(str[1]) - 1;
                                index = responseData.IndexOf(",BM6") + 1;
                            }
                            if (current + bytes > count)
                            {
                                bytes = count - current;
                            }
                            file.Write(data, index, bytes - index);
                            current += bytes - index;
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
        }

        public static async Task Capture5PhotosAsync(string dataFolder)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync("http://192.168.1.83/capture?cache_raw=31&block=true");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                //                json = @"{
                //   'id': 'kE5pogpXYEIrhI4aIxX2',
                //   'jpeg_cache_path': {
                //                    '1': '/images/tmp31.jpg',
                //      '2': '/images/tmp32.jpg',
                //      '5': '/images/tmp33.jpg'
                //   },
                //   'raw_cache_path': {
                //                    '1': '/images/tmp26.tif',
                //      '2': '/images/tmp27.tif',
                //      '3': '/images/tmp28.tif',
                //      '4': '/images/tmp29.tif',
                //      '5': '/images/tmp30.tif'
                //   },
                //   'status': 'complete',
                //   'time': '2016-01-28T02:28:30.000Z'
                //}";
                JObject address = JObject.Parse(json);
                List<string> list = new List<string>();
                for (int i = 1; i <= 5; i++)
                {
                    list.Add(address["raw_cache_path"][i.ToString()].ToString());
                }
                Parallel.For(0, 5, async i =>
                {
                    var stream = await client.GetStreamAsync("http://192.168.1.83" + list[i]);
                    var fs = new System.IO.FileStream(FileGen(i, list[i], dataFolder), System.IO.FileMode.Create);
                    await stream.CopyToAsync(fs);
                });

            }
        }

        private static string FileGen(int index, string name, string parentFolder)
        {
            StringBuilder filename = new StringBuilder();
            filename.Append(parentFolder);
            filename.Append(index);
            filename.Append("-");
            filename.Append(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
            if (index == 0)
                filename.Append(".bmp");
            else
                filename.Append(".tif");
            return filename.ToString();
        }
    }
}