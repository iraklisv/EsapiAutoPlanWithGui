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
    public class RectumScript
    {
        //private Patient p;
        private List<Structure> PTVs=new List<Structure>();
        private List<Structure> PTVse= new List<Structure>();
        private List<Structure> PTVinters= new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure Bladder;
        private Structure BowelBag;
        private Structure FemorL;
        private Structure FemorR;
        private Structure BladderMinusPTV;
        private Structure BowelMinusPTV;
        private OptimizationOptionsVMAT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));

        public void runRectumScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin,
            string bladderId, string bowelId, string femorLid, string femorRid)
        {
            if (Check(machinePars)) return;
            Messenger.Default.Send("Started Running Script");
            p = pat;
            eps = eps1;
            ss = ss1;
            presc = prescriptions;
            NOF = nof;
            mlcId = MlcId;

            if (presc.Count==0)
            {
                MessageBox.Show("Please add target");
                return;
            }

            bool doCrop = true;
            //if (double.IsNaN(CropFromBody)) doCrop = false;

            pat.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);

            var Body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            BodyShrinked.SegmentVolume = Body.Margin(-CropFromBody);
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
            ptvEval.SegmentVolume = ptvEval.And(BodyShrinked);

            if (!double.IsNaN(CropFromBody))
                PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, false);
            else
                PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, true);
            
            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, BodyShrinked, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(Body);

            // ======================================================
            // plan specific structures and their cropped versions
            Bladder = StructureHelpers.getStructureFromStructureSet(bladderId, ss, true);
            BowelBag = StructureHelpers.getStructureFromStructureSet(bowelId, ss, true);
            FemorL = StructureHelpers.getStructureFromStructureSet(femorLid, ss, true);
            FemorR = StructureHelpers.getStructureFromStructureSet(femorRid, ss, true);
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(FemorL);
            listOfOars.Add(FemorR);
            listOfOars.Add(Bladder);
            listOfOars.Add(BowelBag);
            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            // create helper structures
            BladderMinusPTV = StructureHelpers.createStructureIfNotExisting("0_bld-ptv", ss, "ORGAN");
            BowelMinusPTV = StructureHelpers.createStructureIfNotExisting("0_bb-ptv", ss, "ORGAN");
            BladderMinusPTV.SegmentVolume = Bladder.Sub(ptvEval3mm);
            BowelMinusPTV.SegmentVolume = BowelBag.Sub(ptvEval3mm);
            // create rings
            Rings = StructureHelpers.CreateRings(PTVse, ss, Body, ptvEval3mm,30);

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
            if (selectedOffsetOrigin == "Selected PTV Cranial Bound") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + selectedPTViso.MeshGeometry.Bounds.SizeZ + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Selected PTV Center") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + selectedPTViso.MeshGeometry.Bounds.SizeZ / 2 + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Selected PTV Caudal Bound") isoZ = selectedPTViso.MeshGeometry.Bounds.Z + isocenterOffsetZ;
            if (selectedOffsetOrigin == "Overall PTV Center") isoZ = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ/2+isocenterOffsetZ;
            VVector iso = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                isoZ);

            // define beamgeometry and fit the jaws
            double startAngle = 181;
            double stopAngle = 179;
            // assuming head first posterior position
            if (numOfArcs == 2||numOfArcs==3) {
                var arc1 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), 360 - collimatorAngle, 181, 179, GantryDirection.Clockwise, 0, iso);
                var arc2 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), collimatorAngle, 179, 181, GantryDirection.CounterClockwise, 0, iso);
                arc1.Id = "CW";
                arc2.Id = "CCW";
                BeamHelpers.fitArcJawsToTarget(arc1, ss, ptvEval, startAngle, stopAngle, 5, 5);
                BeamHelpers.fitArcJawsToTarget(arc2, ss, ptvEval, startAngle, stopAngle, 5, 5);
                if (numOfArcs == 3)
                {
                    var arc3 = eps.AddArcBeam(machinePars, new VRect<double>(-75,-200,75,200), 0, 179, 181, GantryDirection.CounterClockwise, 0, iso);
                    BeamHelpers.fitArcJawsToTarget(arc3, ss, ptvEval, startAngle, stopAngle, 5, 5);
                    BeamHelpers.SetXJawsToCenter(arc3);
                    arc3.Id = "CCW1";
                }
            } else if (numOfArcs == 1)
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
            if (JawTrackingOn) optSetup.UseJawTracking=true;

            // check for emtpy structures again
            listOfOars.Add(BladderMinusPTV);
            listOfOars.Add(BowelMinusPTV);

            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);

            double maxPrescribed = NOF * presc[0].Value;

            // femors
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorL, maxPrescribed * (35D / 54D), 005, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorR, maxPrescribed * (35D / 54D), 005, 70);
            // bladder
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (45D / 54D), 035, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BladderMinusPTV, maxPrescribed * (40D / 54D), 030, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BladderMinusPTV, maxPrescribed * 0.97D, 000, 70);
            // bowel
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BowelMinusPTV, maxPrescribed * 0.95, 000, 70);
            //implement bowel too

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