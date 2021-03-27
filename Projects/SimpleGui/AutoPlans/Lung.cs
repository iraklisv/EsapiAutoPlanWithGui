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
    public class LungScript
    {
        //private Patient p;
        private List<Structure> PTVs = new List<Structure>();
        private List<Structure> PTVse = new List<Structure>();
        private List<Structure> PTVinters = new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure lungL;
        private Structure heart;
        private Structure lungR;
        private Structure spinalCord;
        private Structure spinalCordPRV;
        private Structure esophagus;
        private Structure heartMinusPTV;
        private Structure lungsMinusPTV;
        private Structure esophagusMinusPTV;
        //private Structure lungRMinusPTV;
        private OptimizationOptionsVMAT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));

        public void runLungScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin,
            double IsocenterX, double IsocenterY, double IsocenterZ,
            string heartId, string lungLId, string lungRId, string SpinalCordid, string Esophagusid)
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

            var Body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            if (doCrop) BodyShrinked.SegmentVolume = Body.Margin(-CropFromBody);

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

            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, Body, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(Body);
            // ======================================================
            // plan specific structures and their cropped versions
            lungL = StructureHelpers.getStructureFromStructureSet(lungLId, ss, true);
            heart = StructureHelpers.getStructureFromStructureSet(heartId, ss, true);
            lungR = StructureHelpers.getStructureFromStructureSet(lungRId, ss, true);
            esophagus = StructureHelpers.getStructureFromStructureSet(Esophagusid, ss, true);
            spinalCord = StructureHelpers.getStructureFromStructureSet(SpinalCordid, ss, true);
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(lungL);
            listOfOars.Add(lungR);
            listOfOars.Add(heart);
            listOfOars.Add(esophagus);
            listOfOars.Add(spinalCord);
            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            // create helper structures
            Structure heartCropped = StructureHelpers.createStructureIfNotExisting("0_heartCr", ss, "ORGAN");
            Structure esophagusCropped = StructureHelpers.createStructureIfNotExisting("0_EsophCr", ss, "ORGAN");
            Structure lungs = StructureHelpers.createStructureIfNotExisting("0_lungs", ss, "ORGAN");
            StructureHelpers.CopyStructureInBounds(heartCropped, heart, ss.Image, (ptvEval.MeshGeometry.Bounds.Z - 10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ + 10));
            StructureHelpers.CopyStructureInBounds(esophagusCropped, esophagus, ss.Image, (ptvEval.MeshGeometry.Bounds.Z - 10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ + 10));
            esophagusMinusPTV = StructureHelpers.createStructureIfNotExisting("0_Esoph-PTV", ss, "ORGAN");
            spinalCordPRV = StructureHelpers.createStructureIfNotExisting("0_SpinalPrv", ss, "ORGAN");

            spinalCordPRV.SegmentVolume = spinalCord.Margin(3);
            spinalCordPRV.SegmentVolume = spinalCordPRV.Sub(spinalCord);
            heartMinusPTV = StructureHelpers.createStructureIfNotExisting("0_heart-ptv", ss, "ORGAN");
            lungs.SegmentVolume = lungL.Margin(0);
            lungs.SegmentVolume = lungs.Or(lungR);
            lungsMinusPTV = StructureHelpers.createStructureIfNotExisting("0_lungs-ptv", ss, "ORGAN");
            heartMinusPTV.SegmentVolume = heartCropped.Sub(ptvEval);
            lungsMinusPTV.SegmentVolume = lungs.Sub(ptvEval);
            esophagusMinusPTV.SegmentVolume = esophagus.Sub(ptvEval);
            //lungRMinusPTV.SegmentVolume = lungR.Sub(ptvEval3mm);

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
            if (!double.IsNaN(IsocenterX)) iso.x = IsocenterX;
            if (!double.IsNaN(IsocenterY)) iso.y = IsocenterY;
            if (!double.IsNaN(IsocenterZ)) iso.z = IsocenterZ;

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
            listOfOars.Add(esophagusCropped);
            listOfOars.Add(esophagusMinusPTV);
            listOfOars.Add(heartCropped);
            listOfOars.Add(heartMinusPTV);
            listOfOars.Add(spinalCordPRV);

            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);

            // considering a 66 Gy to the target, max scenario, following closes on London Cancer guidline for lung treatment.
            /*The spinal cord, both lungs, heart and oesophagus should be outlined.
            No more than 10 cm oesophagus should be included in PTV.
            The spinal cord position must be identified throughout the PTV.
            Maximum radiation dose to 10 cm spinal cord should not exceed 44
            Gy in 2 Gy per fraction or 36Gy in 2.75 Gy fraction size.
            Every effort should be made to exclude normal lung tissue. Less
            than 35 % of ‘normal’ lung(i.e.whole lung excluding Gross Tumour
             Volume) should receive a radiation dose of ≥ 20 Gy i.e.V20 < 35 %
             but < 30 % preferable.
            The heart can receive the total dose(TD) to < 30 % of its volume.
            For > 50 % of cardiac volume, dose < 50 % of TD is recommended.*/

            double maxPrescribed = NOF * presc[0].Value;

            // spinal cord
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCord, maxPrescribed * (40D / 66D), 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCordPRV, maxPrescribed * (45D / 66D), 0, 70);
            // esophagus
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, esophagusCropped, maxPrescribed * 1.02, 0, 70);
            // lungs
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lungsMinusPTV, maxPrescribed * 19.5D / 66D, 30, 70);
            // heart
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, heartCropped, maxPrescribed * 33D / 66D, 30, 0);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, heartMinusPTV, maxPrescribed * (30D / 66D), 20);
            // esophagus
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, esophagusMinusPTV, maxPrescribed * (30D / 66D), 20);

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