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
    public class HeadNeckScript
    {
        //private Patient p;
        private List<Structure> PTVs = new List<Structure>();
        private List<Structure> PTVse = new List<Structure>();
        private List<Structure> PTVinters = new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure Body;
        private Structure ptvEval;
        private Structure ptvEval3mm;
        private Structure mandible;
        private Structure parotidL;
        private Structure parotidLmPTV;
        private Structure parotidR;
        private Structure parotidRmPTV;
        private Structure spinalCord;
        private Structure spinalCordPRV;
        private Structure brainStem;
        private Structure brainStemPRV;
        private Structure opticNerveL;
        private Structure opticNerveLPRV;
        private Structure opticNerveR;
        private Structure opticNerveRPRV;
        private Structure eyeL;
        private Structure eyeR;
        private Structure cochleaL;
        private Structure cochleaR;
        private Structure chiasm;
        private Structure chiasmPRV;
        private Structure esophagus;
        private Structure esophagusCr;
        private Structure esophagusMinusPTV;

        //private Structure ParotidRMinusPTV;
        private OptimizationOptionsVMAT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));

        public void runHeadNeckScript(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
            ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
            int nof, List<KeyValuePair<string, double>> prescriptions, double collimatorAngle, double CropFromBody, bool JawTrackingOn, int numOfArcs,
            double isocenterOffsetZ, string selectedTargetForIso, string selectedOffsetOrigin,
            double IsocenterX, double IsocenterY, double IsocenterZ,
            string MandibleId, string ParotidLId, string ParotidRId, string SpinalCordid, string BrainStemId, string OpticNerveLid, string OpticNerveRid, string EyeLid, string EyeRid, string CochleaLid, string CochleaRid, string ChiasmId, string EsophagusId)
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
            spinalCordPRV = StructureHelpers.createStructureIfNotExisting("0_SpinalPrv", ss, "ORGAN");
            brainStemPRV = StructureHelpers.createStructureIfNotExisting("0_BrnStmPrv", ss, "ORGAN");
            chiasmPRV = StructureHelpers.createStructureIfNotExisting("0_chiasmPrv", ss, "ORGAN");
            opticNerveLPRV = StructureHelpers.createStructureIfNotExisting("0_OnerveLPrv", ss, "ORGAN");
            opticNerveRPRV = StructureHelpers.createStructureIfNotExisting("0_OnerveRPrv", ss, "ORGAN");
            // ======================================================
            // plan specific structures and their cropped versions
            mandible = StructureHelpers.getStructureFromStructureSet(MandibleId, ss, true);
            parotidL = StructureHelpers.getStructureFromStructureSet(ParotidLId, ss, true);
            parotidLmPTV = StructureHelpers.createStructureIfNotExisting("0_prtdL-ptv", ss, "ORGAN");
            parotidR = StructureHelpers.getStructureFromStructureSet(ParotidRId, ss, true);
            parotidRmPTV = StructureHelpers.createStructureIfNotExisting("0_prtdR-ptv", ss, "ORGAN");
            spinalCord = StructureHelpers.getStructureFromStructureSet(SpinalCordid, ss, true);
            brainStem = StructureHelpers.getStructureFromStructureSet(BrainStemId, ss, true);
            opticNerveL = StructureHelpers.getStructureFromStructureSet(OpticNerveLid, ss, true);
            opticNerveR = StructureHelpers.getStructureFromStructureSet(OpticNerveRid, ss, true);
            eyeL = StructureHelpers.getStructureFromStructureSet(EyeLid, ss, true);
            eyeR = StructureHelpers.getStructureFromStructureSet(EyeRid, ss, true);
            cochleaL = StructureHelpers.getStructureFromStructureSet(CochleaLid, ss, true);
            cochleaR = StructureHelpers.getStructureFromStructureSet(CochleaRid, ss, true);
            chiasm = StructureHelpers.getStructureFromStructureSet(ChiasmId, ss, true);
            esophagus = StructureHelpers.getStructureFromStructureSet(EsophagusId, ss, true);
            esophagusCr = StructureHelpers.createStructureIfNotExisting("0_esoCr", ss, "ORGAN");
            esophagusMinusPTV = StructureHelpers.createStructureIfNotExisting("0_eso-PTV", ss, "ORGAN");
            // check inputs
            List<Structure> listOfOars = new List<Structure>();
            if (mandible != null) listOfOars.Add(mandible);
            if (parotidL != null)
            {
                listOfOars.Add(parotidL);
                parotidLmPTV.SegmentVolume = parotidL.Sub(ptvEval);
            }
            if (parotidR != null)
            {
                listOfOars.Add(parotidR);
                parotidRmPTV.SegmentVolume = parotidR.Sub(ptvEval);
            }

            if (spinalCord != null) listOfOars.Add(spinalCord);
            if (brainStem != null) listOfOars.Add(brainStem);
            if (opticNerveL != null) listOfOars.Add(opticNerveL);
            if (opticNerveR != null) listOfOars.Add(opticNerveR);
            if (eyeL != null) listOfOars.Add(eyeL);
            if (eyeR != null) listOfOars.Add(eyeR);
            if (cochleaL != null) listOfOars.Add(cochleaL);
            if (cochleaR != null) listOfOars.Add(cochleaR);
            if (chiasm != null) listOfOars.Add(chiasm);
            if (esophagus != null)
            {
                listOfOars.Add(esophagus);
                StructureHelpers.CopyStructureInBounds(esophagusCr, esophagus, ss.Image, (ptvEval.MeshGeometry.Bounds.Z - 10, ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ + 10));
                esophagusMinusPTV.SegmentVolume = esophagusCr.Sub(ptvEval);
            }

            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            foreach (var p in listOfOars)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;


            if (spinalCord != null)
            {
                spinalCordPRV.SegmentVolume = spinalCord.Margin(3);
                spinalCordPRV.SegmentVolume = spinalCordPRV.Sub(spinalCord);
                listOfOars.Add(spinalCordPRV);
            }
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
                var arc1 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), 360 - collimatorAngle, 270, 179, GantryDirection.Clockwise, 0, iso);
                var arc2 = eps.AddArcBeam(machinePars, new VRect<double>(-50, -50, 50, 50), collimatorAngle, 90, 181, GantryDirection.CounterClockwise, 0, iso);
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


            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF, 0.95);

            double maxPrescribed = NOF * presc[0].Value;
            double maxScale = 70D;

            /*
             * //https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6481934/
Organ-at-risk	        Endpoint	                Suggested constraint	            Expected complication rate
Brainstem	            Neuropathy or necrosis	    Max dose    <54 Gy	                <5%
Optic nerve / chiasm	Optic neuropathy	        Max dose    <55 Gy	                <3%
Cochlea	                Hearing loss	            Mean dose   <45 Gy	                <30%
Parotid glands          Reduced salivary function	Mean dose   <25 Gy for both glands  <20%
                                                    Mean dose   <20 Gy for single gland	<20%
Pharyngeal constrictors	Dysphagia	                Mean dose   <50 Gy	                <20%
Larynx	                Aspiration                  Mean dose   <50 Gy                  <30%
                        Edema	                    Mean dose   <44 Gy                  <20%
Esophagus	            Acute esophagitis	                V35 <50%                    <30%
                                                            V50 <40%                    <30%
                                                            V70 <20%	                <30%
             */
            // trie to export hardcoded constraints to external file, need to work on this further
            //ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            //configFileMap.ExeConfigFilename = "HeadNeck.config";
            //Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            //double scale = maxPrescribed / maxScale;
            ////BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, mandible, "Mandible");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, spinalCord, "SpinalCord");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, spinalCordPRV, "SpinalCordPrv");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, brainStem, "BrainStem");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, brainStemPRV, "BrainStemPrv");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, opticNerveL, "OpticNerveL");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, opticNerveLPRV, "OpticNerveLPrv");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, opticNerveR, "OpticNerveR");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, opticNerveRPRV, "OpticNerveRPrv");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, parotidL, "ParotidL");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, parotidLmPTV, "ParotidLminusPTV");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, parotidR, "ParotidR");
            //BeamHelpers.setConstraintsFromConfigurationFile(optSetup, config, scale, parotidRmPTV, "ParotidRminusPTV");
            // mandible
            
            
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, mandible, maxPrescribed, 000, 70);
            //parotids
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, parotidL, maxPrescribed * 20 / maxScale, 0);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, parotidR, maxPrescribed * 20 / maxScale, 0);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, parotidLmPTV, maxPrescribed * 20 / maxScale, 20, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, parotidRmPTV, maxPrescribed * 20 / maxScale, 20, 70);
            // cord
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCord, maxPrescribed * 40D / maxScale, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, spinalCordPRV, maxPrescribed * 45D / maxScale, 000, 70);
            // brainstem
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, brainStem, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, brainStemPRV, maxPrescribed, 0, 70);
            // optic cenrves
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveL, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveLPRV, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveR, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, opticNerveRPRV, maxPrescribed, 0, 70);
            // eyes
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, eyeL, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, eyeR, maxPrescribed, 0, 70);
            // cochlea
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, cochleaL, maxPrescribed * 40 / maxScale, 20);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, cochleaR, maxPrescribed * 40 / maxScale, 20);
            // esophagus
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, esophagusCr, maxPrescribed, 0, 70);
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, esophagusMinusPTV, maxPrescribed / 2, 0);

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