using GalaSoft.MvvmLight.Messaging;
using SimpleGui.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Markup;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SimpleGui.AutoPlans
{
    public class EsophagusScript
    {
        //private Patient p;
        private List<Structure> PTVs = new List<Structure>();
        private List<Structure> PTVse = new List<Structure>();
        private List<Structure> PTVinters = new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure Body;
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure lungL;
        private Structure heart;
        private Structure lungR;
        private Structure spinalCord;
        private Structure spinalCordPRV;
        private Structure Liver;
        private Structure KidneyL;
        private Structure KidneyR;
        private Structure Bowel;
        private Structure heartMinusPTV;
        private Structure lungsMinusPTV;
        private Structure LiverMinusPTV;
        //private Structure KidneyLMinusPTV;
        //private Structure KidneyRMinusPTV;
        private Structure BowelMinusPTV;
        //private Structure lungRMinusPTV;
        private OptimizationOptionsVMAT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));

        public void runEsophagusScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin,
            string heartId, string lungLId, string lungRId, string SpinalCordid, string LiverId, string kidneyLid, string kidneyRid, string BowelId)
        {
            if (Check(machinePars)) return;
            Messenger.Default.Send("Started Running Script");
            p = pat;
            eps = eps1;
            ss = ss1;
            presc = prescriptions;
            NOF = nof;
            mlcId = MlcId;
            if (presc.Count == 0)
            {
                MessageBox.Show("Please add target");
                return;
            }

            bool doCrop = true;
            if (double.IsNaN(CropFromBody)) doCrop = false;

            pat.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);
            Body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            if(doCrop) BodyShrinked.SegmentVolume = Body.Margin(-CropFromBody);

            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            #region Prepare general Structures
            Messenger.Default.Send("Creating Optimization Structures");
            ptvEval = StructureHelpers.createStructureIfNotExisting("0_ptvEval", ss, "PTV");
            ptvEval3mm = StructureHelpers.createStructureIfNotExisting("0_ptvEval3mm", ss, "CONTROL");
            // segment helper structures
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            // make all PTVs add to ptvEval
            foreach (var p in PTVs) if (ptvEval.IsEmpty) ptvEval.SegmentVolume = p.Margin(0);
                else ptvEval.SegmentVolume = ptvEval.Or(p);

            if (doCrop)
                ptvEval.SegmentVolume = ptvEval.And(BodyShrinked);

            PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, doCrop);

            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, BodyShrinked, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(Body);
            // ======================================================
            // plan specific structures and their cropped versions
            lungL = StructureHelpers.getStructureFromStructureSet(lungLId, ss, true);
            heart = StructureHelpers.getStructureFromStructureSet(heartId, ss, true);
            lungR = StructureHelpers.getStructureFromStructureSet(lungRId, ss, true);
            Liver = StructureHelpers.getStructureFromStructureSet(LiverId, ss, true);
            KidneyL = StructureHelpers.getStructureFromStructureSet(kidneyLid, ss, true);
            KidneyR = StructureHelpers.getStructureFromStructureSet(kidneyRid, ss, true);
            Bowel = StructureHelpers.getStructureFromStructureSet(BowelId, ss, true);
            spinalCord = StructureHelpers.getStructureFromStructureSet(SpinalCordid, ss, true);
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(lungL);
            listOfOars.Add(lungR);
            listOfOars.Add(heart);
            listOfOars.Add(spinalCord);
            if (Liver != null) listOfOars.Add(Liver);
            if (KidneyL != null) listOfOars.Add(KidneyL);
            if (KidneyR != null) listOfOars.Add(KidneyR);

            Structure KidneyLMinusPTV = StructureHelpers.createStructureIfNotExisting("0_kidneyL-PTV", ss, "ORGAN");
            Structure KidneyRMinusPTV = StructureHelpers.createStructureIfNotExisting("0_kidneyR-PTV", ss, "ORGAN");
            if (KidneyL != null) KidneyLMinusPTV.SegmentVolume = KidneyL.Sub(ptvEval);
            if (KidneyR != null) KidneyRMinusPTV.SegmentVolume = KidneyL.Sub(ptvEval);

            if (Bowel != null) listOfOars.Add(Bowel);
            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            // create helper structures
            Structure heartCropped = StructureHelpers.createStructureIfNotExisting("0_heartCr", ss, "ORGAN");
            Structure LiverCropped = StructureHelpers.createStructureIfNotExisting("0_liverCr", ss, "ORGAN");
            Structure lungs = StructureHelpers.createStructureIfNotExisting("0_lungs", ss, "ORGAN");
            StructureHelpers.CopyStructureInBounds(heartCropped, heart, ss.Image, (ptvEval.MeshGeometry.Bounds.Z - 10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ + 10));
            StructureHelpers.CopyStructureInBounds(LiverCropped, Liver, ss.Image, (ptvEval.MeshGeometry.Bounds.Z - 10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ + 10));
            LiverMinusPTV = StructureHelpers.createStructureIfNotExisting("0_liver-PTV", ss, "ORGAN");
            spinalCordPRV = StructureHelpers.createStructureIfNotExisting("0_SpinalPrv", ss, "ORGAN");

            spinalCordPRV.SegmentVolume = spinalCord.Margin(3);
            spinalCordPRV.SegmentVolume = spinalCordPRV.Sub(spinalCord);
            heartMinusPTV = StructureHelpers.createStructureIfNotExisting("0_heart-ptv", ss, "ORGAN");
            if (heart != null) heartMinusPTV.SegmentVolume = heartCropped.Sub(ptvEval);
            lungs.SegmentVolume = lungL.Margin(0);
            lungs.SegmentVolume = lungs.Or(lungR);
            lungsMinusPTV = StructureHelpers.createStructureIfNotExisting("0_lungs-ptv", ss, "ORGAN");
            lungsMinusPTV.SegmentVolume = lungs.Sub(ptvEval);
            if (Liver != null) LiverMinusPTV.SegmentVolume = Liver.Sub(ptvEval);
            //lungRMinusPTV.SegmentVolume = lungR.Sub(ptvEval3mm);
            // create rings
            Rings = StructureHelpers.CreateRings(PTVse, ss, Body, ptvEval3mm, 30);

            //Messenger.Default.Send<NotificationMessage<string>>(new NotificationMessage<string>("Generic Value", "notification message"));
            Messenger.Default.Send<string>("Start");

            #endregion

            // add new plan
            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);


            #region field placement
            // get isocenter so that X and Y is in center of whole PTV while z is 10 cm in CaurodCranial from the bottom bound of ptv high, this way cbct doesn't need shifting
            double isoZ = 0;
            var selectedPTViso = PTVs.Find(x => x.Id.Equals(selectedTargetForIso));
            isocenterOffsetZ = isocenterOffsetZ * 10;// convert it to mm
            if (selectedOffsetOrigin == "Cranial Bound") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + selectedPTViso.MeshGeometry.Bounds.SizeZ + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Center") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + selectedPTViso.MeshGeometry.Bounds.SizeZ / 2 + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Caudal Bound") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Overall PTV Center") isoZ = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ / 2 + isocenterOffsetZ;
            VVector iso = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                isoZ);

            // define beamgeometry and fit the jaws
            double startAngle = 181;
            double stopAngle = 179;
            // assuming head first posterior position
            if (numOfArcs == 2 || numOfArcs == 3)
            {
                var arc1 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), 360 - collimatorAngle, 181, 179, GantryDirection.Clockwise, 0, iso);
                var arc2 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), collimatorAngle, 179, 181, GantryDirection.CounterClockwise, 0, iso);
                arc1.Id = "CW";
                arc2.Id = "CCW";
                BeamHelpers.fitArcJawsToTarget(arc1, ss, ptvEval, startAngle, stopAngle, 5, 5);
                BeamHelpers.fitArcJawsToTarget(arc2, ss, ptvEval, startAngle, stopAngle, 5, 5);
                if (numOfArcs == 3)
                {
                    var arc3 = eps.AddArcBeam(machinePars, new VRect<double>(-75, -200, 75, 200), 0, 179, 181, GantryDirection.CounterClockwise, 0, iso);
                    BeamHelpers.fitArcJawsToTarget(arc3, ss, ptvEval, startAngle, stopAngle, 5, 5);
                    BeamHelpers.SetXJawsToCenter(arc3);
                    arc3.Id = "CCW1";
                }
            }
            else if (numOfArcs == 1)
            {
                var arc1 = eps.AddArcBeam(machinePars, new VRect<double>(-75, -200, 75, 200), collimatorAngle, 181, 179, GantryDirection.Clockwise, 0, iso);
                arc1.Id = "CW";
                BeamHelpers.SetXJawsToCenter(arc1);
            }
            #endregion

            #region Prepare Optimization Stuff
            // start optimization
            eps.SetCalculationModel(CalculationType.PhotonVMATOptimization, OptimizationAlgorithmModel);
            //eps.SetCalculationOption(DoseCalculationAlgo, "", "");
            var optSetup = eps.OptimizationSetup;
            optSetup.AddAutomaticNormalTissueObjective(40);
            if (JawTrackingOn) optSetup.UseJawTracking = true;

            // check for emtpy structures again
            listOfOars.Add(LiverCropped);
            listOfOars.Add(LiverMinusPTV);
            listOfOars.Add(heartCropped);
            listOfOars.Add(heartMinusPTV);
            listOfOars.Add(spinalCordPRV);

            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);

            /*
             Lung V40Gy<10%
             Lung V30Gy<15%
             Lung V20Gy<20%
             Lung V10Gy<40%
             Lung V05Gy<50%
             Lung Mean < 20Gy
            Cord Max<45Gy
            Left/Right Kidney (evaluate seperate) V18Gy<1/3
            Lef/Right Kiddney mean <18Gy
            Liver V20Gy <30%
            Liver V30Gy <20%
            Liver mean < 25 Gy
            Bowel Max < max ptv
            Bowel D05<45Gy
            Stomach Mean < 30 Gy
            Stomach max < 54
            Heart V30Gt < 30%
            Heart Mean < 30Gy
             */

            double maxPrescribed = NOF * presc[0].Value;
            double maxScale = 66D;

            //lungs
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 39.5D / maxScale, 010, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 29.5D / maxScale, 015, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 19.5D / maxScale, 020, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 9.5D / maxScale, 040, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 4.7D / maxScale, 050, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungs, maxPrescribed * 39.5D / maxScale, 010, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungs, maxPrescribed * 29.5D / maxScale, 015, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungs, maxPrescribed * 19.5D / maxScale, 020, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungs, maxPrescribed * 9.5D / maxScale, 040, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungs, maxPrescribed * 4.7D / maxScale, 050, 0);
            // cord
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCord, maxPrescribed * 40D / maxScale, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCordPRV, maxPrescribed * 45D / maxScale, 000, 70);
            // heart
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, heartCropped, maxPrescribed * 30D / maxScale, 030, 70);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, heartMinusPTV, maxPrescribed * 30D / maxScale, 20);
            // kidneys
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, KidneyLMinusPTV, maxPrescribed * 18D / maxScale, 033, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, KidneyRMinusPTV, maxPrescribed * 18D / maxScale, 033, 70);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, KidneyLMinusPTV, maxPrescribed * 18D / maxScale, 20);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, KidneyRMinusPTV, maxPrescribed * 18D / maxScale, 20);
            // liver
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LiverCropped, maxPrescribed * 1.02, 000, 70);
            //BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LiverCropped, maxPrescribed * 20D/maxScale, 000, 70);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, LiverMinusPTV, maxPrescribed * 30D / maxScale, 20);
            // Bowel
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BowelMinusPTV, maxPrescribed * 0.99D, 0, 70);

            //implement lungR too
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            ss.RemoveStructure(BodyShrinked);
            ss.RemoveStructure(ptvEval3mm);

            #endregion
            Messenger.Default.Send("Plan Prepared");
            MessageBox.Show("All done");
        }

        private bool Check(ExternalBeamMachineParameters machinePars)
        {
            if (machinePars == null)
            {
                MessageBox.Show("Forgot to set machine parameters");
                return true;
            }
            return false;
        }

        public void runOptimization()
        {
            optimizationOptions = new OptimizationOptionsVMAT(OptimizationOption.RestartOptimization, mlcId);
            var res = eps.OptimizeVMAT(optimizationOptions);
            if (!res.Success)
                Messenger.Default.Send("Optimization Failed");
            else
            {
                Messenger.Default.Send("Optimization Complete");
                eps.CalculateDose();
                Messenger.Default.Send("Dose Calculation Complete");
            }
        }
    }
}