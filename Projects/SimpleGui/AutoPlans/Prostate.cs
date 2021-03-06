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
    public class ProstateScript
    {
        //private Patient p;
        private List<Structure> PTVs=new List<Structure>();
        private List<Structure> PTVse= new List<Structure>();
        private List<Structure> PTVinters= new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure Bladder;
        private Structure Rectum;
        private Structure BowelBag;
        private Structure FemorL;
        private Structure FemorR;
        private Structure RectumMinusPTV;
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

        public void runProstateScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, bool PostOpFlag, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin, 
            string rectumId, string bladderId, string bowelId, string femorLid, string femorRid)
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

            // create transition regions
            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, Body, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(Body);

            // ======================================================
            // plan specific structures and their cropped versions
            Bladder = StructureHelpers.getStructureFromStructureSet(bladderId, ss, true);
            Rectum = StructureHelpers.getStructureFromStructureSet(rectumId, ss, true);
            BowelBag = StructureHelpers.getStructureFromStructureSet(bowelId, ss, true);
            FemorL = StructureHelpers.getStructureFromStructureSet(femorLid, ss, true);
            FemorR = StructureHelpers.getStructureFromStructureSet(femorRid, ss, true);
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(FemorL);
            listOfOars.Add(FemorR);
            listOfOars.Add(Rectum);
            listOfOars.Add(Bladder);
            listOfOars.Add(BowelBag);
            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return; 
            
            // do the trick with rectum, so that only part near ptv slices are considered
            Structure RectumCropped = StructureHelpers.createStructureIfNotExisting("0_Rectum", ss, "ORGAN");
            StructureHelpers.CopyStructureInBounds(RectumCropped, Rectum, ss.Image, (ptvEval.MeshGeometry.Bounds.Z-10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ+10));
            //RectumCropped.SegmentVolume = Rectum.Margin(0);
            //ptvEval.MeshGeometry.Bounds.Z

            // create helper structures
            RectumMinusPTV = StructureHelpers.createStructureIfNotExisting("0_rct-ptv", ss, "ORGAN");
            BladderMinusPTV = StructureHelpers.createStructureIfNotExisting("0_bld-ptv", ss, "ORGAN");
            BowelMinusPTV = StructureHelpers.createStructureIfNotExisting("0_bb-ptv", ss, "ORGAN");
            RectumMinusPTV.SegmentVolume = RectumCropped.Sub(ptvEval3mm);
            BladderMinusPTV.SegmentVolume = Bladder.Sub(ptvEval3mm);
            BowelMinusPTV.SegmentVolume = BowelBag.Sub(ptvEval3mm);

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
            if (selectedOffsetOrigin == "Overall PTV Center") isoZ = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ / 2 + isocenterOffsetZ;
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
            listOfOars.Add(RectumCropped);
            listOfOars.Add(RectumMinusPTV);
            listOfOars.Add(BladderMinusPTV);
            listOfOars.Add(BowelMinusPTV);

            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);

            double maxPrescribed = NOF * presc.FirstOrDefault().Value;
            double maxddx = presc.FirstOrDefault().Value;

            //========================== QUANTEC ========================
            //Rectum V50<50 %        < 10 % Grade 3 + toxicity
            //Rectum V60<35 %        < 10 % Grade 3 + toxicity
            //Rectum V65<25 %        < 10 % Grade 3 + toxicity
            //Rectum V70<20 %        < 10 % Grade 3 + toxicity
            //Rectum V75<15 %        < 10 % Grade 3 + toxicity
            //Bladder(prostate cancer)    V65 < 50 % Grade 3 + toxicity
            //Bladder(prostate cancer)   V70 < 35 % Grade 3 + toxicity
            //Bladder(prostate cancer)   V75 < 25 % Grade 3 + toxicity
            //Bladder(prostate cancer)   V80 < 15 % Grade 3 + toxicity
            //============================RTOG=============================
            //Critical Structure  Dose / fx Volume Dose    Max Dose    Protocol Treated organ
            // Bladder 1.8 Gy  60 % 50 Gy       0621    Prostate
            // Bladder 1.8 Gy  55 % 50 Gy PMID 18947938   RTOG Prostate Group Consensus 2009
            // Bladder 1.8 Gy  50 % 65 Gy       0415    Prostate
            // Bladder 1.8 Gy  40 % 66.6 Gy     0621    Prostate
            // Bladder 1.8 Gy  35 % 70 Gy       0415    Prostate
            // Bladder 1.8 Gy  30 % 70 Gy PMID 18947938   RTOG Prostate Group Consensus 2009
            // Bladder 1.8 Gy  25 % 75 Gy       0415    Prostate
            // Bladder 1.8 Gy  15 % 80 Gy       0415    Prostate
            // Femoral Head	1.8 Gy	5%	50 Gy	 	PMID 18947938	RTOG Prostate Group Consensus 2009
            // Penile Bulb 1.8 Gy Mean    51 Gy       0415    Prostate
            //Rectum  1.8 Gy  50 % 50 Gy       0621, PMID 18947938 Prostate
            //Rectum  1.8 Gy  50 % 60 Gy       0415    Prostate
            //Rectum  1.8 Gy  35 % 65 Gy       0415    Prostate
            //Rectum  1.8 Gy  25 % 66.6 Gy     0621    Prostate
            //Rectum  1.8 Gy  25 % 70 Gy       0415    Prostate
            //Rectum  1.8 Gy  15 % 75 Gy       0415    Prostate
            //Rectum  1.8 Gy  20 % 70 Gy PMID 18947938   RTOG Prostate Group Consensus 2009
            //Small Bowel	1.8 Gy	 	 	52 Gy	PMID 18947938	RTOG Prostate Group Consensus 2009
            // Bladder*1.8 Gy  70 % 40 Gy       0534    Postop prostate
            // Bladder*1.8 Gy  50 % 65 Gy       0534    Postop prostate
            // Femoral Head*1.8 Gy	10%	50 Gy	 	0534	Postop prostate
            //Rectum * 1.8 Gy  55 % 40 Gy       0534    Postop prostate
            //Rectum * 1.8 Gy  35 % 65 Gy       0534    Postop prostate
            /* ============== NCBI POSTOP ======================
            https://www.ncbi.nlm.nih.gov/pmc/articles/PMC5905127/
            Table 1
            Organs at risk optimization goals of auto-planning
            Organ	Dose/volume parametersa	Priority
            Bladder	        V40 < 40%	Medium              <===== not useful due post op bladders are hard to constraint anyway
                            V65 < 20%	Medium              <===== not useful due post op bladders are hard to constraint anyway
            Bladder-PTVb	V40 < 40%	High
                            V65 < 20%	High
            Rectumc	        V40 < 40%	Medium
                            V65 < 20%	Medium
            Rectum-PTVb	    V40 < 40%	High
                            V65 < 20%	High
            Left femor	Dmax< 45 Gy	High
            Right femor	Dmax< 45 Gy	High
            Small intestine	Dmax< 48 Gy	High
            Abbreviation: PTV planning target volume
            aVx is percent volume of the organ receiving x Gy radiation, Dmax is maximum dose received by the organ
            b“A-PTV” means the volume of A from which the PTV was excluded
            cRectum volume here is from the slice 1 cm above the highest part of the PTV to the slice 1 cm below the lowest part of the PTV in axial slices
            */

            if (PostOpFlag)
            {
                // postop prostate is usually 2 Gy/fx, max 70Gy, assuming max scenario and aiming much lower than typically!
                if (maxPrescribed > 70D) MessageBox.Show("Warning, prescriped dose is Higher than permitted for postop prostate, check constraints!");
                //femor
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorL, maxPrescribed * 45D / 70D, 005, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorR, maxPrescribed * 45D / 70D, 005, 70);
                //bladder
                //optSetup.AddPointObjective(Bladder, OptimizationObjectiveOperator.Upper, new DoseValue(maxPrescribed * (40D / 70D), DoseValue.DoseUnit.Gy), 040, 30);
                //optSetup.AddPointObjective(Bladder, OptimizationObjectiveOperator.Upper, new DoseValue(maxPrescribed * (65D / 66D), DoseValue.DoseUnit.Gy), 020, 30);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * 1.01, 000, 30);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * 40D / 70D, 60, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BladderMinusPTV, maxPrescribed * 65D / 68D, 30, 70);
                //rectum
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * 40D / 70D, 40, 30);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * 65D / 70D, 30, 30);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * 1.02D, 00, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumMinusPTV, maxPrescribed * 40D / 70D, 40, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumMinusPTV, maxPrescribed * 65D / 66D, 20, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumMinusPTV, maxPrescribed * 0.95D, 0, 70);
                //bowel, small intestines
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BowelMinusPTV, maxPrescribed * 0.95D, 0, 70);
            }
            else
            {
                //Rectum  1.8 Gy  50 % 50 Gy       0621, PMID 18947938 Prostate
                //Rectum  1.8 Gy  50 % 60 Gy       0415    Prostate
                //Rectum  1.8 Gy  35 % 65 Gy       0415    Prostate
                //Rectum  1.8 Gy  25 % 66.6 Gy     0621    Prostate
                //Rectum  1.8 Gy  25 % 70 Gy       0415    Prostate
                //Rectum  1.8 Gy  15 % 75 Gy       0415    Prostate
                // here prescription plays major role, treatment could be 2, 2.3, or even 2.5 Gy/fx. For safety reasons it is assumed constraints are given for 80 Gy prescription and scaled down linearly according for prescription.

                // bladder
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (50D / 80D), 055, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (65D / 80D), 050, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (66D / 80D), 040, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (70D / 80D), 030, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (75D / 80D), 025, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Bladder, maxPrescribed * (80D / 80D), 015, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BladderMinusPTV, maxPrescribed * (40D / 80D), 030, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BladderMinusPTV, maxPrescribed * 0.96, 000, 70);
                
                // rectum
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * (40D / 80D), 060, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * (60D / 80D), 050, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * (65D / 80D), 035, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * (70D / 80D), 025, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * (75D / 80D), 015, 00);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumCropped, maxPrescribed * 1.01, 00, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumMinusPTV, maxPrescribed * (40D / 80D), 050, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, RectumMinusPTV, maxPrescribed * 0.98, 000, 70);

                // femors
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorL, maxPrescribed * (50D / 80D), 005, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, FemorR, maxPrescribed * (50D / 80D), 005, 70);

                // bowel
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BowelMinusPTV, maxPrescribed * 0.95, 000, 70);
            }
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss); // pay special attention as this theoretically can make some lists items absolete and lead to crash
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