namespace LMS111
{
    class OriginPoint
    {
        public OriginPoint(float distance, float angle)
        {
            Distance = distance;
            Angle = angle;
        }

        public float Distance { get; set; }
        public float Angle { get; set; }
    }

    public class SpatialPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}