using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LMS111Classic
{
    public class DataBroker
    {
        private ConcurrentQueue<SpatialPoint> queue = new ConcurrentQueue<SpatialPoint>();
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
            while (!queue.IsEmpty)
            {
                SpatialPoint spatialPoint;
                var result = queue.TryDequeue(out spatialPoint);
                if (!result)
                {
                    break;
                }
                sb.Append("v " + spatialPoint.X + " ");
                sb.Append(spatialPoint.Y + " ");
                sb.Append(spatialPoint.Z + Environment.NewLine);
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
