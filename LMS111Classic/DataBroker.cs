using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LMS111Classic
{
    public class DataBroker
    {
        private Queue<SpatialPoint> queue = new Queue<SpatialPoint>();
        private string objFilename;

        System.Timers.Timer timer;

        public DataBroker(string objFile)
        {
            objFilename = objFile;
            timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public void Enqueue(SpatialPoint p)
        {
            queue.Enqueue(p);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            StringBuilder sb = new StringBuilder();
            while (queue.Count != 0)
            {
                var data = queue.Dequeue();
                sb.Append("v " + data.X + " ");
                sb.Append(data.Y + " ");
                sb.Append(data.Z + System.Environment.NewLine);
            }
            File.AppendAllText(objFilename, sb.ToString());
            if (timer != null)
            {
                timer.Start();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Write timer error!");
            }

        }

        public void Esc()
        {
            timer.Stop();
            timer = null;
        }
    }
}
