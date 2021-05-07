using System;
using System.Collections.Generic;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Drawing;
using Point = System.Windows.Point;
using ScottPlot;
using System.Configuration;
using SimpleGui.ConfigParser;

namespace SimpleGui.Helpers
{
    public class ContourParameters2D
    {
        public Point Center { get; private set; }
        public double Xmin { get; set; }
        public double Xmax { get; set; }
        public double Ymin { get; set; }
        public double Ymax { get; set; }

        public ContourParameters2D(Point center, double xmin, double xmax, double ymin, double ymax)
        {
            this.Center = center;
            this.Xmin = xmin;
            this.Xmax = xmax;
            this.Ymin = ymin;
            this.Ymax = ymax;
        }
    }
    public class BeamHelpers
    {
        public static readonly FitToStructureMargins margins5 = new FitToStructureMargins(5);
        public static readonly FitToStructureMargins margins10 = new FitToStructureMargins(10);
        public static readonly FitToStructureMargins LeftBreastFBmarginsMed = new FitToStructureMargins(5, 15, 20, 15);
        public static readonly FitToStructureMargins LeftBreastFBmarginsLat = new FitToStructureMargins(20, 15, 5, 15);
        public static readonly FitToStructureMargins RightBreastFBmarginsMed = new FitToStructureMargins(20, 15, 5, 15);
        public static readonly FitToStructureMargins RightBreastFBmarginsLat = new FitToStructureMargins(5, 15, 20, 15);
        public static readonly FitToStructureMargins margins0 = new FitToStructureMargins(0);
        public static readonly JawFitting jawFit = JawFitting.FitToRecommended;
        public static readonly OpenLeavesMeetingPoint olmp = OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Outside;
        public static readonly ClosedLeavesMeetingPoint clmp = ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_Center;
        public static readonly VRect<double> defaultJawPositions = new VRect<double>(-100, -100, 100, 100);

        public const string DVHEstimationAlgorithm = "DVH Estimation Algorithm [15.5.11]";
        public const int NumberOfIterationsForVMATOptimization = 2;
        public static readonly VRect<double> fs10x10 = new VRect<double>(-100, -100, 100, 100);
        public static Dictionary<int, Tuple<double, double>> mlc120IndexMappingX { get; } = new Dictionary<int, Tuple<double, double>>();

        public static void setConstraintsFromConfigurationFile(OptimizationSetup optSetup, Configuration config, double scale, Structure oar, string oarSection)
        {
            var section = config.GetSection(oarSection);
            if (section == null)
                MessageBox.Show(string.Format("{0} not found in configuration in {1}", oarSection, config.FilePath.ToString()));
            else
            {
                ConstraintElementCollection constraints = new ConstraintElementCollection();
                try { constraints = (section as ConstraintList).Constraints; }
                catch (NullReferenceException e) { MessageBox.Show(string.Format("{0} section is missing in configuration", oarSection)); }
                for (int i = 0; i < constraints.Count; i++)
                {
                    var thisC = constraints[i];
                    var typeC = thisC.Name;
                    var valueC = Convert.ToDouble(thisC.Value);
                    var volumeC = Convert.ToDouble(thisC.Volume);
                    var weightC = Convert.ToDouble(thisC.Weight);
                    if (typeC.ToLower().Contains("upper"))
                        SetOptimizationUpperObjectiveInGy(optSetup, oar, valueC, volumeC, weightC);
                    if (typeC.ToLower().Contains("lower"))
                        SetOptimizationLowerObjectiveInGy(optSetup, oar, valueC, volumeC, weightC);
                    if (typeC.ToLower().Contains("mean"))
                        SetOptimizationMeanObjectiveInGy(optSetup, oar, valueC, weightC);
                    //MessageBox.Show(string.Format("id {0} name {1} value {2:0.00} volume {3:0.00} weight {4:0.00}", thisC.Id, thisC.Name, Convert.ToDouble(thisC.Value), Convert.ToDouble(thisC.Volume), Convert.ToDouble(thisC.Weight)));
                }
            }
        }

        public static ContourParameters2D findContourParameters(List<Point> contour)
        {
            double minX = 10000;
            double maxX = -10000;
            double minY = 10000;
            double maxY = -10000;
            foreach (var p in contour)
            {
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
            }
            var center = new Point((minX + maxX) / 2, (minY + maxY) / 2);
            return new ContourParameters2D(center, minX, maxX, minY, maxY);
        }

        public static void setY1OfStaticField(Beam field, double y1)
        {
            var pars = field.GetEditableParameters();
            var jawPositions = pars.ControlPoints.First().JawPositions;
            pars.SetJawPositions(new VRect<double>(jawPositions.X1, -y1, jawPositions.X2, jawPositions.Y2));
            field.ApplyParameters(pars);
        }
        public static void setY2OfStaticField(Beam field, double y2)
        {
            var pars = field.GetEditableParameters();
            var jawPositions = pars.ControlPoints.First().JawPositions;
            pars.SetJawPositions(new VRect<double>(jawPositions.X1, jawPositions.Y1, jawPositions.X2, y2));
            field.ApplyParameters(pars);
        }
        public static void setX1OfStaticField(Beam field, double x1)
        {
            var pars = field.GetEditableParameters();
            var jawPositions = pars.ControlPoints.First().JawPositions;
            pars.SetJawPositions(new VRect<double>(-x1, jawPositions.Y1, jawPositions.X2, jawPositions.Y2));
            //pars.SetJawPositions(new VRect<double>(jawPositions.X1, jawPositions.Y1, jawPositions.X2, jawPositions.Y2));
            field.ApplyParameters(pars);
        }
        public static void setX2OfStaticField(Beam field, double x2)
        {
            var pars = field.GetEditableParameters();
            var jawPositions = pars.ControlPoints.First().JawPositions;
            pars.SetJawPositions(new VRect<double>(jawPositions.X1, jawPositions.Y1, x2, jawPositions.Y2));
            //pars.SetJawPositions(new VRect<double>(jawPositions.X1, jawPositions.Y1, jawPositions.X2, jawPositions.Y2));
            field.ApplyParameters(pars);
        }
        public static Beam optimizeCollimator(double beamAngle, Structure target, Structure lung, StructureSet ss, ExternalPlanSetup eps, ExternalBeamMachineParameters machinePars, VVector iso, double startCA, double endCA, double stepCA, string SelectedBreastSide, bool isMedialField, double shiftAmount, bool hasMLC)
        {
            if (beamAngle >= 360) beamAngle -= 360;
            if (beamAngle < 0) beamAngle += 360;
            var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, iso, ss, target, lung, beamAngle, startCA, endCA, stepCA, SelectedBreastSide, isMedialField); // get optimal angle

            Beam newBeam;
            if (hasMLC) newBeam = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), ColAndJaw.Item1, beamAngle, 0, iso);
            else newBeam = eps.AddStaticBeam(machinePars, new VRect<double>(-100, -100, 100, 100), ColAndJaw.Item1, beamAngle, 0, iso);
            if (SelectedBreastSide == "Left")
            {
                // for left breast 0 is medial field, 1 if for lateral field
                if (isMedialField) newBeam.FitCollimatorToStructure(LeftBreastFBmarginsMed, target, true, true, false); // if
                else newBeam.FitCollimatorToStructure(LeftBreastFBmarginsLat, target, true, true, false); // here modify margins debending on which breast side and bank it is?
            }
            if (SelectedBreastSide == "Right")
            {
                // for right breast 1 is medial field, 0 if for lateral field
                if (isMedialField) newBeam.FitCollimatorToStructure(RightBreastFBmarginsMed, target, true, true, false); // if
                else newBeam.FitCollimatorToStructure(RightBreastFBmarginsLat, target, true, true, false); // here modify margins debending on which breast side and bank it is?
            }
            var pars = newBeam.GetEditableParameters();
            var currentJaws = pars.ControlPoints.FirstOrDefault().JawPositions;
            if (SelectedBreastSide == "Left" && isMedialField)
                pars.SetJawPositions(new VRect<double>(ColAndJaw.Item2 - shiftAmount, currentJaws.Y1, currentJaws.X2, currentJaws.Y2));
            if (SelectedBreastSide == "Left" && !isMedialField)
                pars.SetJawPositions(new VRect<double>(currentJaws.X1, currentJaws.Y1, ColAndJaw.Item2 + shiftAmount, currentJaws.Y2));
            if (SelectedBreastSide == "Right" && isMedialField)
                pars.SetJawPositions(new VRect<double>(currentJaws.X1, currentJaws.Y1, ColAndJaw.Item2 + shiftAmount, currentJaws.Y2));
            if (SelectedBreastSide == "Right" && !isMedialField)
                pars.SetJawPositions(new VRect<double>(ColAndJaw.Item2 - shiftAmount, currentJaws.Y1, currentJaws.X2, currentJaws.Y2));

            newBeam.ApplyParameters(pars);
            return newBeam;
        }


        public static void findMLCEdgeXAndInitiateMap()
        {
            for (int i = 0; i < 60; i++)
            {
                double leftEdge = double.NaN;
                double rightEdge = double.NaN;
                if (i < 10)
                {
                    leftEdge = -200 + i * 10;
                    rightEdge = -200 + (i + 1) * 10;
                }
                else if (i < 50)
                {
                    leftEdge = -100 + (i - 10) * 5;
                    rightEdge = -100 + (i - 10 + 1) * 5;
                }
                else
                {
                    leftEdge = -400 + i * 10;
                    rightEdge = -400 + (i + 1) * 10;
                }

                mlc120IndexMappingX.Add(i, new Tuple<double, double>(leftEdge, rightEdge));
            }
        }

        public static void SetOptimizationUpperObjectiveInGy(OptimizationSetup optSetup, Structure strct, double doseVale, double volume, double weight)
        {
            if (strct != null && !strct.IsEmpty)
                optSetup.AddPointObjective(strct, OptimizationObjectiveOperator.Upper, new DoseValue(doseVale, DoseValue.DoseUnit.Gy), volume, weight);
        }
        public static void SetOptimizationLowerObjectiveInGy(OptimizationSetup optSetup, Structure strct, double doseVale, double volume, double weight)
        {
            if (strct != null && !strct.IsEmpty)
                optSetup.AddPointObjective(strct, OptimizationObjectiveOperator.Lower, new DoseValue(doseVale, DoseValue.DoseUnit.Gy), volume, weight);
        }
        public static void SetOptimizationMeanObjectiveInGy(OptimizationSetup optSetup, Structure strct, double doseVale, double weight)
        {
            if (strct != null && !strct.IsEmpty)
                optSetup.AddMeanDoseObjective(strct, new DoseValue(doseVale, DoseValue.DoseUnit.Gy), weight);
        }

        public static void SetXJawsToCenter(Beam arc)
        {
            var tmp = arc.ControlPoints[0].JawPositions;
            var setJawsTo = new VRect<double>(-75, tmp.Y1, 75, tmp.Y2);
            var pars = arc.GetEditableParameters();
            pars.SetJawPositions(setJawsTo);
            arc.ApplyParameters(pars);
        }
        public static void fitArcJawsToTarget(Beam arc, StructureSet ss, Structure target, double startAngle, double stopAgnle, double step, double margin)
        {
            // dummy start/stop angles for now, fitting 360

            var iso = arc.IsocenterPosition;
            double collimatorAngle = arc.ControlPoints[0].CollimatorAngle;
            double smallestX1 = 1000;
            double biggestX2 = -1000;
            double smallestY1 = 1000;
            double biggestY2 = -1000;

            for (double angle = 0; angle < 360; angle += step)
            {
                var jawPositions = FitJawsToTarget(iso, ss, target, angle, collimatorAngle, margin);
                if (smallestX1 > jawPositions.X1) smallestX1 = jawPositions.X1;
                if (biggestX2 < jawPositions.X2) biggestX2 = jawPositions.X2;
                if (smallestY1 > jawPositions.Y1) smallestY1 = jawPositions.Y1;
                if (biggestY2 < jawPositions.Y2) biggestY2 = jawPositions.Y2;
                //MessageBox.Show(jawPositions.X1.ToString());
                //MessageBox.Show(jawPositions.X2.ToString());
            }
            if (smallestY1 < -200) smallestY1 = -200;
            if (biggestY2 > 200) biggestY2 = 200;
            if (arc.GantryDirection == GantryDirection.Clockwise) biggestX2 = smallestX1 + 150; // jaw X size should be <= than 15cm
            else smallestX1 = biggestX2 - 150;
            VRect<double> optimalJawSize = new VRect<double>(smallestX1, smallestY1, biggestX2, biggestY2);
            //if (checkJawPositions(optimalJawSize)) {
            BeamParameters pars = arc.GetEditableParameters();
            pars.SetJawPositions(optimalJawSize);
            arc.ApplyParameters(pars);
            //}
        }

        public static void SetTargetOptimization(OptimizationSetup optSetup, List<Structure> PTVse, List<KeyValuePair<string, double>> presc, int NOF)
        {
            foreach (var p in PTVse)
            {
                var originalPTV = Strings.cropLastNChar(p.Id, 1);
                originalPTV = Strings.cropFirstNChar(originalPTV, 2).ToLower();
                var thisDose = presc.FirstOrDefault(x => x.Key.ToLower().Equals(originalPTV)).Value * NOF;
                BeamHelpers.SetOptimizationLowerObjectiveInGy(optSetup, p, thisDose * 0.96D, 100, 100);
                BeamHelpers.SetOptimizationLowerObjectiveInGy(optSetup, p, thisDose * 1.00D, 096, 100);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, p, thisDose * 1.07D, 005, 100);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, p, thisDose * 1.08D, 000, 100);
            }
        }
        public static void SetTransitionRegiontOptimization(OptimizationSetup optSetup, List<Structure> PTVinters, List<KeyValuePair<string, double>> presc, int NOF)
        {
            foreach (var p in PTVinters)
            {
                var originalPTV = Strings.cropLastNChar(p.Id, 2);
                originalPTV = Strings.cropFirstNChar(originalPTV, 2);
                var maxDose = presc.FirstOrDefault(x => x.Key.Equals(originalPTV)).Value * NOF;
                var minDose = presc.SkipWhile(x => x.Key != originalPTV).Skip(1).FirstOrDefault().Value * NOF;
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, p, maxDose, 000, 100);
                BeamHelpers.SetOptimizationLowerObjectiveInGy(optSetup, p, minDose, 096, 100);
                BeamHelpers.SetOptimizationLowerObjectiveInGy(optSetup, p, minDose * 0.95D, 100, 100);
            }
        }
        public static void SetRingsOptimization(OptimizationSetup optSetup, List<Structure> Rings, List<KeyValuePair<string, double>> presc, int NOF, double scale)
        {
            foreach (var p in Rings)
            {
                var originalPTV = Strings.cropFirstNChar(p.Id, 4);
                var maxDose = presc.FirstOrDefault(x => x.Key.Equals(originalPTV)).Value * NOF;
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, p, maxDose * scale, 000, 100);
            }
        }
        public static VRect<double> FitJawsToTarget(VVector iso, StructureSet ss, Structure ptv, double angle, double colAngle, double margin)
        {
            var gantryAngleInRad = DegToRad(angle);
            var collimatorRotationInRad = DegToRad(colAngle);
            double xMin = 0;
            double yMin = 0;
            double xMax = 0;
            double yMax = 0;

            var nPlanes = ss.Image.ZSize;
            for (int z = 0; z < nPlanes; z++)
            {
                var contoursOnImagePlane = ptv.GetContoursOnImagePlane(z);
                if (contoursOnImagePlane != null && contoursOnImagePlane.Length > 0)
                {
                    foreach (var contour in contoursOnImagePlane)
                    {
                        AdjustJawSizeForContour(ref xMin, ref xMax, ref yMin, ref yMax, iso, contour, gantryAngleInRad, collimatorRotationInRad);
                    }
                }
            }
            //return new VRect<double>(xMin - margin, yMin - margin, xMax + margin, yMax + margin);
            return new VRect<double>(xMin - margin, yMin - 10, xMax + margin, yMax + 10);
        }
        private static void AdjustJawSizeForContour(ref double xMin, ref double xMax, ref double yMin, ref double yMax, VVector isocenter, IEnumerable<VVector> contour, double gantryRtnInRad, double collRtnInRad)
        {
            foreach (var point in contour)
            {
                var projection = ProjectToBeamEyeView(point, isocenter, gantryRtnInRad, collRtnInRad);
                var xCoord = projection.Item1;
                var yCoord = projection.Item2;

                // Update the coordinates for jaw positions.
                if (xCoord < xMin)
                {
                    xMin = xCoord;
                }

                if (xCoord > xMax)
                {
                    xMax = xCoord;
                }

                if (yCoord < yMin)
                {
                    yMin = yCoord;
                }

                if (yCoord > yMax)
                {
                    yMax = yCoord;
                }
            }
        }

        public static void BreastOptimizeCollimatorAndJawsForIMRT(Beam f, StructureSet ss, Structure target, Structure lung)
        {
            var gantryAngle = f.ControlPoints.FirstOrDefault().CollimatorAngle;
            var nPlanes = ss.Image.ZSize;
            List<Point> targetProjection = new List<Point>();
            List<Point> lungProjection = new List<Point>();

            //var aaa = f.GetStructureOutlines(lung, true); // crashes on 15.5

            for (int z = 0; z < nPlanes; z++)
            {
                var targetContours = target.GetContoursOnImagePlane(z);
                var lungContours = lung.GetContoursOnImagePlane(z);
                var gantryRad = DegToRad(gantryAngle);
                var collRad = DegToRad(f.ControlPoints.First().CollimatorAngle);

                foreach (var contour in targetContours)
                {
                    double minX = 1000;
                    double maxX = -1000;
                    double minY = 0;
                    double maxY = 0;
                    foreach (var p in contour)
                    {
                        var targetProjectionP = ProjectToBeamEyeView(p, f.IsocenterPosition, gantryRad, collRad);
                        if (minX > targetProjectionP.Item1)
                        {
                            minX = targetProjectionP.Item1;
                            minY = targetProjectionP.Item2;
                        }
                        if (maxX < targetProjectionP.Item1)
                        {
                            maxX = targetProjectionP.Item1;
                            maxY = targetProjectionP.Item2;
                        }
                    }
                    targetProjection.Add(new Point(minX, minY));
                    targetProjection.Add(new Point(maxX, maxY));
                }
                foreach (var contour in lungContours)
                {
                    double minX = 1000;
                    double maxX = -1000;
                    double minY = 0;
                    double maxY = 0;
                    foreach (var p in contour) // get only edges
                    {
                        var lungProjectionP = ProjectToBeamEyeView(p, f.IsocenterPosition, gantryRad, collRad);
                        if (minX > lungProjectionP.Item1)
                        {
                            minX = lungProjectionP.Item1;
                            minY = lungProjectionP.Item2;
                        }
                        if (maxX < lungProjectionP.Item1)
                        {
                            maxX = lungProjectionP.Item1;
                            maxY = lungProjectionP.Item2;
                        }
                    }
                    lungProjection.Add(new Point(minX, minY));
                    lungProjection.Add(new Point(maxX, maxY));
                }
            }
            //targetProjection = targetProjection.OrderBy(x=>x.X).ToList();
            targetProjection = targetProjection.OrderBy(x => x.Y).ToList();
            targetProjection.Count();
            //lungProjection = lungProjection.OrderBy(x=>x.X).ToList();
            lungProjection = lungProjection.OrderBy(x => x.Y).ToList();
            lungProjection.Count();
        }
        private static Tuple<double, double> ProjectToBeamEyeView(VVector point, VVector isocenter, double gantryRtnInRad, double collRtnInRad)
        {
            // Calculate coordinates with respect to isocenter location.
            var p = point - isocenter;

            // Calculate the components of a vector corresponding to beam direction (from isocenter toward source).
            var nx = Math.Cos(gantryRtnInRad - Math.PI / 2.0);
            var ny = Math.Sin(gantryRtnInRad - Math.PI / 2.0);

            // Calculate the projection of a contour point p on the plane orthogonal to beam direction such that collimator rotation is taken into account.
            var cosCollRtn = Math.Cos(collRtnInRad);
            var sinCollRtn = Math.Sin(collRtnInRad);
            var xCoord = cosCollRtn * (nx * p.y - ny * p.x) + sinCollRtn * p.z;
            var yCoord = sinCollRtn * (ny * p.x - nx * p.y) + cosCollRtn * p.z;

            return new Tuple<double, double>(xCoord, yCoord);
        }
        private static double DegToRad(double angle)
        {
            const double degToRad = Math.PI / 180.0D;
            return angle * degToRad;
        }
        public static string createFifName(ExternalPlanSetup ps, string masterFieldName)
        {
            List<char> chars = masterFieldName.ToList();
            chars.RemoveAt(chars.Count() - 1); // remove last char
            string tmpString = new string(chars.ToArray());
            int numberOfFields = ps.Beams.Count(x => x.Id.Contains(tmpString));
            string returnValue = string.Format("{0}{1}", tmpString, numberOfFields.ToString());
            return returnValue;
        }
        public static string getBaseName(string mainFieldName, int removeLastNchars)
        {
            List<char> chars = mainFieldName.ToList();
            chars.RemoveAt(chars.Count() - removeLastNchars); // remove last N chars
            return new string(chars.ToArray());
        }
        public static void substractFif(double fifWeight, Beam fif, Beam master)
        {
            BeamParameters fifPars = fif.GetEditableParameters();
            BeamParameters masterPars = master.GetEditableParameters();
            fifPars.WeightFactor = fifWeight;
            masterPars.WeightFactor -= fifWeight;
            fif.ApplyParameters(fifPars);
            master.ApplyParameters(masterPars);
        }


        public static DRRCalculationParameters drrCalcPars = new DRRCalculationParameters();
        public static DRRCalculationParameters breastDrrPars = new DRRCalculationParameters(400, 1, -100, 1000);
        public static DRRCalculationParameters boneDrrPars = new DRRCalculationParameters(400, 1, 300, 1000);
        public static void NormalizePlanToStructureCoverageRelAbs(PlanSetup ps,
                                                           Structure theStructure)
        {
            DoseValue coverage = ps.GetDoseAtVolume(theStructure, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute);
            MessageBox.Show(string.Format("target cr coverage was {0} Gy\ndose normalization value was {1}%", coverage.Dose.ToString("0.0"), ps.PlanNormalizationValue.ToString("0.0")), "", MessageBoxButton.OK, MessageBoxImage.Information);
            double normalization = ps.PlanNormalizationValue * coverage.Dose / 50;
            ps.PlanNormalizationValue = normalization;
            coverage = ps.GetDoseAtVolume(theStructure, 95, VolumePresentation.Relative, DoseValuePresentation.Absolute);
            MessageBox.Show(string.Format("target cr coverage now is {0} Gy\ndose normalization value now is {1}%", coverage.Dose.ToString("0.0"), ps.PlanNormalizationValue.ToString("0.0")), "", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public static List<double> measureProfileLocalMaximums(double[] size, PlanSetup plan, VVector profileStart, VVector profileEnd)
        {

            Dose d = plan.Dose;
            DoseProfile dp = d.GetDoseProfile(profileStart, profileEnd, size);

            string filename = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
            "\\profile.csv";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("Position,Dose");
                foreach (ProfilePoint pp in dp)
                {
                    //sw.WriteLine(String.Format("{0:0.00},{1:0.00}", pp.Position.Length-dp[0].Position.Length, pp.Value));
                    sw.WriteLine(string.Format("{0:0.00}\t{1:0.00}\t{2:0.00}\t{3:0.00}", pp.Position.x, pp.Position.y, pp.Position.z, pp.Value));

                }
            }
            List<double> maxProfileDoses = new List<double>();

            if (dp.Count() > 100)
            {
                for (int i = 10; i < dp.Count() - 10; i++)
                {
                    double currentValue = dp[i].Value;
                    if (!double.IsNaN(currentValue) && currentValue != 0)
                    {
                        bool isLocalMaximum = true;
                        for (int yy = 1; yy < 11; yy++)
                        {
                            if (currentValue < dp[i - yy].Value) { isLocalMaximum = false; }
                            if (currentValue < dp[i + yy].Value) { isLocalMaximum = false; }
                        }
                        if (isLocalMaximum)
                        {
                            maxProfileDoses.Add(dp[i].Value);
                        }
                    }
                }

            }
            return maxProfileDoses;

        }
        public static float calculateAreaWithinMLCOpen(float[,] field1, float[,] field2)
        {
            /*
             * field1 is leafpos for beam fit to the target, while field2 is fit to the lung
             */
            float areaMLC = 0;
            /* The positions of the beam collimator leaf pairs (in mm) in the IEC BEAMLIMITING DEVICE coordinate axis appropriate to the device type.
             * For example, the X-axis for MLCX and the Y-axis for MLCY. The two-dimensional array is indexed [bank, leaf] where the bank is either 0 or 1. 
             * Bank 0 represents the leaf bank to the negative MLC X direction, and bank 1 to the positive MLC X direction. If there is no MLC, a (0,0)-length array is returned. */
            for (int mlcIndex = 0; mlcIndex < 60; mlcIndex++)
            {
                float bankA = field1[0, mlcIndex];
                float bankB = field2[1, mlcIndex];
                if (bankB > bankA)
                {
                    //Console.WriteLine(bankA.ToString() +"\t"+ bankB.ToString());
                    float diff = bankB - bankA;
                    float mlcWidth = 5;
                    if (mlcIndex < 10 || mlcIndex > 49) { mlcWidth = 10; }
                    areaMLC += mlcWidth * diff;
                }
                //MessageBox.Show(string.Format("{0}{1}", bankA.ToString(), bankB.ToString()), "mlc area calc", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return areaMLC;
        }

        public static void DrawContourOnBitmap(List<Point> contour, Bitmap b, Color color, double scaleX, double scaleY, double shiftX, double shiftY, double AnetriorToThisLineY)
        {
            foreach (var p in contour)
            {
                int x = (int)((p.X - shiftX) * scaleX);
                int y = (int)((p.Y - shiftY) * scaleY);
                b.SetPixel(x, y, color);
            }
            int yy = (int)((AnetriorToThisLineY - shiftY) * scaleY);
            for (int i = 0; i < b.Width; i++)
                b.SetPixel(i, yy, Color.Blue);
        }

        public static double findBreastOptimalGantryAngleForMedialField(ExternalBeamMachineParameters machinePars, ExternalPlanSetup eps, StructureSet ss, Structure target, Structure lung, VVector isocenter, string side)
        {
            Point pivotPoint = new Point(isocenter.x, -isocenter.y); // look at place where UP is anterior side
            int slicesInImage = ss.Image.ZSize;
            double optimalGantryAngle = 0;
            List<KeyValuePair<double, double>> lungVolumesByGantry = new List<KeyValuePair<double, double>>();

            double startGA, endGA, stepSizeGA;
            startGA = endGA = 0;
            stepSizeGA = 0.5;
            if (side == "Left") { startGA = 290; endGA = 340; }
            if (side == "Right") { startGA = 20; endGA = 70; }

            for (double gantryAngle = startGA; gantryAngle < endGA; gantryAngle += stepSizeGA)
            //for (double gantryAngle = 270; gantryAngle < 360; gantryAngle += 10)
            {
                double lungVolumeInField = 0;
                for (int z = 0; z < slicesInImage; z++)
                {
                    List<VVector[]> targetcontours = target.GetContoursOnImagePlane(z).ToList();
                    List<VVector[]> lungipsicontours = lung.GetContoursOnImagePlane(z).ToList();
                    if (targetcontours.Count > 0 && lungipsicontours.Count > 0)
                    {

                        List<List<Point>> targetcontoursRotated = new List<List<Point>>();
                        List<List<Point>> lungipsicontoursRotated = new List<List<Point>>();
                        List<List<Point>> targetcontoursRotatedDiv = new List<List<Point>>();
                        List<List<Point>> lungipsicontoursRotatedDiv = new List<List<Point>>();

                        double rotationAngle = 0;
                        if (side == "Left") rotationAngle = gantryAngle + 90;
                        if (side == "Right") rotationAngle = gantryAngle - 90;
                        foreach (var targetcontour in targetcontours)
                        {
                            var cnt = new List<Point>();
                            foreach (var point in targetcontour)
                                cnt.Add(rotate2DvectorAroundPivot(new Point(point.x, -point.y), pivotPoint, DegToRad(rotationAngle)));
                            targetcontoursRotated.Add(cnt);
                        }
                        foreach (var lungcontour in lungipsicontours)
                        {
                            var cnt = new List<Point>();
                            foreach (var point in lungcontour)
                                cnt.Add(rotate2DvectorAroundPivot(new Point(point.x, -point.y), pivotPoint, DegToRad(rotationAngle))); // remember Y negative is posterior side, to draw things as in TPS put negative up
                            lungipsicontoursRotated.Add(cnt);
                        }


                        // here Y is positive toward anterior
                        // get lowest y point, also this is quite right thing to do, but should work to some extend
                        var pointWithLowestY = PlanarHelpers.findPointWithLowestYForContours(targetcontoursRotated);
                        // source position in this reference plane
                        Point sourceProjectionXY = new Point(isocenter.x, -isocenter.y);
                        if (side == "Left") sourceProjectionXY.X -= 1000; // if it is right breast, source projection is on the left (no patient, but given reference plane)
                        if (side == "Right") sourceProjectionXY.X += 1000; // if it is right breast, source projection is on the right (no patient, but given reference plane)
                        // to account for divergence of the beam, rotate (cw for left, ccw for right) by divergence angle
                        var delX = 1000 - (pointWithLowestY.X - isocenter.x);
                        var delY = pointWithLowestY.Y + isocenter.y;
                        double divA = (180 / Math.PI) * Math.Atan(delY / delX); // degrees

                        foreach (var targetcontour in targetcontoursRotated)
                        {
                            var cnt = new List<Point>();
                            foreach (var point in targetcontour)
                                cnt.Add(rotate2DvectorAroundPivot(new Point(point.X, -point.Y), pivotPoint, DegToRad(-divA)));
                            targetcontoursRotatedDiv.Add(cnt);
                        }
                        foreach (var lungcontour in lungipsicontoursRotated)
                        {
                            var cnt = new List<Point>();
                            foreach (var point in lungcontour)
                                cnt.Add(rotate2DvectorAroundPivot(new Point(point.X, -point.Y), pivotPoint, DegToRad(-divA))); // remember Y negative is posterior side, to draw things as in TPS put negative up
                            lungipsicontoursRotatedDiv.Add(cnt);
                        }


                        var lowerYedge = StructureHelpers.getLowerYedgeOfContours(targetcontoursRotatedDiv) - 5;

                        if (!double.IsNaN(lowerYedge))
                        {
                            double lungAreaInField = 0;

                            List<List<double>> lungsX = new List<List<double>>();
                            List<List<double>> lungsY = new List<List<double>>();
                            List<List<double>> targetsX = new List<List<double>>();
                            List<List<double>> targetsY = new List<List<double>>();

                            foreach (var lungcontour in lungipsicontoursRotatedDiv)
                            {
                                lungsX.Add(returnXorYlistFromListOfPoints(lungcontour, true));
                                lungsY.Add(returnXorYlistFromListOfPoints(lungcontour, false));
                            }
                            foreach (var targetcontour in targetcontoursRotatedDiv)
                            {
                                targetsX.Add(returnXorYlistFromListOfPoints(targetcontour, true));
                                targetsY.Add(returnXorYlistFromListOfPoints(targetcontour, false));
                            }

                            if (lungsX.Count > 0 && targetsX.Count > 0)
                            {
                                //if (z == 89)
                                //{
                                //    var plt = new ScottPlot.Plot(600, 600);
                                //    for (int i = 0; i < targetsX.Count(); i++)
                                //        plt.PlotScatter(targetsX[i].ToArray(), targetsY[i].ToArray(), Color.Red);
                                //    for (int i = 0; i < lungsX.Count(); i++)
                                //        plt.PlotScatter(lungsX[i].ToArray(), lungsY[i].ToArray(), Color.Blue);
                                //    plt.PlotScatter(new List<double> { isocenter.x }.ToArray(), new List<double> { -isocenter.y }.ToArray(), Color.Green);
                                //    plt.Title("Contours in BEV");
                                //    var outputdir = @"C:\Users\Varian\Desktop\DEBUG\GantryOptimization\";
                                //    var fileName = string.Format("Gantry{0:00.0}_Z{1:0}.png", gantryAngle, z);
                                //    plt.Axis(-400, 400, -400, 400);
                                //    plt.SaveFig(outputdir + fileName);
                                //    plt.Clear();
                                //}

                                foreach (var lungcontour in lungipsicontoursRotatedDiv)
                                    lungAreaInField += StructureHelpers.calculateAreaAboveCoordinateY(lungcontour, lowerYedge);

                                lungVolumeInField += lungAreaInField * ss.Image.ZRes / 1000; // cubic centimeters
                            }
                        }
                    }
                }
                lungVolumesByGantry.Add(new KeyValuePair<double, double>(gantryAngle, lungVolumeInField));
            }
            lungVolumesByGantry = lungVolumesByGantry.OrderByDescending(p => p.Value).ToList();
            optimalGantryAngle = lungVolumesByGantry.Last().Key;

            // MessageBox.Show(string.Format("found optimal gantry angle {0:00.0}", optimalGantryAngle));

            return optimalGantryAngle;
        }

        public static List<Point> ConvertJaggedPointArrayToPointList(Point[][] BEVcontour)
        {
            List<Point> pointList = new List<Point>();
            foreach (var p in BEVcontour)
            {
                foreach (var t in p)
                {
                    pointList.Add(t);
                }
            }
            return pointList;
        }

        public static List<double> returnXorYlistFromListOfPoints(List<Point> points, bool returnX) // if true return x, if not return y
        {
            List<double> tis = new List<double>();
            foreach (var p in points)
                if (returnX) tis.Add(p.X);
                else tis.Add(p.Y);
            return tis;
        }

        public static void plotScatterContour(ScottPlot.Plot plt, List<Point> contour, Color color)
        {
            var xs = returnXorYlistFromListOfPoints(contour, true);
            var ys = returnXorYlistFromListOfPoints(contour, false);
            plt.PlotScatter(xs.ToArray(), ys.ToArray(), color);
        }
        public static void plotScatterContour(ScottPlot.Plot plt, Tuple<Point, Point> tuple, Color color)
        {
            double[] intersectionXs = new double[] { tuple.Item1.X, tuple.Item2.X };
            double[] intersectionYs = new double[] { tuple.Item1.Y, tuple.Item2.Y };
            if (intersectionXs.Length > 1) plt.PlotScatter(intersectionXs, intersectionYs, color);
        }
        public static void plotScatterContourList(ScottPlot.Plot plt, List<List<Point>> contours, Color color)
        {
            foreach (var contour in contours)
                plotScatterContour(plt, contour, color);
        }

        // this find optimal collimator rotation angle for given gantry angle, and recoomends jaw position, which corresponds shiftAmount into lung, specify bank
        public static Tuple<double, double> findBreastOptimalCollAndJawIntoLung(ExternalBeamMachineParameters machinePars, ExternalPlanSetup eps, VVector isocenter, StructureSet ss, Structure targetStructure, Structure lungStructure, double gantryAngle, double startCA, double endCA, double stepSizeCA, string SelectedBreastSide, bool isMedialField)
        {
            var tmp = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, gantryAngle, 0, isocenter);

            Point[][] target = tmp.GetStructureOutlines(targetStructure, true);
            Point[][] lung = tmp.GetStructureOutlines(lungStructure, true);

            var pivotPoint = new Point(0, 0);
            // convert terrible arrays to point list
            var targetPoints = ConvertJaggedPointArrayToPointList(target);
            var lungPoints = ConvertJaggedPointArrayToPointList(lung);


            var targetContourParameters = findContourParameters(targetPoints);
            var lungContourParameters = findContourParameters(lungPoints);

            double optimalCollimatorAngle = double.NaN;

            double lungCutOff = 100;

            List<Point> lungSegment = new List<Point>();
            foreach (var lp1 in lungPoints)
            {
                var lp2 = lungPoints.SkipWhile(s => s != lp1).Skip(1).DefaultIfEmpty(new Point(double.NaN, double.NaN)).FirstOrDefault();
                if (lp1 == lungPoints.Last()) lp2 = lungPoints.First();
                if (lp1.Y < targetContourParameters.Ymax && lp1.Y > targetContourParameters.Ymin && lp2.X - lp1.X >= 0)
                {
                    if (SelectedBreastSide == "Left" && isMedialField)
                        if (lp1.X > lungContourParameters.Xmax - lungCutOff && lp2.X > lungContourParameters.Xmax - lungCutOff)
                            if (lp1.X > lungContourParameters.Center.X && lp2.X > lungContourParameters.Center.X)
                                    lungSegment.Add(lp1);
                    if (SelectedBreastSide == "Left" && !isMedialField)
                        if (lp1.X < lungContourParameters.Xmin + lungCutOff && lp2.X < lungContourParameters.Xmin + lungCutOff)
                            if (lp1.X < lungContourParameters.Center.X && lp2.X < lungContourParameters.Center.X)
                                    lungSegment.Add(lp1);
                    if (SelectedBreastSide == "Right" && isMedialField)
                        if (lp1.X < lungContourParameters.Xmin + lungCutOff && lp2.X < lungContourParameters.Xmin + lungCutOff)
                            if (lp1.X < lungContourParameters.Center.X && lp2.X < lungContourParameters.Center.X)
                                lungSegment.Add(lp1);
                    if (SelectedBreastSide == "Right" && !isMedialField)
                        if (lp1.X > lungContourParameters.Xmax - lungCutOff && lp2.X > lungContourParameters.Xmax - lungCutOff)
                            if (lp1.X > lungContourParameters.Center.X && lp2.X > lungContourParameters.Center.X)
                                lungSegment.Add(lp1);
                }
            }

            //var plt = new ScottPlot.Plot(600, 600);
            //plotScatterContour(plt, targetPoints, Color.Red);
            //plotScatterContour(plt, lungPoints, Color.Green);
            //plotScatterContour(plt, lungSegment, Color.Blue);
            //plt.Title("Contours in BEV");
            ////plt.Axis(-200, 200, -200, 200);
            //var outputdir = @"C:\Users\Varian\Desktop\DEBUG\CollimatorOptimization\";
            //var fileName = string.Format("Gantry{0:00.0}.png", gantryAngle);
            //plt.SaveFig(outputdir + fileName);

            optimalCollimatorAngle = PlanarHelpers.findCollimatorAngle(lungSegment, gantryAngle, SelectedBreastSide, isMedialField);

            //MessageBox.Show(string.Format("found optimal collimator angle {0:00.0}", optimalCollimatorAngle));
            eps.RemoveBeam(tmp);
            if (double.IsNaN(optimalCollimatorAngle))
            {
                MessageBox.Show(string.Format("Error determining optimal collimator angle at the gantry angle = {0:0.0}! Set collimator manually", gantryAngle));
                optimalCollimatorAngle = 0;
            }

            //if (SelectedBreastSide == "Left") optimalCollimatorAngle = 360 - optimalCollimatorAngle;
            optimalCollimatorAngle = 360 - optimalCollimatorAngle;
            if (optimalCollimatorAngle == 360) optimalCollimatorAngle = 0;

            return new Tuple<double, double>(optimalCollimatorAngle, 0);
        }

        public static Beam minimizeLungDoseByRunningDoseCalc(ExternalBeamMachineParameters machinePars, ExternalPlanSetup ps, Structure target, Structure lung, double startGA, double endGA, double stepSizeGA, double startCA, double endCA, double stepSizeCA, VVector isocenter)
        {
            if (ps.Beams.ToList().Count() > 0)
            {
                Console.WriteLine("there is a field before optimizer, remove it please");
                return null;
            }
            double ganAngle = 0;
            double colAngle = 0;
            double minVol = 1000000;
            DoseValue lungDose = new DoseValue(20, DoseValue.DoseUnit.Gy);
            for (double gantryAngle = startGA; gantryAngle < endGA; gantryAngle += stepSizeGA)
            {
                Beam f1 = ps.AddMLCBeam(machinePars, null, fs10x10, (startCA + endCA) / 2, gantryAngle, 0, isocenter);
                f1.FitMLCToStructure(margins5, target, false, jawFit, olmp, clmp);
                ps.CalculateDose();
                double vol = ps.GetVolumeAtDose(lung, lungDose, VolumePresentation.Relative);
                if (vol < minVol)
                {
                    minVol = vol;
                    ganAngle = gantryAngle;
                }
                ps.RemoveBeam(f1);
            }

            for (double collimatorAngle = startCA; collimatorAngle < endCA; collimatorAngle += stepSizeCA)
            {
                Beam f1 = ps.AddMLCBeam(machinePars, null, fs10x10, collimatorAngle, ganAngle, 0, isocenter);
                f1.FitMLCToStructure(margins5, target, false, jawFit, olmp, clmp);
                ps.CalculateDose();
                double vol = ps.GetVolumeAtDose(lung, lungDose, VolumePresentation.Relative);
                if (vol < minVol)
                {
                    minVol = vol;
                    colAngle = collimatorAngle;
                }
                ps.RemoveBeam(f1);
            }

            Beam medialField = ps.AddMLCBeam(machinePars, null, fs10x10, colAngle, ganAngle, 0, isocenter);
            medialField.FitMLCToStructure(margins5, target, false, jawFit, olmp, clmp);
            return medialField;
        }

        public static void openMLCoutOfBody(Beam f1, bool moveOutBankA)
        {
            float maxA = 10000; // x1 - more negative positions means aways from isocenter in direction of bankA
            float maxB = -10000; // x2 - more positive positions means away from isocenter in direction of bankB
            float[,] leafPositions = f1.ControlPoints[0].LeafPositions;
            BeamParameters pars = f1.GetEditableParameters();
            for (int mlcIndex = 0; mlcIndex < 60; mlcIndex++)
            {
                float bankA = leafPositions[0, mlcIndex];
                float bankB = leafPositions[1, mlcIndex];
                if (bankA < maxA)
                {
                    maxA = bankA;
                }

                if (bankB > maxB)
                {
                    maxB = bankB;
                }
            }
            for (int mlcIndex = 0; mlcIndex < 60; mlcIndex++)
            {
                float bankA = leafPositions[0, mlcIndex];
                float bankB = leafPositions[1, mlcIndex];

                if (moveOutBankA)
                {
                    leafPositions[0, mlcIndex] = maxA - 20; // if using breathhold one can use symetric margins and moveout mlc manually
                    leafPositions[0, mlcIndex] = maxA; // free breathing balient is fit with asym margins
                }
                else
                {
                    leafPositions[1, mlcIndex] = maxB + 20;// if using breathhold one can use symetric margins and moveout mlc manually
                    leafPositions[1, mlcIndex] = maxB;

                }
            }
            //        if (moveOutBankA)
            //        {
            //            VRect<double> jaws = new VRect<double>(
            // f1.ControlPoints[0].JawPositions.X1 - 20,
            // f1.ControlPoints[0].JawPositions.Y1,
            // f1.ControlPoints[0].JawPositions.X2,
            // f1.ControlPoints[0].JawPositions.Y2);
            //            pars.SetJawPositions(jaws);
            //        } else
            //        {
            //            VRect<double> jaws = new VRect<double>(
            //f1.ControlPoints[0].JawPositions.X1,
            //f1.ControlPoints[0].JawPositions.Y1,
            //f1.ControlPoints[0].JawPositions.X2 + 20,
            //f1.ControlPoints[0].JawPositions.Y2);
            //            pars.SetJawPositions(jaws);
            //        }
            pars.SetAllLeafPositions(leafPositions);

            try
            {
                f1.ApplyParameters(pars);
            }
            catch (ValidationException es)
            {
                MessageBox.Show(es.ToString(), "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        public static Beam buildOposingToJawPlan(ExternalBeamMachineParameters machinePars, ExternalPlanSetup ps, Structure target, Beam field1, string SelectedBreastSide)
        {
            double x1 = 0;
            double x2 = 0;
            if (SelectedBreastSide == "Left") x1 = Math.Abs(field1.ControlPoints[0].JawPositions.X1);
            if (SelectedBreastSide == "Right") x2 = Math.Abs(field1.ControlPoints[0].JawPositions.X2);

            double alphax1 = (180 / Math.PI) * Math.Asin(x1 / 1000);
            double alphax2 = (180 / Math.PI) * Math.Asin(x2 / 1000);
            double lateralFieldGantryAngle = 0;
            if (SelectedBreastSide == "Left") lateralFieldGantryAngle = field1.ControlPoints[0].GantryAngle - 180 + 2 * alphax1;
            if (SelectedBreastSide == "Right") lateralFieldGantryAngle = field1.ControlPoints[0].GantryAngle + 180 - 2 * alphax2;
            double lateralColimator = 360 - field1.ControlPoints[0].CollimatorAngle;
            if (lateralColimator == 360) lateralColimator = 0;
            Beam beam2 = ps.AddMLCBeam(machinePars, null, fs10x10, lateralColimator, lateralFieldGantryAngle, 0, target.CenterPoint);
            return beam2;
        }

        public static List<Structure> createBreastFifHotSpotContour(StructureSet ss, ExternalPlanSetup plan, double hotspotValue)
        {
            double removeSmallerPartsMM = 2;
            Structure body = StructureHelpers.getStructureFromStructureSet("body", ss, false);

            string hotspotAnteriorID = string.Format("0_hs{0}", hotspotValue.ToString());
            string hotspotNearLungID = string.Format("0_hs{0}nl", hotspotValue.ToString());
            Structure hotspotAnterior = StructureHelpers.getStructureFromStructureSet(hotspotAnteriorID, ss, true);
            Structure hotspotNearLung = StructureHelpers.getStructureFromStructureSet(hotspotNearLungID, ss, true);
            if (hotspotAnterior == null)
            {
                hotspotAnterior = StructureHelpers.createStructureIfNotExisting(hotspotAnteriorID, ss, "DOSE_REGION");
            }

            if (hotspotNearLung == null)
            {
                hotspotNearLung = StructureHelpers.createStructureIfNotExisting(hotspotNearLungID, ss, "DOSE_REGION");
            }

            Structure lungIpsi = StructureHelpers.getStructureFromStructureSet("lung l", ss, false);
            if (lungIpsi == null)
            {
                return null;
            }

            List<Structure> hotspotList = new List<Structure>();

            DoseValue hotspotDoseValue = new DoseValue(hotspotValue, DoseValue.DoseUnit.Percent);
            DoseValue idv5p = new DoseValue(5, DoseValue.DoseUnit.Percent);
            DoseValue idv30p = new DoseValue(30, DoseValue.DoseUnit.Percent);
            Structure s_idv5p = StructureHelpers.getStructureFromStructureSet("0_id5p", ss, true);
            Structure s_idv30p = StructureHelpers.getStructureFromStructureSet("0_id30p", ss, true);
            if (s_idv5p == null)
            {
                s_idv5p = StructureHelpers.createStructureIfNotExisting("0_id5p", ss, "DOSE_REGION");

            }

            if (s_idv30p == null)
            {
                s_idv30p = StructureHelpers.createStructureIfNotExisting("0_id30p", ss, "DOSE_REGION");
            }

            //hotspotAnterior.ConvertDoseLevelToStructure(plan.Dose, hotspotDoseValue);
            //StructureHelpers.removeSmallParts(hotspotAnterior, ss, removeSmallerPartsMM);
            if (s_idv5p.IsEmpty) // create this helper structure only if its emtpy to reduce resources
            {
                s_idv5p.ConvertDoseLevelToStructure(plan.Dose, idv5p);
                s_idv30p.ConvertDoseLevelToStructure(plan.Dose, idv30p);
                StructureHelpers.removeSmallParts(s_idv5p, ss, removeSmallerPartsMM);
                StructureHelpers.removeSmallParts(s_idv30p, ss, removeSmallerPartsMM);
                StructureHelpers.cropAwayFromBody(ss, body, s_idv5p, 10);
                StructureHelpers.cropAwayFromBody(ss, body, s_idv30p, 10);
                s_idv5p.SegmentVolume = s_idv5p.Sub(s_idv30p);
                StructureHelpers.cropAwayFromStructure(ss, lungIpsi, s_idv5p, true, 10, 10, 10, 10, 0, 0);
            }

            Structure dummy2 = StructureHelpers.createStructureIfNotExisting("dummy2", ss, "DOSE_REGION");
            dummy2.ConvertDoseLevelToStructure(plan.Dose, hotspotDoseValue);
            StructureHelpers.removeSmallParts(dummy2, ss, removeSmallerPartsMM);

            Structure id5p5 = StructureHelpers.getStructureFromStructureSet("id5p5", ss, true);
            if (id5p5 == null)
            {
                AxisAlignedMargins tmpmargins = new AxisAlignedMargins(StructureMarginGeometry.Outer, 50, 50, 0, 50, 50, 0);
                id5p5 = StructureHelpers.createStructureIfNotExisting("0_id5p5", ss, "Control");
                SegmentVolume id5p5Segment = s_idv5p.Margin(marginInMM: 0);
                id5p5.SegmentVolume = id5p5Segment;
                id5p5.SegmentVolume = id5p5.AsymmetricMargin(tmpmargins);
            }
            StructureHelpers.seperateHotSpots(dummy2, id5p5, hotspotAnterior, hotspotNearLung, ss);
            if (!hotspotAnterior.IsEmpty) hotspotList.Add(hotspotAnterior);
            if (!hotspotNearLung.IsEmpty) hotspotList.Add(hotspotNearLung);
            ss.RemoveStructure(dummy2);
            return hotspotList;
        }
        public static Point rotate2DvectorAroundPivot(Point t, Point pivot, double angleRad)
        {
            Point newPoint = new Point();
            newPoint.X = Math.Cos(angleRad) * (t.X - pivot.X) - Math.Sin(angleRad) * (t.Y - pivot.Y) + pivot.X;
            newPoint.Y = Math.Sin(angleRad) * (t.X - pivot.X) + Math.Cos(angleRad) * (t.Y - pivot.Y) + pivot.Y;
            return newPoint;
        }

        public static List<Point> rotate2DPointListAroundPivot(List<Point> contour, Point pivot, double angleRad)
        {
            List<Point> tis = new List<Point>();
            foreach (var p in contour)
                tis.Add(rotate2DvectorAroundPivot(p, pivot, angleRad));
            return tis;
        }

        public static void drawMLCshape(float[,] lp, string name)
        {
            List<double> xsbank1 = new List<double>();
            List<double> xsbank2 = new List<double>();
            List<double> ysbank1 = new List<double>();
            List<double> ysbank2 = new List<double>();

            for (int i = 0; i < 60; i++)
            {
                // left edge
                xsbank1.Add(Convert.ToDouble(mlc120IndexMappingX[i].Item1));
                xsbank2.Add(Convert.ToDouble(mlc120IndexMappingX[i].Item1));
                ysbank1.Add(Convert.ToDouble(lp[0, i]));
                ysbank2.Add(Convert.ToDouble(lp[1, i]));
                // right edge
                xsbank1.Add(Convert.ToDouble(mlc120IndexMappingX[i].Item2));
                xsbank2.Add(Convert.ToDouble(mlc120IndexMappingX[i].Item2));
                ysbank1.Add(Convert.ToDouble(lp[0, i]));
                ysbank2.Add(Convert.ToDouble(lp[1, i]));
            }

            //var plt = new ScottPlot.Plot(600, 600);
            //plt.Axis(-200, 200, -200, 200);
            //plt.Title("MLC shape");
            //plt.PlotScatter(xsbank1.ToArray(), ysbank1.ToArray(), Color.Red); // bankA
            //plt.PlotScatter(xsbank2.ToArray(), ysbank2.ToArray(), Color.Blue); // bankB
            //plt.SaveFig(@"C:\Users\Varian\Desktop\DEBUG\FieldBEVs\" + name + ".png");
        }
        public static bool checkIfMLCisOK(float[,] lp)
        {
            float min = 1000;
            float max = -1000;
            for (int i = 0; i < 60; i++)
            {
                // mlc span is 15 cm
                if (lp[0, i] < min) min = lp[0, i];
                if (lp[1, i] > max) max = lp[1, i];
            }
            if (max - min <= 150) return true;
            else return false;
        }

        public static void generateFifForHotSpot(ExternalBeamMachineParameters machinePars, StructureSet ss, ExternalPlanSetup plan, Structure hs, Beam mainField, bool modifyBankA)
        {
            if (plan.IsDoseValid)
            {
                BeamParameters mainFieldPars = mainField.GetEditableParameters();
                VVector someverctor = new VVector();
                Beam FiF = plan.AddMLCBeam(machinePars, null, fs10x10, 0, 0, 0, someverctor);
                FiF.ApplyParameters(mainFieldPars);
                FiF.FitMLCToStructure(margins0, hs, false, jawFit, olmp, clmp);
                float[,] lpFiF = FiF.ControlPoints[0].LeafPositions;
                float[,] lpMF = mainField.ControlPoints[0].LeafPositions;
                double FiFY1 = FiF.ControlPoints[0].JawPositions.Y1;
                double FiFY2 = FiF.ControlPoints[0].JawPositions.Y2;
                for (int mlcIndex = 0; mlcIndex < 60; mlcIndex++)
                {
                    float bankAFiF = lpFiF[0, mlcIndex];
                    float bankBFiF = lpFiF[1, mlcIndex];
                    double leftEdge = double.NaN;
                    double rightEdge = double.NaN;
                    if (mlcIndex < 10)
                    {
                        leftEdge = -200 + (mlcIndex) * 10;
                        rightEdge = -200 + (mlcIndex + 1) * 10;
                    }
                    else if (mlcIndex < 50)
                    {
                        leftEdge = -100 + (mlcIndex - 10) * 5;
                        rightEdge = -100 + (mlcIndex - 10 + 1) * 5;
                    }
                    else
                    {
                        leftEdge = -400 + (mlcIndex) * 10;
                        rightEdge = -400 + (mlcIndex + 1) * 10;
                    }
                    if (leftEdge == double.NaN || rightEdge == double.NaN)
                    {
                        MessageBox.Show("did not find leaf edge, something is wrong, exiting", "FiF field y jaws", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    bool isInField = false;
                    if (leftEdge >= FiFY1 && rightEdge <= FiFY2) isInField = true;

                    if (isInField)
                    {
                        if (modifyBankA)
                        {
                            if (bankBFiF < lpMF[1, mlcIndex] - 2)
                                lpMF[0, mlcIndex] = bankBFiF;
                            else
                                lpMF[0, mlcIndex] = lpMF[1, mlcIndex] - 2;
                        }
                        else
                        {
                            if (bankAFiF > lpMF[0, mlcIndex] + 2)
                                lpMF[1, mlcIndex] = bankAFiF;
                            else
                                lpMF[1, mlcIndex] = lpMF[0, mlcIndex] + 2;
                        }
                    }
                }
                if (!checkIfMLCisOK(lpMF)) MessageBox.Show("MLC leaf span is bigger than 15cm for " + hs.Id);

                drawMLCshape(lpMF, hs.Id);
                BeamParameters FiFPars = mainField.GetEditableParameters();
                FiFPars.WeightFactor = 0;
                FiFPars.SetAllLeafPositions(lpMF);

                //int pointCount = 50;
                //double[] dataXs = ScottPlot.DataGen.Consecutive(pointCount);
                //double[] dataSin = ScottPlot.DataGen.Sin(pointCount);
                //double[] dataCos = ScottPlot.DataGen.Cos(pointCount);
                //plt.PlotScatter(dataXs, dataSin);
                //plt.PlotScatter(dataXs, dataCos);
                //plt.AxisAuto(0, .5); // no horizontal padding, 50% vertical padding

                FiF.ApplyParameters(FiFPars);
                FiF.Id = createFifName(plan, mainField.Id);
            }
        }
    }
}