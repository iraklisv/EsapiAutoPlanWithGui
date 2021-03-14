using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SimpleGui.Helpers
{
    class PlanarHelpers
    {
        public static double findCollimatorAngle(List<Point> contour, double GantryAngle, string SelectedBreastSide, bool isMedialField)
        {
            List<double> angles = new List<double>();
            double weightedSum = 0;
            double sumOfWeights = 0;

            foreach (var p1 in contour)
            {
                var p2 = contour.SkipWhile(s => s != p1).Skip(1).DefaultIfEmpty(new Point(double.NaN, double.NaN)).FirstOrDefault(); // get second point in target contour
                if (p1 == contour.Last()) p2 = contour.First(); // closed poligon

                var Angle = getAngleForSegment(p1, p2);
                var len = getLength(p1, p2);
                int weight = (int)(len / 0.1);
                // len is weight, ok?
                if ((SelectedBreastSide == "Left" && isMedialField) ||
                    (SelectedBreastSide == "Right" && !isMedialField))
                    if (Angle >= 270)
                    {
                        weightedSum += len * Angle;
                        sumOfWeights += len;
                    }
                if ((SelectedBreastSide == "Left" && !isMedialField) ||
                    (SelectedBreastSide == "Right" && isMedialField))
                    if (Angle <= 90)
                    {
                        weightedSum += len * Angle;
                        sumOfWeights += len;
                    }
                //for (int i = 0; i < weight; i++)
                //    angles.Add(Angle);
            }

            double OptimalAngle = weightedSum / sumOfWeights;
            if (double.IsNaN(OptimalAngle)) OptimalAngle = 0;
            
            //var histogram = new ScottPlot.Histogram(angles.ToArray(), min: 0, max: 360, binSize: 1);
            //var plt1 = new ScottPlot.Plot(600, 600);
            //plt1.PlotBar(histogram.bins, histogram.counts, barWidth: 1);
            //string filePath = @"C:\Users\Varian\Desktop\DEBUG\CollimatorOptimization\";
            //string fileName = string.Format("AngleHistogram_{0}", GantryAngle.ToString());
            //plt1.SaveFig(filePath + fileName + ".png");

            //string yourPointsFile = filePath + fileName + ".xml";
            //XmlSerializer xmls = new XmlSerializer(typeof(List<Point>));
            //using (Stream writer = new FileStream(yourPointsFile, FileMode.Create))
            //{
            //    xmls.Serialize(writer, contour);
            //    writer.Close();
            //}

            return OptimalAngle;
        }
        private static double getAngleForSegment(Point a, Point b)
        {
            var delX = b.X - a.X;
            var delY = b.Y - a.Y;
            var div = delX / delY; // becuase col=0 is along Y axis
            double theAngle = (180 / Math.PI) * Math.Atan(div); // degrees
            //divA = 90 - divA;
            //if (delX < 0) divA += 180;
            //if (divA > 90 && divA < 270) divA += 180;
            //if (divA >= 360) divA -= 360;
            if (theAngle < 0) theAngle += 360;
            return theAngle;
        }
        private static double getLength(Point a, Point b)
        {
            var delX = b.X - a.X;
            var delY = b.Y - a.Y;
            return Math.Sqrt(delX * delX + delY * delY);
        }

        public static Point findPointWithLowestYForContour(List<Point> contour)
        {
            double lowestY = 100000000;
            Point pointWithLowestY = new Point(double.NaN, double.NaN);
            foreach (var p in contour)
            {
                if (p.Y < lowestY)
                {
                    lowestY = p.Y;
                    pointWithLowestY = p;
                }
            }
            return pointWithLowestY;
        }
        public static Point findPointWithLowestYForContours(List<List<Point>> contours)
        {
            double lowestY = 100000000;
            Point pointWithLowestY = new Point(double.NaN, double.NaN);
            foreach (var c in contours)
            {
                var lowestYpoint = findPointWithLowestYForContour(c);
                if (lowestYpoint.Y < lowestY)
                {
                    lowestY = lowestYpoint.Y;
                    pointWithLowestY = lowestYpoint;
                }
            }
            return pointWithLowestY;
        }


        public static double distanceBetweenTwoPoints(Point p1, Point p2)
        {
            return Math.Abs((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        }
        public static Tuple<Point, Point> findPairWithBiggestSeperattion(List<Point> points)
        {
            double max = -100000;
            Point maxp1 = new Point(double.NaN, double.NaN);
            Point maxp2 = new Point(double.NaN, double.NaN);

            foreach (var p1 in points)
            {
                foreach (var p2 in points)
                {
                    if (max < distanceBetweenTwoPoints(p1, p2))
                    {
                        max = distanceBetweenTwoPoints(p1, p2);
                        maxp1 = p1;
                        maxp2 = p2;
                    }
                }
            }
            return new Tuple<Point, Point>(maxp1, maxp2);
        }
        public static Point FindIntersection(Point s1, Point e1, Point s2, Point e2)
        {
            var a1 = e1.Y - s1.Y;
            var b1 = s1.X - e1.X;
            var c1 = a1 * s1.X + b1 * s1.Y;

            var a2 = e2.Y - s2.Y;
            var b2 = s2.X - e2.X;
            var c2 = a2 * s2.X + b2 * s2.Y;

            var delta = a1 * b2 - a2 * b1;
            //If lines are parallel, the result will be (NaN, NaN).
            return delta == 0 ? new Point(float.NaN, float.NaN)
                : new Point((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }

        // Given three colinear points p, q, r, the function checks if 
        // point q lies on line segment 'pr' 
        private static Boolean onSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;
            else return false;
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int orientation(Point p, Point q, Point r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
            // for details of below formula. 
            var val = (q.Y - p.Y) * (r.X - q.X) -
                    (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) return 0; // colinear 

            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }

        // The main function that returns true if line segment 'p1q1' 
        // and 'p2q2' intersect. 

        public static Boolean doIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            // Find the four orientations needed for general and 
            // special cases 
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;
            // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;
            // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;
            // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases 
        }

        // Driver code 
        //public static void Main(String[] args)
        //{
        //    Point p1 = new Point(1, 1);
        //    Point q1 = new Point(10, 1);
        //    Point p2 = new Point(1, 2);
        //    Point q2 = new Point(10, 2);

        //    if (doIntersect(p1, q1, p2, q2))
        //        Console.WriteLine("Yes");
        //    else
        //        Console.WriteLine("No");

        //    p1 = new Point(10, 1); q1 = new Point(0, 10);
        //    p2 = new Point(0, 0); q2 = new Point(10, 10);
        //    if (doIntersect(p1, q1, p2, q2))
        //        Console.WriteLine("Yes");
        //    else
        //        Console.WriteLine("No");

        //    p1 = new Point(-5, -5); q1 = new Point(0, 0);
        //    p2 = new Point(1, 1); q2 = new Point(10, 10); ;
        //    if (doIntersect(p1, q1, p2, q2))
        //        Console.WriteLine("Yes");
        //    else
        //        Console.WriteLine("No");
        //}
    }

    /* This code contributed by PrinciRaj1992 */

}
