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

namespace modbus
{
    class Program
    {
        static void Main(string[] args)
        {
            using (SerialPort port = new SerialPort("COM1"))
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
                ushort startAddress = 100;
                ushort[] registers = new ushort[] { 1, 2, 3 };

                // write three registers
                master.WriteMultipleRegisters(slaveId, startAddress, registers);
            }
        }

        static async Task Send()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync("http://192.168.1.83/capture?cache_raw=31&block=true");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                json = @"{
   'id': 'kE5pogpXYEIrhI4aIxX2',
   'jpeg_cache_path': {
                    '1': '/images/tmp31.jpg',
      '2': '/images/tmp32.jpg',
      '5': '/images/tmp33.jpg'
   },
   'raw_cache_path': {
                    '1': '/images/tmp26.tif',
      '2': '/images/tmp27.tif',
      '3': '/images/tmp28.tif',
      '4': '/images/tmp29.tif',
      '5': '/images/tmp30.tif'
   },
   'status': 'complete',
   'time': '2016-01-28T02:28:30.000Z'
}";
                JObject address = JObject.Parse(json);
                List<string> list = new List<string>();
                for (int i = 1; i <= 5; i++)
                {
                    list.Add(address["raw_cache_path"][i].ToString());
                }
                Parallel.For(1, 6, async i =>
                {
                    var stream = await client.GetStreamAsync("http://192.168.1.83" + list[i]);
                    var fs = new System.IO.FileStream(FileGen(i, list[i]), System.IO.FileMode.Create);
                    await stream.CopyToAsync(fs);
                });

            }
        }

        private static string FileGen(int index, string name)
        {
            StringBuilder filename = new StringBuilder();
            filename.Append(index);
            filename.Append("-");
            filename.Append(DateTime.Now);
            return filename.ToString();
        }
    }
}