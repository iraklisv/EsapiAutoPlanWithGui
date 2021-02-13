
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using SimpleGui.Helpers;
using System.Linq;
using System.Windows;

namespace SimpleGui.AutoPlans
{
    public class WholeBrainPalliativeScript
    {
        private Structure LensL;
        private Structure LensR;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        public void runWholeBrainPalliative(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
     ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
     int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle,
     string LensLId, string LensRId)
        {
            //if (Check(machinePars)) return;
            p = pat;
            eps = eps1;
            ss = ss1;
            presc = prescriptions;
            NOF = nof;
            mlcId = MlcId;

            if (!machinePars.TechniqueId.Contains("STATIC")) {
                MessageBox.Show("currently supporting only STATIC");
                return;
            }

            if (presc.Count == 0)
            {
                MessageBox.Show("Please add target");
                return;
            }
            pat.BeginModifications();

            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction
            //PTVe = StructureHelpers.createStructureIfNotExisting("0_ptve", ss, "PTV");

            Structure brain = StructureHelpers.getStructureFromStructureSet(presc.FirstOrDefault().Key, ss, true);
            LensR = StructureHelpers.getStructureFromStructureSet(LensRId, ss, true);
            LensL = StructureHelpers.getStructureFromStructureSet(LensLId, ss, true);

            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);

            if (LensL == null || LensR == null) return;
            double isoX = (LensL.CenterPoint.x + LensR.CenterPoint.x) / 2;
            double isoY = (LensL.CenterPoint.y + LensR.CenterPoint.y) / 2;
            double isoZ = (LensL.CenterPoint.z + LensR.CenterPoint.z) / 2;

            var isocenter = brain.CenterPoint;
            isocenter.x = isoX;
            isocenter.y = isoY + 10; // shift isocenter closer to brains by 1cm
            isocenter.z = isoZ;
            //// addmlcbeam (mahcinePars,leaf positions float[,], jaw position, double collimator angle, gantry angle, patient support angle, isocenter)
            Beam g90 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-10, -10, 10, 10), collimatorAngle, 90, 0, isocenter);
            Beam g270 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-10, -10, 10, 10), 360- collimatorAngle, 270, 0, isocenter);
            bool useAsymmetricXJaw = true, useAsymmetricYJaws = true, optimizeCollimatorRotation = false;
            g90.FitCollimatorToStructure(new FitToStructureMargins(20), brain, useAsymmetricXJaw, useAsymmetricYJaws, optimizeCollimatorRotation);
            g270.FitCollimatorToStructure(new FitToStructureMargins(20), brain, useAsymmetricXJaw, useAsymmetricYJaws, optimizeCollimatorRotation);

            FitToStructureMargins margins = new FitToStructureMargins(10);
            JawFitting jawFit = JawFitting.FitToRecommended;
            OpenLeavesMeetingPoint olmp = OpenLeavesMeetingPoint.OpenLeavesMeetingPoint_Outside;
            ClosedLeavesMeetingPoint clmp = ClosedLeavesMeetingPoint.ClosedLeavesMeetingPoint_Center;

            g90.FitMLCToStructure(margins, brain, optimizeCollimatorRotation, jawFit, olmp, clmp);
            g270.FitMLCToStructure(margins, brain, optimizeCollimatorRotation, jawFit, olmp, clmp);

            g90.Id = string.Format("g{0}c{1}",
                        g90.GantryAngleToUser(g90.ControlPoints[0].GantryAngle),
                        g90.CollimatorAngleToUser(g90.ControlPoints[0].CollimatorAngle)
                        );
            g270.Id = string.Format("g{0}c{1}",
                        g270.GantryAngleToUser(g270.ControlPoints[0].GantryAngle),
                        g270.CollimatorAngleToUser(g270.ControlPoints[0].CollimatorAngle)
                        );
            g90.CreateOrReplaceDRR(BeamHelpers.boneDrrPars);
            g270.CreateOrReplaceDRR(BeamHelpers.boneDrrPars);

            //plan.CalculateDose();

            //if (plan.IsDoseValid)
            //{
            //    double maxDose = plan.Dose.DoseMax3D.Dose;
            //    double maxShouldBe = 108;
            //    myMessageBoxWrapper.Show(string.Format("maximum dose without normalization is {0}%", maxDose.ToString("0.0")), "info", MessageBoxButton.OK, MessageBoxImage.Information);
            //    plan.PlanNormalizationValue = maxDose / maxShouldBe * 100;

            //    myMessageBoxWrapper.Show(string.Format("to make max 3D dose 108, plan normalization was set = {0:0.0}", plan.PlanNormalizationMethod), "info", MessageBoxButton.OK, MessageBoxImage.Information);
            //    DVHData LensLdvhData = plan.GetDVHCumulativeData(LensL, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
            //    DVHData LensRdvhData = plan.GetDVHCumulativeData(LensR, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
            //    DVHData braindvhData = plan.GetDVHCumulativeData(brain, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1);
            //    double LensLmaxDose = LensLdvhData.MaxDose.Dose;
            //    double LensRmaxDose = LensRdvhData.MaxDose.Dose;
            //    DoseValue d03cc = plan.GetDoseAtVolume(brain, 0.03, VolumePresentation.AbsoluteCm3, DoseValuePresentation.Absolute);
            //    DoseValue coverage = plan.GetDoseAtVolume(brain, 98, VolumePresentation.Relative, DoseValuePresentation.Absolute);
            //    myMessageBoxWrapper.Show(string.Format(
            //        "len L max dose is {0} Gy\n" +
            //        "len R max dose is {1} Gy\n" +
            //        "target coverage (98%) is {2} Gy\n" +
            //        "target max (3cc) is {3} Gy",
            //        LensLmaxDose.ToString("0.0"), LensRmaxDose.ToString("0.0"), coverage.Dose.ToString("0.0"), d03cc.Dose.ToString("0.0")), "info", MessageBoxButton.OK, MessageBoxImage.Information);

            //}
            //myMessageBoxWrapper.Show("end of autoplan script, please press SAVE button to save the new plan and open it in Aria", "info", MessageBoxButton.OK, MessageBoxImage.Information);

            MessageBox.Show("All done, close script");
        }
    }
}
