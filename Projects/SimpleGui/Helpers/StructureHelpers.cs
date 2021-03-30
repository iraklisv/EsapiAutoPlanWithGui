using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Image = VMS.TPS.Common.Model.API.Image;
using Point = System.Windows.Point;

namespace SimpleGui.Helpers
{
    public class StructureHelpers
    {
        //public static readonly AxisAlignedMargins ringMargin = new AxisAlignedMargins(StructureMarginGeometry.Outer, 20, 20, 6, 20, 20, 6);
        public static Structure CopyStructureInBounds(Structure copy, Structure orig, Image im, (double LowerZBound, double UpperZBound) zBounds)
        {
            if (orig != null)
            {
                for (int z = 0; z < im.ZSize; z++)
                {
                    var zPos = im.Origin.z + z * im.ZRes;
                    if (zPos >= zBounds.LowerZBound && zPos <= zBounds.UpperZBound)
                    {
                        copy.ClearAllContoursOnImagePlane(z);
                        var zContour = orig.GetContoursOnImagePlane(z);
                        zContour.ToList().ForEach(c => copy.AddContourOnImagePlane(c, z));
                    }

                }
            }
            return orig;
        }

        public static bool checkIfStructureIsNotOk(Structure p)
        {
            if (p == null) { MessageBox.Show("Structure Is Null!"); return true; }
            if (p.IsEmpty) { MessageBox.Show($"{p.Id} is emtpy!"); return true; }
            return false;
        }

        public static void clearAllContours(Structure p, StructureSet ss)
        {
            int slicesInImage = ss.Image.ZSize;
            for (int z = 0; z < slicesInImage; z++)
                p.ClearAllContoursOnImagePlane(z);
        }
        public static double getLowerYedgeOfContours(List<List<Point>> contours)
        {
            if (contours == null) return double.NaN;
            else
            {
                double lowerYedge = getLowerYedgeOfContour(contours.FirstOrDefault());
                foreach (var p in contours)
                {
                    var low = getLowerYedgeOfContour(p);
                    if (low < lowerYedge) lowerYedge = low;
                }
                return (lowerYedge);
            }
        }

        public static double getLowerYedgeOfContour(List<Point> contour)
        {
            if (contour == null) return double.NaN;
            else
            {
                double lowerYedge = contour.FirstOrDefault().Y;
                foreach (var p in contour)
                {
                    if (p.Y < lowerYedge) lowerYedge = p.Y;
                }
                return (lowerYedge);
            }
        }
        public static List<Structure> GenerateIntermediatePTVs(List<Structure> PTVse, Structure PTVe, List<KeyValuePair<string, double>> presc, StructureSet ss, Structure Body, bool doCrop)
        {
            List<Structure> PTVinters = new List<Structure>();
            foreach (var p in PTVse.TakeWhile(x => x != PTVse.Last()))
            {
                var txt = $"{p.Id}i";
                var origId = Strings.cropFirstNChar(p.Id, 2); origId = Strings.cropLastNChar(origId, 1);
                var pLevelPrescription = presc.First(x => x.Key == origId);
                Structure iLevel = StructureHelpers.createStructureIfNotExisting(txt, ss, "PTV");
                StructureHelpers.clearAllContours(iLevel, ss);
                iLevel.SegmentVolume = p.Margin(7);
                iLevel.SegmentVolume = iLevel.And(PTVe);
                iLevel.SegmentVolume = iLevel.Sub(p);
                if (doCrop) iLevel.SegmentVolume = iLevel.And(Body);
                PTVinters.Add(iLevel);
            }
            return PTVinters;
        }

        public static List<Structure> CleanIntermediatePTVs(List<Structure> PTVse, List<Structure> PTVinters, List<KeyValuePair<string, double>> presc)
        {
            foreach (var p in PTVinters)
            {
                var origId = Strings.cropFirstNChar(p.Id, 2);
                origId = Strings.cropLastNChar(origId, 2);
                var pLevelPrescription = presc.First(x => x.Key == origId);
                foreach (var t in PTVse)
                {
                    var tId = Strings.cropFirstNChar(t.Id, 2);
                    tId = Strings.cropLastNChar(tId, 1);
                    var tLevelPrescription = presc.First(x => x.Key.Equals(tId));
                    if (pLevelPrescription.Value > tLevelPrescription.Value)
                        t.SegmentVolume = t.Sub(p);
                    else
                        p.SegmentVolume = p.Sub(t);
                }
                // now remove intersetions of ptv inters
                foreach (var t in PTVinters.AsEnumerable().Reverse().TakeWhile(x => x != p))
                    t.SegmentVolume = t.Sub(p);
            }
            PTVinters = PTVinters.Where(s => !s.IsEmpty).ToList();
            return PTVinters;
        }


        public static Structure createStructureIfNotExisting(string structName, StructureSet ss, string dicomType)
        {
            var whateverStructure = getStructureFromStructureSet(structName, ss, true);
            if (whateverStructure == null)
                return ss.AddStructure(dicomType, structName);
            else
            {
                clearAllContours(whateverStructure, ss);
                return whateverStructure;
            }
        }
        public static Structure getStructureFromStructureSet(string structName, StructureSet ss, bool isExactName)
        {

            Structure structure = null;
            if (isExactName)
                structure = ss.Structures.FirstOrDefault(x => x.Id.Equals(structName));
            else structure = ss.Structures.FirstOrDefault(x => x.Id.ToLower().Contains(structName));
            switch (structure)
            {
                case null:
                    return null;
                default:
                    return structure;
            }
        }
        public static void cropAwayFromBody(StructureSet ss, Structure body, Structure operateOn, double cropDistance)
        {
            var dummy = getStructureFromStructureSet("dummy", ss, true);
            if (dummy == null) dummy = ss.AddStructure("Control", "dummy");
            dummy.SegmentVolume = operateOn.Margin(cropDistance * 1.2);
            dummy.SegmentVolume = dummy.Sub(body);
            dummy.SegmentVolume = dummy.Margin(cropDistance);
            operateOn.SegmentVolume = operateOn.Sub(dummy);
            ss.RemoveStructure(dummy);
        }
        public static void cropAwayFromStructure(StructureSet ss, Structure cropAwayFrom, Structure operateOn, double cropDistance)
        {
            // negative value accepted, it shrinks the volume
            var dummy1 = getStructureFromStructureSet("dummy1", ss, true);
            if (dummy1 == null) dummy1 = ss.AddStructure("Control", "dummy1");
            dummy1.SegmentVolume = cropAwayFrom.Margin(cropDistance);
            operateOn.SegmentVolume = operateOn.Sub(dummy1);
            ss.RemoveStructure(dummy1);
        }
        public static void cropAwayFromStructure(StructureSet ss, Structure cropAwayFrom, Structure operateOn, bool isOuterMargin, double x1, double x2, double y1, double y2, double z1, double z2)
        { // trying to implement quick and dirty trick to increase the margin more than fixed 50mm, assuming assymetric x1=x2=y1=y2
            // negative value are not accepted
            if (x1 < 0 || x2 < 0 || y1 < 0 || y2 < 0 || z1 < 0 || z2 < 0)
            {
                MessageBox.Show("do not use negative margins for assymetric margin", "", MessageBoxButton.OK, MessageBoxImage.Error);
                //return null;
            }
            var dummy1 = getStructureFromStructureSet("dummy1", ss, false);
            if (dummy1 == null) dummy1 = ss.AddStructure("Control", "dummy1");
            if (x1 >= 50)
            {
                Structure extendedFix = getStructureFromStructureSet("extendedfix", ss, true);
                if (extendedFix == null)
                {
                    var tmpmargins = new AxisAlignedMargins(StructureMarginGeometry.Outer, 50, 50, 0, 50, 50, 0);
                    extendedFix = ss.AddStructure("CONTROL", "extendedfix");
                    SegmentVolume extendedFixSegment = cropAwayFrom.Margin(marginInMM: 0);
                    extendedFix.SegmentVolume = extendedFixSegment;
                    extendedFix.SegmentVolume = extendedFix.AsymmetricMargin(tmpmargins);
                }
                x1 = x2 = y1 = y2 = x1 - 50;
                AxisAlignedMargins someMargins = new AxisAlignedMargins(StructureMarginGeometry.Outer, x1, y1, z1, x2, y2, z2);
                if (!isOuterMargin) someMargins = new AxisAlignedMargins(StructureMarginGeometry.Inner, x1, y1, z1, x2, y2, z2);
                dummy1.SegmentVolume = extendedFix.AsymmetricMargin(someMargins);
                operateOn.SegmentVolume = operateOn.Sub(dummy1);
                ss.RemoveStructure(dummy1);
            }
            else
            {
                AxisAlignedMargins someMargins = new AxisAlignedMargins(StructureMarginGeometry.Outer, x1, y1, z1, x2, y2, z2);
                if (!isOuterMargin) someMargins = new AxisAlignedMargins(StructureMarginGeometry.Inner, x1, y1, z1, x2, y2, z2);
                dummy1.SegmentVolume = cropAwayFrom.AsymmetricMargin(someMargins);
                operateOn.SegmentVolume = operateOn.Sub(dummy1);
                ss.RemoveStructure(dummy1);
            }
        }

        public static void ClearAllOptimizationContours(StructureSet ss)
        {
            foreach (var p in ss.Structures.ToList())
                if (p.Id.StartsWith("0_")) ss.RemoveStructure(p);
        }

        public static void ClearAllEmtpyOptimizationContours(StructureSet ss)
        {
            // pay special attention as this theoretically can make some lists items absolete and lead to crash
            foreach (var p in ss.Structures.ToList().FindAll(x => x.Id.Contains("0_")))
                if (p.IsEmpty) ss.RemoveStructure(p);
        }

        public static List<Structure> CreatePTVsEval(List<Structure> PTVs, StructureSet ss, Structure Body, bool doCrop)
        {
            var list = new List<Structure>();
            foreach (var p in PTVs)
            {
                if (p.Id.Length > 12)
                {
                    MessageBox.Show($"Name {p.Id} is too long (max is 12 chars)");
                    return null;
                }
                var txt = $"0_{p.Id}e";
                //if (txt.ToLower() != "0_ptve")// this means name of one of the ptvs is "PTV" and it conflicts with 0_PTVe created manually, duct tape solution
                //{
                var tmp = StructureHelpers.createStructureIfNotExisting(txt, ss, "PTV");
                if (doCrop)
                    tmp.SegmentVolume = p.And(Body);
                else
                    tmp.SegmentVolume = p.Margin(0); // if not cropping make a copy
                list.Add(tmp);
                //}
                //else
                //{
                //    var tmp = ss.Structures.FirstOrDefault(x => x.Id.ToLower().Equals("0_ptve"));
                //    list.Add(tmp);
                //} // duct tape solution, just add 0_ptve to ptvs list
            }

            foreach (var p in list)
            {
                // iterate from last element to first one and cut smaller prescription ptv away from higher one
                foreach (var t in list.AsEnumerable().Reverse().TakeWhile(x => x != p))
                    t.SegmentVolume = t.Sub(p);
            }
            return list;
        }
        public static List<Structure> CreateRings(List<Structure> PTVse, StructureSet ss, Structure Body, Structure PTVe3mm, double margin)
        {
            var list = new List<Structure>();
            foreach (var p in PTVse)
            {
                var originalPTV = Strings.cropLastNChar(p.Id, 1);
                originalPTV = Strings.cropFirstNChar(originalPTV, 2);
                var txt = $"0_R_{originalPTV}";
                var str = StructureHelpers.createStructureIfNotExisting(txt, ss, "CONTROL");
                AxisAlignedMargins ringMargin = new AxisAlignedMargins(StructureMarginGeometry.Outer, margin, margin, 6, margin, margin, 6);
                str.SegmentVolume = p.AsymmetricMargin(ringMargin);
                str.SegmentVolume = str.And(Body);
                str.SegmentVolume = str.Sub(PTVe3mm);
                //var allExceptLast = Rings.Skip(Rings.IndexOf(str));
                foreach (var r in list) str.SegmentVolume = str.Sub(r);
                //foreach (var r in listOfOars) str.SegmentVolume = str.Sub(r);
                if (!str.IsEmpty) list.Add(str);
            }
            return list;
        }

        public static List<Structure> CreateRingsForBreastSIB(List<Structure> PTVse, List<Structure> listOfOars, StructureSet ss, Structure Body, Structure PTVe3mm, double margin, List<string> boostList)
        {
            var list = new List<Structure>();
            foreach (var p in PTVse)
            {
                var originalPTV = Strings.cropLastNChar(p.Id, 1);
                originalPTV = Strings.cropFirstNChar(originalPTV, 2);
                var txt = $"0_R_{originalPTV}";
                var str = StructureHelpers.createStructureIfNotExisting(txt, ss, "CONTROL");
                double useThisMargin = margin;
                foreach (var boost in boostList)
                    if (originalPTV == boost)
                        useThisMargin = 6;
                AxisAlignedMargins ringMargin = new AxisAlignedMargins(StructureMarginGeometry.Outer, useThisMargin, useThisMargin, 6, useThisMargin, useThisMargin, 6);
                str.SegmentVolume = p.AsymmetricMargin(ringMargin);
                str.SegmentVolume = str.And(Body);
                str.SegmentVolume = str.Sub(PTVe3mm);
                //var allExceptLast = Rings.Skip(Rings.IndexOf(str));
                foreach (var r in list) str.SegmentVolume = str.Sub(r);
                //foreach (var r in listOfOars) str.SegmentVolume = str.Sub(r);
                if (!str.IsEmpty) list.Add(str);
            }
            return list;
        }

        public static double calculatePolygonArea(VVector[] c)
        {
            double area = 0;
            int numOfPoints = c.Length;
            for (int i = 0; i < numOfPoints - 1; i++)
            {
                area +=
                    (c[i + 1].x - c[i].x) *// this is width
                    (c[i + 1].y + c[i].y) / 2; //average height
            }
            return Math.Abs(area);
        }
        public static double calculateAreaAboveCoordinateY(List<Point> c, double aboveY)
        {
            double area = 0;

            int numOfPoints = c.Count;
            for (int i = 0; i < numOfPoints - 1; i++)
            {
                var thisY = c[i].Y - aboveY;
                var nextY = c[i + 1].Y - aboveY;
                if (thisY >= 0 && nextY >= 0)
                    area +=
                    (c[i + 1].X - c[i].X) *// this is width
                    (thisY + nextY) / 2;
            }
            return Math.Abs(area); // because we pick only negative Y, area will be negative, return abs..
        }
        public static double calculateAreaAboveCoordinateYAndShade(List<Point> c, double aboveY, Bitmap b, double scaleX, double scaleY, double minX, double minY, Graphics g) // here the Y coordinate is negative to anterior side of patient! so in reality on need Y not more, but less
        {
            double area = 0;

            int numOfPoints = c.Count;
            for (int i = 0; i < numOfPoints - 1; i++)
            {
                // remembet positive y is posterior side of pacient!
                double binWidth = c[i + 1].X - c[i].X;
                var thisY = c[i].Y - aboveY;
                var nextY = c[i + 1].Y - aboveY;
                if (thisY <= 0 && nextY <= 0)
                {
                    area +=
                    binWidth * (thisY + nextY) / 2;
                    double xMidPoint = ((c[i + 1].X + c[i].X) / 2 - minX) * scaleX;
                    double yMidPoint = ((c[i + 1].Y + c[i].Y) / 2 - minY) * scaleY;

                    int aveX = (int)(xMidPoint);
                    int aveY = (int)(yMidPoint);
                    var aboveYscaled = (int)((aboveY - minY) * scaleY);
                    Pen penBlack = new Pen(Color.Black, 8);
                    Pen penWhite = new Pen(Color.White, 8);
                    PointF p1 = new PointF(aveX, aveY);
                    PointF p2 = new PointF(aveX, aboveYscaled);
                    if (binWidth < 0) g.DrawLine(penWhite, p1, p2);
                    else g.DrawLine(penBlack, p1, p2);
                    //b.SetPixel(aveX, aveY, Color.Black);
                }

            }
            return Math.Abs(area);
        }

        public static VVector findCenter(VVector[] c)
        {
            double _x = 0;
            double _y = 0;
            double _z = 0;
            int numOfPoints = c.Length;
            for (int i = 0; i < numOfPoints; i++)
            {
                _x += c[i].x;
                _y += c[i].y;
                _z += c[i].z;
            }
            _x = _x / Convert.ToDouble(numOfPoints);
            _y = _y / Convert.ToDouble(numOfPoints);
            _z = _z / Convert.ToDouble(numOfPoints);
            VVector midPoint = new VVector(_x, _y, _z);
            return midPoint;
        }


        //public static VVector getCenter
        public static void removeSmallParts(Structure operateOn, StructureSet ss, double removeSmallerThan)
        {
            int slicesInImage = ss.Image.ZSize;

            for (int z = 0; z < slicesInImage; z++)
            {
                List<VVector[]> contoursOnPlane = operateOn.GetContoursOnImagePlane(z).ToList();
                List<VVector[]> tempContours = new List<VVector[]>();
                if (contoursOnPlane.Count > 0)
                {
                    foreach (VVector[] contour in contoursOnPlane)
                    {
                        double area = calculatePolygonArea(contour);
                        if (area >= removeSmallerThan) // clear contour if its less than 2cm2
                            tempContours.Add(contour);
                    }
                    operateOn.ClearAllContoursOnImagePlane(z);
                    foreach (VVector[] contour in tempContours)
                        operateOn.AddContourOnImagePlane(contour, z);
                }
            }
        }

        public static void seperateHotSpots(Structure SeperateThis, Structure nearLungContours, Structure ant, Structure post, StructureSet ss)
        {
            int slicesInImage = ss.Image.ZSize;

            for (int z = 0; z < slicesInImage; z++)
            {
                List<VVector[]> contoursOnPlane = SeperateThis.GetContoursOnImagePlane(z).ToList();
                if (contoursOnPlane.Count > 0)
                {
                    foreach (VVector[] contour in contoursOnPlane)
                    {
                        var center = findCenter(contour);
                        if (nearLungContours.IsPointInsideSegment(center)) post.AddContourOnImagePlane(contour, z);
                        else ant.AddContourOnImagePlane(contour, z);
                    }
                }
            }
        }

        //public static void MergeSameLevelTargets(List<KeyValuePair<string, double>> presc, StructureSet ss)
        //{
        //    // must be sorted!
        //    foreach (var p in presc.TakeWhile(x => x.Key!=presc.Last().Key))
        //    {
        //        var nextKeyValuePair = presc.SkipWhile(x => x.Key == p.Key).Skip(1).FirstOrDefault();
        //        if (p.Value == nextKeyValuePair.Value)
        //        {
        //            var thisStructure = getStructureFromStructureSet(p.Key, ss, true);
        //            var nextStructure = getStructureFromStructureSet(nextKeyValuePair.Key, ss, true);
        //            thisStructure.SegmentVolume = thisStructure.Or(nextStructure);
        //        }
        //    }
        //}
    }
}