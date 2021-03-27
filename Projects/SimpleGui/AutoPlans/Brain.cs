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
using System.Configuration;
using SimpleGui.Models;
using System.Collections.Specialized;

namespace SimpleGui.AutoPlans
{
    public class BrainScript
    {
        //private Patient p;
        private List<Structure> PTVs = new List<Structure>();
        private List<Structure> PTVse = new List<Structure>();
        private List<Structure> PTVinters = new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure Body;
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure brainStem;
        private Structure brainStemMinusPTV;
        private Structure brainStemPRV;
        private Structure opticNerveL;
        private Structure opticNerveLPRV;
        private Structure opticNerveR;
        private Structure opticNerveRPRV;
        private Structure eyeL;
        private Structure eyeR;
        private Structure lensL;
        private Structure lensLPRV;
        private Structure lensR;
        private Structure lensRPRV;
        private Structure cochleaL;
        private Structure cochleaR;
        private Structure hippoL;
        private Structure hippoR;
        private Structure chiasm;
        private Structure chiasmPRV;

        //private Structure ParotidRMinusPTV;
        private OptimizationOptionsVMAT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));

        public void runBrainScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin,
            double IsocenterX, double IsocenterY, double IsocenterZ,
            //string MandibleId, string ParotidLId, string ParotidRId, string SpinalCordid, 
            string BrainStemId, string OpticNerveLid, string OpticNerveRid, string EyeLid, string EyeRid,
            string LensLid, string LensRid,
            string CochleaLid, string CochleaRid, 
            string HippoLid, string HippoRid,
            string ChiasmId)
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

            //ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            //configFileMap.ExeConfigFilename = "Brain.config";
            //var config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            ////var settingsSection = (AppSettingsSection)config.GetSection("SpinalCord/Max");
            ////var Type = settingsSection.Settings["Type"].Value;
            ////var Constraint = settingsSection.Settings["Constraint"].Value;
            ////var Weight = settingsSection.Settings["Weight"].Value;
            //var scConstraints = config.GetSectionGroup("SpinalCord");
            //int aa = scConstraints.Sections.Count;

            bool doCrop = true;
            if (double.IsNaN(CropFromBody)) doCrop = false;

            pat.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);
            Body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
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

            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, BodyShrinked, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(Body);
            // create helper structures
            brainStemPRV = StructureHelpers.createStructureIfNotExisting("0_BrnStmPrv", ss, "ORGAN");
            chiasmPRV = StructureHelpers.createStructureIfNotExisting("0_chiasmPrv", ss, "ORGAN");
            opticNerveLPRV = StructureHelpers.createStructureIfNotExisting("0_OnerveLPrv", ss, "ORGAN");
            opticNerveRPRV = StructureHelpers.createStructureIfNotExisting("0_OnerveRPrv", ss, "ORGAN");
            // ======================================================
            // plan specific structures and their cropped versions
            brainStem = StructureHelpers.getStructureFromStructureSet(BrainStemId, ss, true);
            opticNerveL = StructureHelpers.getStructureFromStructureSet(OpticNerveLid, ss, true);
            opticNerveR = StructureHelpers.getStructureFromStructureSet(OpticNerveRid, ss, true);
            eyeL = StructureHelpers.getStructureFromStructureSet(EyeLid, ss, true);
            eyeR = StructureHelpers.getStructureFromStructureSet(EyeRid, ss, true);
            lensL = StructureHelpers.getStructureFromStructureSet(LensLid, ss, true);
            lensLPRV = StructureHelpers.createStructureIfNotExisting("0_lensLprv", ss, "ORGAN");
            lensR = StructureHelpers.getStructureFromStructureSet(LensRid, ss, true);
            lensRPRV = StructureHelpers.createStructureIfNotExisting("0_lensRprv", ss, "ORGAN");
            cochleaL = StructureHelpers.getStructureFromStructureSet(CochleaLid, ss, true);
            cochleaR = StructureHelpers.getStructureFromStructureSet(CochleaRid, ss, true);
            hippoL = StructureHelpers.getStructureFromStructureSet(HippoLid, ss, true);
            hippoR = StructureHelpers.getStructureFromStructureSet(HippoRid, ss, true);
            chiasm = StructureHelpers.getStructureFromStructureSet(ChiasmId, ss, true);
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            if (brainStem != null) listOfOars.Add(brainStem);
            if (opticNerveL != null) listOfOars.Add(opticNerveL);
            if (opticNerveR != null) listOfOars.Add(opticNerveR);
            if (eyeL != null) listOfOars.Add(eyeL);
            if (eyeR != null) listOfOars.Add(eyeR);
            if (lensL != null) listOfOars.Add(lensL);
            if (lensR != null) listOfOars.Add(lensR);
            if (cochleaL!= null) listOfOars.Add(cochleaL);
            if (cochleaR!= null) listOfOars.Add(cochleaR);
            if (hippoL!= null) listOfOars.Add(hippoL);
            if (hippoR!= null) listOfOars.Add(hippoR);
            if (chiasm != null) listOfOars.Add(chiasm);

            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;

            if (brainStem != null)
            {
                brainStemPRV.SegmentVolume = brainStem.Margin(3);
                brainStemPRV.SegmentVolume = brainStemPRV.Sub(brainStem);
                listOfOars.Add(brainStemPRV);
            }
            if (chiasm != null)
            {
                chiasmPRV.SegmentVolume = chiasm.Margin(3);
                chiasmPRV.SegmentVolume = chiasmPRV.Sub(chiasm);
                listOfOars.Add(chiasmPRV);
            }
            if (opticNerveL != null)
            {
                opticNerveLPRV.SegmentVolume = opticNerveL.Margin(3);
                opticNerveLPRV.SegmentVolume = opticNerveLPRV.Sub(opticNerveL);
                listOfOars.Add(opticNerveLPRV);
            }
            if (opticNerveR != null)
            {
                opticNerveRPRV.SegmentVolume = opticNerveR.Margin(3);
                opticNerveRPRV.SegmentVolume = opticNerveRPRV.Sub(opticNerveR);
                listOfOars.Add(opticNerveRPRV);
            }
            if (lensL != null)
            {
                lensLPRV.SegmentVolume = lensL.Margin(3);
                lensLPRV.SegmentVolume = lensLPRV.Sub(lensL);
                listOfOars.Add(lensLPRV);
            }
            if (lensR != null)
            {
                lensRPRV.SegmentVolume = lensR.Margin(3);
                lensRPRV.SegmentVolume = lensRPRV.Sub(lensR);
                listOfOars.Add(lensRPRV);
            }

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
                    var arc3 = eps.AddArcBeam(machinePars, new VRect<double>(-75, -200, 75, 200), 0, 179, 0, GantryDirection.CounterClockwise, 270, iso);
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


            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);

            double maxPrescribed = NOF * presc[0].Value;
            double maxScale = 70D;

            // brainstem
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, brainStem, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, brainStemPRV, maxPrescribed, 0, 70);
            
            // chiasm
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, chiasm, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, chiasmPRV, maxPrescribed, 0, 70);

            // optic cenrves
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveL, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveLPRV, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveR, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveRPRV, maxPrescribed, 0, 70);
            
            // lens
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensL, maxPrescribed * 7 / maxScale, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensLPRV, maxPrescribed * 9 / maxScale, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensR, maxPrescribed * 7 / maxScale, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensRPRV, maxPrescribed*9 / maxScale, 0, 70);

            // eyes
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, eyeL, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, eyeR, maxPrescribed, 0, 70);
            
            // lenss
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensL, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, lensR, maxPrescribed, 0, 70);

            // cochlea
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, cochleaL, maxPrescribed * 40 / maxScale, 20);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, cochleaR, maxPrescribed * 40 / maxScale, 20);
            
            // hippo
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, hippoL, maxPrescribed * 40 / maxScale, 0);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, hippoR, maxPrescribed * 40 / maxScale, 0);

            // hippo
            //BeamHelpers.SetOptimizationMeanObjectiveInGy(opticNerveL)

            ss.RemoveStructure(BodyShrinked);
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
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