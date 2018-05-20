using System;
using System.IO.Ports;
using NModbus.Serial;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using NModbus;
using System.Linq;

namespace ModbusClassic
{
    public class Program
    {
        public static async Task SendStart(SerialPort port, ushort address, int value)
        {
            var adapter = new SerialPortAdapter(port);
            // create modbus master
            var factory = new ModbusFactory();

            IModbusMaster master = factory.CreateRtuMaster(adapter);

            byte slaveId = 1;
            ushort[] result = new ushort[]{
                    BitConverter.ToUInt16(BitConverter.GetBytes(value), 0),
                BitConverter.ToUInt16(BitConverter.GetBytes(value), 2) };
            // write three registers
            await master.WriteMultipleRegistersAsync(slaveId, address, result);
            // 写入距离二
            await master.WriteSingleCoilAsync(slaveId, 2054, true);
            await master.WriteSingleCoilAsync(slaveId, 2054, false);
            // 启动
            await master.WriteSingleCoilAsync(slaveId, 2078, true);
            // read registers
            //var x = await master.ReadHoldingRegistersAsync(slaveId, address, 2);
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToInt32(BitConverter.GetBytes(x[0]).Concat(BitConverter.GetBytes(x[1])).ToArray(), 0));
        }
        public static async Task SendBack(SerialPort port, ushort address)
        {
            var adapter = new SerialPortAdapter(port);
            // create modbus master
            var factory = new ModbusFactory();

            IModbusMaster master = factory.CreateRtuMaster(adapter);

            byte slaveId = 1;
            await master.WriteSingleCoilAsync(slaveId, address, true);
            await master.WriteSingleCoilAsync(slaveId, address, false);
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
                    catch
                    {
                        throw;
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
                JObject address = JObject.Parse(json);
                List<string> list = new List<string>();
                for (int i = 1; i <= 5; i++)
                {
                    list.Add(address["raw_cache_path"][i.ToString()].ToString());
                }
                for (int i = 0; i < 5; i++)
                {
                    var stream = await client.GetStreamAsync("http://192.168.1.83" + list[i]);
                    var fs = new System.IO.FileStream(FileGen5(i, list[i], dataFolder), System.IO.FileMode.Create);
                    await stream.CopyToAsync(fs);
                }
            }
        }

        private static string FileGen(int index, string name, string parentFolder)
        {
            StringBuilder filename = new StringBuilder();
            filename.Append(parentFolder);
            filename.Append("\\");
            filename.Append(index);
            filename.Append("-");
            filename.Append(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
            filename.Append(".bmp");
            return filename.ToString();
        }

        private static string FileGen5(int index, string name, string parentFolder)
        {
            StringBuilder filename = new StringBuilder();
            filename.Append(parentFolder);
            filename.Append("\\");
            filename.Append(index);
            filename.Append("-");
            filename.Append(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
            filename.Append(".tif");
            return filename.ToString();
        }
    }
}