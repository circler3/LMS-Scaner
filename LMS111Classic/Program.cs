using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;

namespace LMS111Classic
{
    public class Program
    {
        const int DEVICECOUNT = 2;
        static TcpClient[] clients = new TcpClient[DEVICECOUNT];
        static NetworkStream[] streams = new NetworkStream[DEVICECOUNT];

        public async static Task SendRequestAsync(string dataFolder)
        {
            byte[] bytes = { 0x02, 0x73, 0x52, 0x4E, 0x20, 0x4C, 0x4D, 0x44, 0x73, 0x63, 0x61, 0x6E, 0x64, 0x61, 0x74, 0x61, 0x03 };
            foreach (var n in streams)
            {
                if (n.CanWrite)
                {
                    await n.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }

        public static void Stop()
        {
            DataBrokerLeft.Esc();
            DataBrokerRight.Esc();
        }

        static void Listen(object index)
        {
            var indices = Convert.ToInt32(index);
            while (true)
            {
                try
                {
                    byte[] mes = new byte[4096];
                    int count = streams[indices].Read(mes, 0, mes.Length);
                    ProcessMessage(indices, mes, count);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    offset[0] = 0;
                    offset[1] = 0;
                }
                finally
                {

                }
            }
        }

        static byte[][] segments = new byte[DEVICECOUNT][];
        static int[] offset = new int[DEVICECOUNT];

        static void ProcessMessage(int index, byte[] buffer, int len)
        {
            var currentLength = offset[index] + len;
            Array.Copy(buffer, 0, segments[index], offset[index], len);
            int restIndex = Decode(index, segments[index], 0, currentLength);
            if (currentLength - restIndex != 0)
            {
                Array.Copy(segments[index], restIndex, segments[index], 0, currentLength);
                offset[index] = currentLength - restIndex;
            }
            else
                offset[index] = 0;
            if (offset[index] > 768 * 1024)
            {
                offset[index] = 0;
            }
            //Console.WriteLine(offset[index]);
        }

        private static int Decode(int index, byte[] buffer, int startIndex, int len)
        {
            int dataProcessedIndex = 0;
            var overallSize = startIndex + offset[index] + len;
            for (int i = startIndex; i < overallSize; i++)
            {
                if (buffer[i] == 0x02)
                {
                    for (int j = i; j < overallSize; j++)
                    {
                        if (buffer[j] == 0x03)
                        {
                            dataProcessedIndex = i;
                            var str = Encoding.ASCII.GetString(buffer, i, j - i + 1);
                            Task.Factory.StartNew(ParsePacket, Tuple.Create(index, str));
                            return Decode(index, buffer, j + 1, overallSize - (j + 1));
                        }
                    }
                }
            }
            return startIndex;
        }

        private static void ParsePacket(object str)
        {
            List<SpatialPoint> spList = new List<SpatialPoint>();
            var indices = (str as Tuple<int, string>).Item1;
            var datagram = (str as Tuple<int, string>).Item2;
            if (datagram.IndexOf("sRA LMDscandata") < 0)
                return;

            datagram = datagram.Substring(datagram.IndexOf("sRA LMDscandata"));
            //分解数据
            String[] parts = datagram.Split(' ');

            var ts = (DateTime.Now - TimeStamp).TotalSeconds;
            var y = ts * Speed;

            try
            {
                String test1 = parts[23];
                String test2 = parts[24];
                String test3 = parts[25];
            }
            catch (Exception)
            {
                // TODO: handle exception
                return;
            }
            string startAngelStr = parts[23];
            string stepStr = parts[24];
            string numStr = parts[25];
            int startAngel1 = int.Parse(startAngelStr, System.Globalization.NumberStyles.HexNumber) / 10000;
            //BigInteger startAngel = new BigInteger(startAngelStr, 16).divide(new BigInteger(10000 + ""));//6DDD0//FFFF3CB0
            float step = int.Parse(stepStr, NumberStyles.HexNumber) / 10000f;
            int num = int.Parse(numStr, NumberStyles.HexNumber);

            for (int index = 26; index < (26 + num) && index < parts.Length - 1; index++)
            {
                String aDataItem = parts[index];
                float distance = int.Parse(aDataItem, NumberStyles.HexNumber) / 1000f;
                float angle = startAngel1 + (index - 26) * step;
                if (distance == 0.0)
                {
                    continue;
                }
                //pointList.Add(new OriginPoint(distance, angel));
                spList.Add(Format(indices, distance, angle, y));
            }
            if (spList.Count < 170)
            {
                return;
            }
            else
            {
                foreach (var n in spList)
                {
                    if (n.X > 1.82 && n.X < 2.57)
                    {
                        //LEFT
                        DataBrokerLeft.Enqueue(n);
                    }
                    else if (n.X > 4.58 && n.X < 5.88)
                    {
                        //RIGHT
                        DataBrokerRight.Enqueue(n);
                    }
                }
            }
        }

        static SpatialPoint Format(OriginPoint op, double x)
        {
            float deltaY = 43;
            float totalH = 3.2f;
            var y = deltaY + op.Distance * Math.Cos(op.Angle);
            var z = totalH - op.Distance * Math.Sin(op.Angle);
            return new SpatialPoint() { X = x, Y = y, Z = z };
        }

        static SpatialPoint Format(int index, double distance, double angle, double y)
        {
            float deltaXLeft = 1.82f;
            float deltaXRight = 4.96f;
            float totalH = 2.25f;
            double x = 0;
            if (index == 0)
            {
                //left
                x = deltaXLeft + distance * Math.Cos(angle);
            }
            else
            {
                x = deltaXRight + distance * Math.Cos(angle);
            }
            var z = totalH - distance * Math.Sin(angle);
            return new SpatialPoint() { X = x, Y = y, Z = z };
        }

        public static double Speed { get; set; }
        public static DateTime TimeStamp { get; set; }
        public static DataBroker DataBrokerLeft { get; set; }
        public static DataBroker DataBrokerRight { get; set; }
        public static string Folder { get; set; } = "data\\";

        public static string Init(double distance, double totalSecond)
        {
            Speed = distance / totalSecond;
            TimeStamp = DateTime.Now;
            System.IO.Directory.CreateDirectory(Folder);
            Folder = Folder + TimeStamp.ToString("yyyyMMdd");
            System.IO.Directory.CreateDirectory(Folder);
            DataBrokerLeft = new DataBroker(Folder + "ScanL " + DateTime.Now.ToString("yyyyMMddHHmmss") + ".OBJ");
            DataBrokerRight = new DataBroker(Folder + "ScanR " + DateTime.Now.ToString("yyyyMMddHHmmss") + ".OBJ");


            for (int i = 0; i < DEVICECOUNT; i++)
            {
                segments[i] = new byte[1024 * 1024];
                clients[i] = new TcpClient("192.168.1.2" + (i + 1), 2112);
                streams[i] = clients[i].GetStream();
                Thread th0 = new Thread(Listen);
                th0.Name = "ListenThread" + i;
                th0.IsBackground = true;
                th0.Start(i);
            }

            return Folder;
        }

    }
}
