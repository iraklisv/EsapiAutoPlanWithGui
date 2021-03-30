using GalaSoft.MvvmLight.Messaging;
using SimpleGui.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SimpleGui.AutoPlans
{
    public class Breast
    {
        private List<Structure> PTVs = new List<Structure>();
        private List<Structure> PTVse = new List<Structure>();
        private List<Structure> PTVinters = new List<Structure>();
        private List<Structure> Rings = new List<Structure>();
        private Structure Heart;
        private Structure LAD;
        private Structure LADprv;
        private Structure Esophagus;
        private Structure SpinalCord;
        private Structure SpinalCordPrv;
        private Structure L_LungIpsi;
        private Structure L_LungContra;
        private Structure L_BreastContra;
        private Structure L_ptvSupra;
        private Structure L_ptvBreast;
        private Structure L_ptvBoost;
        private Structure L_ptvIMN;
        private Structure L_ptvEval;
        //private Structure L_ptvEvalBelowIsocenter;
        private Structure R_LungIpsi;
        private Structure R_LungContra;
        private Structure R_BreastContra;
        private Structure R_ptvSupra;
        private Structure R_ptvBreast;
        private Structure R_ptvBoost;
        private Structure R_ptvIMN;
        private Structure R_ptvEval;
        //private Structure R_ptvEvalBelowIsocenter;

        private Structure ptvEval;

        //private OptimizationOptionsIMRT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private VVector L_isocenter;
        private VVector R_isocenter;
        //private VVector isocenter;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));
        //public void runBreastFif(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
        //        ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
        //        int nof, List<KeyValuePair<string, double>> prescriptions,
        //        string SelectedHeart, string SlectedLAD, string SelectedEsophagus, string SelectedSpinalCord,
        //        double L_SelectedMedGantryAngle, double L_SelectedMedColAngle, double L_SelectedCropFromBody,
        //        double L_SelectedIsocenterX, double L_SelectedIsocenterY, double L_SelectedIsocenterZ,
        //        string L_SelectedLungIpsi, string L_SelectedLungContra, string L_SelectedBreastContra,
        //        string L_SelectedSupraPTV, string L_SelectedBreastPTV, string L_SelectedBoostPTV, string L_SelectedIMNPTV,
        //        double R_SelectedMedGantryAngle, double R_SelectedMedColAngle, double R_SelectedCropFromBody,
        //        double R_SelectedIsocenterX, double R_SelectedIsocenterY, double R_SelectedIsocenterZ,
        //        string R_SelectedLungIpsi, string R_SelectedLungContra, string R_SelectedBreastContra,
        //        string R_SelectedSupraPTV, string R_SelectedBreastPTV, string R_SelectedBoostPTV, string R_SelectedIMNPTV
        //    )
        //{
        //    p = pat;
        //    eps = eps1;
        //    ss = ss1;
        //    presc = prescriptions;
        //    NOF = nof;
        //    mlcId = MlcId;
        //    if (presc.Count == 0)
        //    {
        //        MessageBox.Show("Please add target");
        //        return;
        //    }
        //    bool tryFifBuild = true;

        //    bool doCrop = true;
        //    if (double.IsNaN(L_SelectedCropFromBody)) doCrop = false;

        //    pat.BeginModifications();

        //    // HERE REMOVE OLD OPTIMIZATION STRUCTURES!
        //    StructureHelpers.ClearAllOptimizationContours(ss);
        //    presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

        //    #region Prepare general Structures
        //    Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
        //    Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
        //    if (doCrop) BodyShrinked.SegmentVolume = body.Margin(-L_SelectedCropFromBody);
        //    LungIpsi = StructureHelpers.getStructureFromStructureSet(L_SelectedungIpsi, ss, true);
        //    LungContra = StructureHelpers.getStructureFromStructureSet(L_SelectedungContra, ss, true);
        //    Structure target = StructureHelpers.getStructureFromStructureSet(presc.FirstOrDefault().Key, ss, true);
        //    Structure targetCr = StructureHelpers.getStructureFromStructureSet("ctvcr", ss, false);
        //    Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss, true);
        //    BreastContra = StructureHelpers.getStructureFromStructureSet(SelectedBreastContra, ss, true);
        //    LAD = StructureHelpers.getStructureFromStructureSet(SlectedLAD, ss, true);
        //    ptvSupra = StructureHelpers.getStructureFromStructureSet(SelectedSupraPTV, ss, true);

        //    if (LungIpsi == null) { MessageBox.Show("Need ipsilateral lung"); return; }

        //    targetCr = StructureHelpers.createStructureIfNotExisting("CTVcr", ss, "PTV");
        //    if (doCrop)
        //        targetCr.SegmentVolume = target.And(BodyShrinked);

        //    ptvEval = StructureHelpers.createStructureIfNotExisting("0_ptvEval", ss, "PTV");
        //    foreach (var p in presc)
        //        PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
        //    // make all PTVs add to ptvEval
        //    foreach (var p in PTVs) if (ptvEval.IsEmpty) ptvEval.SegmentVolume = p.Margin(0);
        //        else ptvEval.SegmentVolume = ptvEval.Or(p);


        //    if (doCrop)
        //        ptvEval.SegmentVolume = ptvEval.And(BodyShrinked);
        //    PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, doCrop);


        //    if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

        //    #endregion

        //    Course Course = eps.Course;
        //    //var activePlan = Course.ExternalPlanSetups.);

        //    eps = Course.AddExternalPlanSetup(ss);
        //    eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);

        //    #region isocenter positioning
        //    isocenter = new VVector();
        //    // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
        //    if (ptvSupra == null)
        //        isocenter = ptvEval.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
        //    else
        //    {
        //        var posZmax = ptvEval.MeshGeometry.Bounds.Z + 200 - 20; // leave margin for skinflash
        //        var posZtop = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ;
        //        var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
        //        double posZ;
        //        posZ = lungTop > posZtop ? lungTop : posZtop;
        //        posZ = posZ > posZmax ? posZmax : posZ;
        //        isocenter = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
        //            ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
        //            posZ);
        //    }
        //    if (!double.IsNaN(IsocenterX)) isocenter.x = IsocenterX;
        //    if (!double.IsNaN(IsocenterY)) isocenter.y = IsocenterY;
        //    if (!double.IsNaN(IsocenterZ)) isocenter.z = IsocenterZ;
        //    #endregion

        //    machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 600, "STATIC", ""); // for fif manually change energy to 6x/dr600, static!

        //    Beam med0 = null;
        //    double medstartCA = 0;
        //    double medendCA = 0;
        //    //double latstartCA = 0;
        //    //double latendCA = 0; //for imrt
        //    double stepCA = 1;
        //    if (SelectedBreastSide == "R")
        //    {
        //        medstartCA = 320;
        //        medendCA = 360;
        //        //latstartCA = 20;
        //        //latendCA = 40;
        //    }
        //    if (SelectedBreastSide == "L")
        //    {
        //        medstartCA = 20;
        //        medendCA = 40;
        //        //latstartCA = 320;
        //        //latendCA = 360;

        //    }

        //    #region field placement
        //    if (!double.IsNaN(L_SelectedMedGantryAngle) && !double.IsNaN(L_SelectedMedColAngle))
        //    {
        //        if (ptvSupra == null)
        //            med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, L_SelectedMedColAngle, L_SelectedMedGantryAngle, 0, isocenter); // if no supra, use col angle
        //        else
        //            med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, 0, L_SelectedMedGantryAngle, 0, isocenter); // if supra, col angle = 0
        //    }
        //    #region medial field collimator and gantry angle optimization
        //    else
        //    {
        //        // try to find optimale angle
        //        if (ptvSupra == null)
        //        {
        //            // get optimal gantry angle
        //            var optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, target, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
        //            // get optimal collimator rotation for optimal angle, use beam's eye view for dat
        //            var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, isocenter, ss, target, LungIpsi, optimalGantryAngle, medstartCA, medendCA, stepCA, SelectedBreastSide, true); // get optimal angle
        //            med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
        //        }
        //        else
        //        {
        //            var optimalAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, target, LungIpsi, isocenter, SelectedBreastSide);
        //            med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, 0, optimalAngle, 0, isocenter); // if supra, col angle = 0, that's it
        //        }

        //    }
        //    #endregion // field angle and col optimization
        //    #endregion // end of field placement overall

        //    #region fit medial field and generate opposing field
        //    if (SelectedBreastSide == "L") med0.FitMLCToStructure(BeamHelpers.LBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
        //    if (SelectedBreastSide == "R") med0.FitMLCToStructure(BeamHelpers.RighBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
        //    if (ptvSupra != null) // if supraclav, set Y2 to 0
        //    {
        //        var fieldPars = med0.GetEditableParameters();

        //        var pars = med0.GetEditableParameters();
        //        var setJawsTo = new VRect<double>(fieldPars.ControlPoints.FirstOrDefault().JawPositions.X1,
        //            fieldPars.ControlPoints.FirstOrDefault().JawPositions.Y1,
        //            fieldPars.ControlPoints.FirstOrDefault().JawPositions.X2,
        //            0);
        //        pars.SetJawPositions(setJawsTo);
        //    }
        //    Beam lat0 = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, med0, SelectedBreastSide);
        //    lat0.FitMLCToStructure(BeamHelpers.LBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
        //    BeamHelpers.openMLCoutOfBody(med0, false);
        //    BeamHelpers.openMLCoutOfBody(lat0, true);
        //    MessageBox.Show("created fields with MLC");
        //    #endregion

        //    #region calculate dose profile
        //    double med0Angle = med0.ControlPoints[0].GantryAngle - 180 - 90;
        //    double lat0Angle = lat0.ControlPoints[0].GantryAngle - 90;
        //    double profileAngle = (med0Angle + lat0Angle) / 2 * Math.PI / 180; // and convert it to Radians
        //    eps.CalculateDose();
        //    MessageBox.Show("calculated dose, trying to measure profile");
        //    if (eps.IsDoseValid)
        //    {
        //        BeamHelpers.NormalizePlanToStructureCoverageRelAbs(eps, targetCr);
        //        //coverage = plan.GetDoseAtVolume(targetCr, 96, VolumePresentation.Relative, DoseValuePresentation.Absolute);
        //        // get the dose profile in tangential direction is isocenter axial plane
        //        VVector profileStart = new VVector();
        //        VVector profileEnd = new VVector();
        //        double profileEndPointDistanceToIso = 150;
        //        profileEnd.x = isocenter.x + profileEndPointDistanceToIso * Math.Sin(profileAngle);
        //        profileEnd.y = isocenter.y + profileEndPointDistanceToIso * Math.Cos(profileAngle);
        //        profileEnd.z = isocenter.z;
        //        profileStart.x = isocenter.x - profileEndPointDistanceToIso * Math.Sin(profileAngle);
        //        profileStart.y = isocenter.y - profileEndPointDistanceToIso * Math.Cos(profileAngle);
        //        profileStart.z = isocenter.z;
        //        int binSize = Convert.ToInt32(2 * profileEndPointDistanceToIso); // make 1mm bin size
        //        double[] size = new double[binSize];

        //        List<double> maxProfileDoses = BeamHelpers.measureProfileLocalMaximums(size, eps, profileStart, profileEnd);
        //        MessageBox.Show("measured profiles");

        //        if (maxProfileDoses.Count() == 2)
        //        {
        //            double localMaximaRatios = maxProfileDoses[1] / maxProfileDoses[0];
        //            BeamParameters pars1 = med0.GetEditableParameters();
        //            BeamParameters pars2 = lat0.GetEditableParameters();
        //            pars1.WeightFactor = (localMaximaRatios - 1) / 2 + 1;
        //            pars2.WeightFactor = -(localMaximaRatios - 1) / 2 + 1;
        //            med0.ApplyParameters(pars1);
        //            lat0.ApplyParameters(pars2);
        //        }
        //    }
        //    MessageBox.Show("corrected field weights");
        //    #endregion

        //    if (tryFifBuild)
        //    {
        //        #region format fields
        //        med0.Id = string.Format("med.0");
        //        lat0.Id = string.Format("lat.0");
        //        med0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
        //        lat0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
        //        MessageBox.Show("created DRRs and named fields");
        //        #endregion

        //        #region convert isodoses to structures and operate on them
        //        Structure s_idv5p = StructureHelpers.createStructureIfNotExisting("0_id5p", ss, "Control");
        //        VVector lungVector = LungIpsi.CenterPoint;
        //        lungVector.z = isocenter.z;
        //        double fifBinWidthPercent = 3.5;
        //        if (eps.IsDoseValid)
        //        {
        //            Dose dose = eps.Dose;
        //            double maxDose = eps.Dose.DoseMax3D.Dose;
        //            double fifRemovesAbove = 108.5;
        //            double toBeRemovedPercent = maxDose - fifRemovesAbove;
        //            int nBin = Convert.ToInt32(toBeRemovedPercent / fifBinWidthPercent) + 1;
        //            List<double> fifIDLs = new List<double> { };
        //            for (int bin = 0; bin < nBin; bin++) fifIDLs.Add(fifRemovesAbove + bin * fifBinWidthPercent);
        //            fifIDLs.Sort();
        //            fifIDLs.Reverse();
        //            //List<double> fifIDLs = new List<double> { 115, 113, 111};
        //            //List<double> fifIDLs = new List<double> {109};
        //            BeamHelpers.findMLCEdgeXAndInitiateMap();
        //            foreach (double fifIDL in fifIDLs)
        //            {
        //                List<Structure> hotspots = BeamHelpers.createBreastFifHotSpotContour(ss, eps, fifIDL);


        //                foreach (Structure hotspot in hotspots)
        //                {
        //                    Console.WriteLine($"hotspot id is {hotspot.Id}");
        //                    VVector hsCenter = hotspot.CenterPoint;
        //                    hsCenter.z = isocenter.z;
        //                    VVector deltaHS = hsCenter - lungVector;
        //                    VVector deltaIso = isocenter - lungVector;

        //                    double tanHS = deltaHS.y / deltaHS.x;
        //                    double tanIso = deltaIso.y / deltaIso.x;
        //                    if (tanHS <= tanIso)
        //                    {
        //                        //MessageBox.Show(string.Format("hs len {0}\n isolen {1}",deltaHS.Length.ToString(),deltaIso.Length.ToString()),"info",MessageBoxButton.OK,MessageBoxImage.Information);
        //                        if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, med0, false);
        //                        else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, med0, true);
        //                    }
        //                    else
        //                    {
        //                        if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lat0, true);
        //                        else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lat0, false);
        //                    }
        //                }
        //            }
        //        }
        //        PlanSetup np = Course.CopyPlanSetup(eps);
        //        np.Id = $"{eps.Id}c";
        //        MessageBox.Show("created isodose structures");
        //        #endregion

        //        #region create copy of plan and apply fif weights, recalculate
        //        ExternalPlanSetup newPlan = Course.ExternalPlanSetups.First(x => x.Id.Equals(np.Id));
        //        List<Beam> beams = newPlan.Beams.ToList();
        //        Beam med = beams.FirstOrDefault(x => x.Id.Contains("med") && x.Id.EndsWith(".0"));
        //        Beam lat = beams.FirstOrDefault(x => x.Id.Contains("lat") && x.Id.EndsWith(".0"));
        //        var medialBase = BeamHelpers.getBaseName(med.Id, 1);
        //        var lateralBase = BeamHelpers.getBaseName(lat.Id, 1);
        //        foreach (Beam b in beams)
        //        {
        //            if (b.Id.Contains(medialBase) && !b.Id.Equals(med.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, med);
        //            if (b.Id.Contains(lateralBase) && !b.Id.Equals(lat.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, lat);
        //        }
        //        MessageBox.Show("copied plan and named fifs");
        //        #endregion
        //    }

        //    StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
        //    StructureHelpers.ClearAllOptimizationContours(ss);
        //    //ss.RemoveStructure(BodyShrinked);

        //    MessageBox.Show("All done");
        //}

        public void PrepareIMRT(Patient p1, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
                int nof, List<KeyValuePair<string, double>> prescriptions,
                string SelectedHeart, string SlectedLAD, string SelectedEsophagus, string SelectedSpinalCord, double SelectedCropFromBody,
                double L_SelectedMedGantryAngle, double L_SelectedMedColAngle,
                double L_SelectedIsocenterX, double L_SelectedIsocenterY, double L_SelectedIsocenterZ,
                string L_SelectedLungIpsi, string L_SelectedLungContra, string L_SelectedBreastContra,
                string L_SelectedSupraPTV, string L_SelectedBreastPTV, string L_SelectedBoostPTV, string L_SelectedIMNPTV,
                double R_SelectedMedGantryAngle, double R_SelectedMedColAngle,
                double R_SelectedIsocenterX, double R_SelectedIsocenterY, double R_SelectedIsocenterZ,
                string R_SelectedLungIpsi, string R_SelectedLungContra, string R_SelectedBreastContra,
                string R_SelectedSupraPTV, string R_SelectedBreastPTV, string R_SelectedBoostPTV, string R_SelectedIMNPTV
            )
        {

            // for debugging only, otherwise comment out
            //L_SelectedMedGantryAngle = 310;
            //L_SelectedIsocenterX = 80;
            //L_SelectedIsocenterY = -80;
            //L_SelectedIsocenterZ = -1190;
            //R_SelectedMedGantryAngle = 50;
            //R_SelectedIsocenterX = -80;
            //R_SelectedIsocenterY = -80;
            //R_SelectedIsocenterZ = -1190;

            Messenger.Default.Send("Script Running Started");
            p = p1;
            eps = eps1;
            ss = ss1;
            presc = prescriptions;
            NOF = nof;
            mlcId = MlcId;

            bool hasLSide = false;
            bool hasRSide = false;
            bool isBilateral = false;

            if (L_SelectedBoostPTV != string.Empty ||
                L_SelectedBreastPTV != string.Empty ||
                L_SelectedIMNPTV != string.Empty ||
                L_SelectedSupraPTV != string.Empty)
                hasLSide = true;
            if (R_SelectedBoostPTV != string.Empty ||
                R_SelectedBreastPTV != string.Empty ||
                R_SelectedIMNPTV != string.Empty ||
                R_SelectedSupraPTV != string.Empty)
                hasRSide = true;

            if (hasLSide && hasRSide) isBilateral = true;

            if (presc.Count == 0)
            {
                MessageBox.Show("Please add target");
                return;
            }
            if (hasLSide && L_SelectedLungIpsi == string.Empty)
            {
                MessageBox.Show("Please seleng lung ipsilateral");
                return;
            }
            if (hasRSide && R_SelectedLungIpsi == string.Empty)
            {
                MessageBox.Show("Please seleng lung ipsilateral");
                return;
            }
            bool doCrop = true;

            if (double.IsNaN(SelectedCropFromBody)) doCrop = false;

            p.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            if (doCrop) BodyShrinked.SegmentVolume = body.Margin(-SelectedCropFromBody);

            // creaste helper structures for heart, lad, spinal cord and esophagus
            Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss, true);
            LAD = StructureHelpers.getStructureFromStructureSet(SlectedLAD, ss, true);
            LADprv = StructureHelpers.createStructureIfNotExisting("0_LADprv", ss, "ORGAN");
            Esophagus = StructureHelpers.getStructureFromStructureSet(SelectedEsophagus, ss, true);
            if (LAD != null)
            {
                LADprv.SegmentVolume = LAD.Margin(4);
                LADprv.SegmentVolume = LADprv.Sub(LAD);
            }
            SpinalCord = StructureHelpers.getStructureFromStructureSet(SelectedSpinalCord, ss, true);
            SpinalCordPrv = StructureHelpers.createStructureIfNotExisting("0_SpinalPrv", ss, "ORGAN");
            if (SpinalCord != null)
            {
                SpinalCordPrv.SegmentVolume = SpinalCord.Margin(4);
                SpinalCordPrv.SegmentVolume = SpinalCordPrv.Sub(SpinalCord);
            }

            if (hasLSide)
            {
                L_LungIpsi = StructureHelpers.getStructureFromStructureSet(L_SelectedLungIpsi, ss, true);
                L_LungContra = StructureHelpers.getStructureFromStructureSet(L_SelectedLungContra, ss, true);
                L_BreastContra = StructureHelpers.getStructureFromStructureSet(L_SelectedBreastContra, ss, true);
                L_ptvSupra = StructureHelpers.getStructureFromStructureSet(L_SelectedSupraPTV, ss, true);
                L_ptvBreast = StructureHelpers.getStructureFromStructureSet(L_SelectedBreastPTV, ss, true);
                L_ptvBoost = StructureHelpers.getStructureFromStructureSet(L_SelectedBoostPTV, ss, true);
                L_ptvIMN = StructureHelpers.getStructureFromStructureSet(L_SelectedIMNPTV, ss, true);
                L_ptvEval = StructureHelpers.createStructureIfNotExisting("0_LptvEval", ss, "PTV");
                if (L_ptvEval.IsEmpty) L_ptvEval.SegmentVolume = L_ptvBreast.Margin(0);
                if (L_ptvEval.IsEmpty) L_ptvEval.SegmentVolume = L_ptvBoost.Margin(0);
                if (L_ptvEval.IsEmpty) L_ptvEval.SegmentVolume = L_ptvSupra.Margin(0);
                if (L_ptvEval.IsEmpty) L_ptvEval.SegmentVolume = L_ptvIMN.Margin(0);
                if (L_ptvSupra != null) L_ptvEval.SegmentVolume = L_ptvEval.Or(L_ptvSupra);
                if (L_ptvBoost != null) L_ptvEval.SegmentVolume = L_ptvEval.Or(L_ptvBoost);
                if (L_ptvIMN != null) L_ptvEval.SegmentVolume = L_ptvEval.Or(L_ptvIMN);
                if (doCrop)
                    L_ptvEval.SegmentVolume = L_ptvEval.And(BodyShrinked);
            }
            if (hasRSide)
            {
                R_LungIpsi = StructureHelpers.getStructureFromStructureSet(R_SelectedLungIpsi, ss, true);
                R_LungContra = StructureHelpers.getStructureFromStructureSet(R_SelectedLungContra, ss, true);
                R_BreastContra = StructureHelpers.getStructureFromStructureSet(R_SelectedBreastContra, ss, true);
                R_ptvSupra = StructureHelpers.getStructureFromStructureSet(R_SelectedSupraPTV, ss, true);
                R_ptvBreast = StructureHelpers.getStructureFromStructureSet(R_SelectedBreastPTV, ss, true);
                R_ptvBoost = StructureHelpers.getStructureFromStructureSet(R_SelectedBoostPTV, ss, true);
                R_ptvIMN = StructureHelpers.getStructureFromStructureSet(R_SelectedIMNPTV, ss, true);
                R_ptvEval = StructureHelpers.createStructureIfNotExisting("0_RptvEval", ss, "PTV");
                //R_ptvEvalBelowIsocenter = StructureHelpers.createStructureIfNotExisting("0_RptvSplit", ss, "PTV");
                if (R_ptvEval.IsEmpty) R_ptvEval.SegmentVolume = R_ptvBreast.Margin(0);
                if (R_ptvEval.IsEmpty) R_ptvEval.SegmentVolume = R_ptvBoost.Margin(0);
                if (R_ptvEval.IsEmpty) R_ptvEval.SegmentVolume = R_ptvSupra.Margin(0);
                if (R_ptvEval.IsEmpty) R_ptvEval.SegmentVolume = R_ptvIMN.Margin(0);
                if (R_ptvSupra != null) R_ptvEval.SegmentVolume = R_ptvEval.Or(R_ptvSupra);
                if (R_ptvBoost != null) R_ptvEval.SegmentVolume = R_ptvEval.Or(R_ptvBoost);
                if (R_ptvIMN != null) R_ptvEval.SegmentVolume = R_ptvEval.Or(R_ptvIMN);
                if (doCrop)
                    R_ptvEval.SegmentVolume = R_ptvEval.And(BodyShrinked);
            }

            ptvEval = StructureHelpers.createStructureIfNotExisting("0_ptvEval", ss, "PTV");
            // segment helper structures
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            // make all PTVs add to ptvEval
            PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, doCrop);
            if (hasLSide) ptvEval.SegmentVolume = L_ptvEval.Margin(0);
            if (hasRSide) ptvEval.SegmentVolume = R_ptvEval.Margin(0);
            if (isBilateral) ptvEval.SegmentVolume = L_ptvEval.Or(R_ptvEval);
            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }
            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, ptvEval, presc, ss, BodyShrinked, doCrop);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);
            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(Heart);
            listOfOars.Add(SpinalCord);
            if (isBilateral)
            {
                listOfOars.Add(L_LungIpsi);
                listOfOars.Add(R_LungIpsi);
            }
            else
            {
                if (hasLSide)
                {
                    listOfOars.Add(L_LungIpsi);
                    listOfOars.Add(L_LungContra);
                    listOfOars.Add(L_BreastContra);
                }
                if (hasRSide)
                {
                    listOfOars.Add(R_LungIpsi);
                    listOfOars.Add(R_LungContra);
                    listOfOars.Add(R_BreastContra);
                }
            }
            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;

            //Rings = StructureHelpers.CreateRings(PTVse, ss, body, ptvEval, 50);
            List<string> boosts = new List<string>();
            if (hasLSide) boosts.Add(L_SelectedBoostPTV);
            if (hasRSide) boosts.Add(R_SelectedBoostPTV);
            Rings = StructureHelpers.CreateRingsForBreastSIB(PTVse, listOfOars, ss, body, ptvEval, 50, boosts);

            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc[0].Value, DoseValue.DoseUnit.Gy), 1.0);




            // prepare fields and stuff
            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 1400, "STATIC", "FFF"); // for fif manually change energy to 6x/dr600, static!
            Beam L_med0 = null;
            Beam L_med20 = null;
            Beam L_med40 = null;
            Beam L_lat0 = null;
            Beam L_lat20 = null;
            Beam L_lat40 = null;
            Beam L_cross = null;
            Beam L_scap = null;
            Beam L_sclat = null;
            Beam L_scpa = null;

            double L_optimalGantryAngle = double.NaN;
            double L_medstartCA = 20;
            double L_medendCA = 40;
            double L_latstartCA = 320;
            double L_latendCA = 360;
            Beam R_med0 = null;
            Beam R_med20 = null;
            Beam R_med40 = null;
            Beam R_lat0 = null;
            Beam R_lat20 = null;
            Beam R_lat40 = null;
            Beam R_cross = null;
            Beam R_scap = null;
            Beam R_sclat = null;
            Beam R_scpa = null;
            double R_optimalGantryAngle = double.NaN;
            double R_medstartCA = 320;
            double R_medendCA = 360;
            double R_latstartCA = 20;
            double R_latendCA = 40;
            double stepCA = 1;

            L_isocenter = new VVector();
            R_isocenter = new VVector();
            //if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (hasLSide)
            {
                if (L_ptvSupra == null)
                    L_isocenter = L_ptvEval.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
                else
                {
                    var posZmax = L_ptvEval.MeshGeometry.Bounds.Z + 200 - 30; // leave margin for skinflash
                    var posZtop = L_ptvEval.MeshGeometry.Bounds.Z + L_ptvEval.MeshGeometry.Bounds.SizeZ;
                    var lungTop = L_ptvEval.MeshGeometry.Bounds.Z + L_ptvEval.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                    double posZ;
                    posZ = lungTop > posZtop ? lungTop : posZtop;
                    posZ = posZ > posZmax ? posZmax : posZ;
                    L_isocenter = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                        ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                        posZ);
                }
                if (!double.IsNaN(L_SelectedIsocenterX)) L_isocenter.x = L_SelectedIsocenterX;
                if (!double.IsNaN(L_SelectedIsocenterY)) L_isocenter.y = L_SelectedIsocenterY;
                if (!double.IsNaN(L_SelectedIsocenterZ)) L_isocenter.z = L_SelectedIsocenterZ;
            }

            if (hasRSide)
            {
                if (R_ptvSupra == null)
                    R_isocenter = R_ptvEval.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
                else
                {
                    var posZmax = R_ptvEval.MeshGeometry.Bounds.Z + 200 - 30; // leave margin for skinflash
                    var posZtop = R_ptvEval.MeshGeometry.Bounds.Z + R_ptvEval.MeshGeometry.Bounds.SizeZ;
                    var lungTop = R_ptvEval.MeshGeometry.Bounds.Z + R_ptvEval.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                    double posZ;
                    posZ = lungTop > posZtop ? lungTop : posZtop;
                    posZ = posZ > posZmax ? posZmax : posZ;
                    R_isocenter = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                        ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                        posZ);
                }
                if (!double.IsNaN(R_SelectedIsocenterX)) R_isocenter.x = R_SelectedIsocenterX;
                if (!double.IsNaN(R_SelectedIsocenterY)) R_isocenter.y = R_SelectedIsocenterY;
                if (!double.IsNaN(R_SelectedIsocenterZ)) R_isocenter.z = R_SelectedIsocenterZ;
            }

            if (hasLSide && double.IsNaN(L_SelectedMedGantryAngle)) MessageBox.Show("Enter field angle for left side");
            if (hasRSide && double.IsNaN(R_SelectedMedGantryAngle)) MessageBox.Show("Enter field angle for right side");


            //else
            //{
            /////// until angle optimizer works correctly, require manual values
            //    // get optimal gantry angle
            //    if (SelectedBreastSide == "L")
            //        optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, ptvEvalBelowIsocenter, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
            //    if (SelectedBreastSide == "R")
            //        optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, ptvEvalBelowIsocenter, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
            //    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, isocenter, ss, ptvEvalBelowIsocenter, LungIpsi, optimalGantryAngle, medstartCA, medendCA, stepCA, SelectedBreastSide, true); // get optimal angle
            //    med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
            //}
            //MessageBox.Show(string.Format("found optimal angles: G{0:0.0}, Col{1:0.0}", med0.ControlPoints.First().GantryAngle, med0.ControlPoints.First().CollimatorAngle));

            // place medial fields
            if (hasLSide)
            {
                if (!double.IsNaN(L_SelectedMedColAngle))
                {
                    L_optimalGantryAngle = L_SelectedMedGantryAngle;
                    L_med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, L_SelectedMedColAngle, L_optimalGantryAngle, 0, L_isocenter);
                }
                else
                {
                    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, L_isocenter, ss, L_ptvEval, L_LungIpsi, L_SelectedMedGantryAngle, L_medstartCA, L_medendCA, stepCA, "Left", true); // get optimal angle
                    L_optimalGantryAngle = L_SelectedMedGantryAngle;
                    L_med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, L_SelectedMedGantryAngle, 0, L_isocenter);
                }
                L_med0.FitCollimatorToStructure(BeamHelpers.LeftBreastFBmarginsMed, L_ptvEval, true, true, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_med0, 20);
                L_med20 = BeamHelpers.optimizeCollimator(L_optimalGantryAngle + 20, L_ptvEval, L_LungIpsi, ss, eps, machinePars, L_isocenter, L_medstartCA, L_medendCA, stepCA, "Left", true, 30, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_med20, 20);
                L_med40 = BeamHelpers.optimizeCollimator(L_optimalGantryAngle + 40, L_ptvEval, L_LungIpsi, ss, eps, machinePars, L_isocenter, L_medstartCA, L_medendCA, stepCA, "Left", true, 30, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_med20, 20);
                var L_lp0Angle = L_optimalGantryAngle - 180;
                L_lat0 = BeamHelpers.optimizeCollimator(L_lp0Angle + 30, L_ptvEval, L_LungIpsi, ss, eps, machinePars, L_isocenter, L_latstartCA, L_latendCA, stepCA, "Left", false, 30, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_lat0, 20);
                L_lat20 = BeamHelpers.optimizeCollimator(L_lp0Angle + 10, L_ptvEval, L_LungIpsi, ss, eps, machinePars, L_isocenter, L_latstartCA, L_latendCA, stepCA, "Left", false, 30, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_lat20, 20);
                L_lat40 = BeamHelpers.optimizeCollimator(L_lp0Angle - 10, L_ptvEval, L_LungIpsi, ss, eps, machinePars, L_isocenter, L_latstartCA, L_latendCA, stepCA, "Left", false, 30, false);
                if (L_ptvSupra != null) BeamHelpers.setY2OfStaticField(L_lat20, 20);
                var L_CrossAngle = L_optimalGantryAngle + 100;
                L_CrossAngle = checkFieldAngle(L_CrossAngle);
                L_cross = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 0, L_CrossAngle, 0, L_isocenter);
                L_cross.FitCollimatorToStructure(BeamHelpers.margins5, L_ptvBreast, true, true, false);
                L_med0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_med20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_med40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_lat0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_lat20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_lat40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_cross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                L_med0.Id = "L_med0";
                L_med20.Id = "L_med20";
                L_med40.Id = "L_med40";
                L_lat0.Id = "L_lat0";
                L_lat20.Id = "L_lat20";
                L_lat40.Id = "L_lat40";
                L_cross.Id = "L_cross";

                if (L_ptvSupra != null)
                {
                    if (isBilateral)
                        L_scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 0, 0, L_isocenter);
                    else
                        L_scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 340, 0, L_isocenter);
                    L_sclat = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 20, 0, L_isocenter);
                    L_scpa = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 170, 0, L_isocenter);
                    if (L_scap != null) L_scap.FitCollimatorToStructure(BeamHelpers.margins5, L_ptvSupra, true, true, true);
                    if (L_sclat != null) L_sclat.FitCollimatorToStructure(BeamHelpers.margins5, L_ptvSupra, true, true, true);
                    if (L_scpa != null) L_scpa.FitCollimatorToStructure(BeamHelpers.margins5, L_ptvSupra, true, true, true);
                    L_scap.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    L_scap.Id = "L_scap";
                    L_sclat.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    L_sclat.Id = "L_sclat";
                    L_scpa.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    L_scpa.Id = "L_scpa";
                }
            }
            if (hasRSide)
            {
                if (!double.IsNaN(R_SelectedMedColAngle))
                {
                    R_optimalGantryAngle = R_SelectedMedGantryAngle;
                    R_med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, R_SelectedMedColAngle, R_optimalGantryAngle, 0, R_isocenter);
                }
                else
                {
                    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, R_isocenter, ss, R_ptvEval, R_LungIpsi, R_SelectedMedGantryAngle, R_medstartCA, R_medendCA, stepCA, "Right", true); // get optimal angle
                    R_optimalGantryAngle = R_SelectedMedGantryAngle;
                    R_med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, R_SelectedMedGantryAngle, 0, R_isocenter);
                }
                R_med0.FitCollimatorToStructure(BeamHelpers.RightBreastFBmarginsMed, R_ptvEval, true, true, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_med0, 20);
                R_med20 = BeamHelpers.optimizeCollimator(R_optimalGantryAngle - 20, R_ptvEval, R_LungIpsi, ss, eps, machinePars, R_isocenter, R_medstartCA, R_medendCA, stepCA, "Right", true, 30, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_med20, 20);
                R_med40 = BeamHelpers.optimizeCollimator(R_optimalGantryAngle - 40, R_ptvEval, R_LungIpsi, ss, eps, machinePars, R_isocenter, R_medstartCA, R_medendCA, stepCA, "Right", true, 30, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_med40, 20);
                var R_lp0Angle = R_optimalGantryAngle + 180;
                R_lat0 = BeamHelpers.optimizeCollimator(R_lp0Angle - 30, R_ptvEval, R_LungIpsi, ss, eps, machinePars, R_isocenter, R_latstartCA, R_latendCA, stepCA, "Right", false, 30, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_lat0, 20);
                R_lat20 = BeamHelpers.optimizeCollimator(R_lp0Angle - 10, R_ptvEval, R_LungIpsi, ss, eps, machinePars, R_isocenter, R_latstartCA, R_latendCA, stepCA, "Right", false, 30, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_lat20, 20);
                R_lat40 = BeamHelpers.optimizeCollimator(R_lp0Angle + 10, R_ptvEval, R_LungIpsi, ss, eps, machinePars, R_isocenter, R_latstartCA, R_latendCA, stepCA, "Right", false, 30, false);
                if (R_ptvSupra != null) BeamHelpers.setY2OfStaticField(R_lat40, 20);
                var R_CrossAngle = R_optimalGantryAngle - 100;
                R_CrossAngle = checkFieldAngle(R_CrossAngle);
                R_cross = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 0, R_CrossAngle, 0, R_isocenter);
                R_cross.FitCollimatorToStructure(BeamHelpers.margins5, R_ptvBreast, true, true, false);
                R_med0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_med20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_med40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_lat0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_lat20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_lat40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_cross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                R_med0.Id = "R_med0";
                R_med20.Id = "R_med20";
                R_med40.Id = "R_med40";
                R_lat0.Id = "R_lat0";
                R_lat20.Id = "R_lat20";
                R_lat40.Id = "R_lat40";
                R_cross.Id = "R_cross";
                if (R_ptvSupra != null)
                {
                    if (isBilateral)
                        R_scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 0, 0, R_isocenter);
                    else
                        R_scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 20, 0, R_isocenter);
                    R_sclat = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 340, 0, R_isocenter);
                    R_scpa = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 190, 0, R_isocenter);
                    if (R_scap != null) R_scap.FitCollimatorToStructure(BeamHelpers.margins5, R_ptvSupra, true, true, true);
                    if (R_sclat != null) R_sclat.FitCollimatorToStructure(BeamHelpers.margins5, R_ptvSupra, true, true, true);
                    if (R_scpa != null) R_scpa.FitCollimatorToStructure(BeamHelpers.margins5, R_ptvSupra, true, true, true);
                    R_scap.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    R_scap.Id = "R_scap";
                    R_sclat.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    R_sclat.Id = "R_sclat";
                    R_scpa.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                    R_scpa.Id = "R_scpa";
                }
            }

            eps.SetCalculationModel(CalculationType.PhotonIMRTOptimization, OptimizationAlgorithmModel);
            var optSetup = eps.OptimizationSetup;
            optSetup.AddAutomaticNormalTissueObjective(40);

            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);
            double maxPrescribed = NOF * presc[0].Value;

            // lung ipsi
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungIpsi, maxPrescribed * 1.01, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungIpsi, maxPrescribed * (4.7D / 60D), 060, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungIpsi, maxPrescribed * (9.7D / 60D), 040, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungIpsi, maxPrescribed * (19.7D / 60D), 020, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungIpsi, maxPrescribed * 1.01, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungIpsi, maxPrescribed * (4.7D / 60D), 060, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungIpsi, maxPrescribed * (9.7D / 60D), 040, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungIpsi, maxPrescribed * (19.7D / 60D), 020, 70);
            if (!isBilateral)
            {
                // lung contra
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungContra, maxPrescribed * (20D / 60D), 001, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungContra, maxPrescribed * (10D / 60D), 005, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_LungContra, maxPrescribed * (5D / 60D), 010, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungContra, maxPrescribed * (20D / 60D), 001, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungContra, maxPrescribed * (10D / 60D), 005, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_LungContra, maxPrescribed * (5D / 60D), 010, 70);
                // breast contra
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, L_BreastContra, maxPrescribed * (10D / 60D), 01, 70);
                BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, R_BreastContra, maxPrescribed * (10D / 60D), 01, 70);
            }

            // heart
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, Heart, maxPrescribed * 4.5 / 60D, 30);
            // lad
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LAD, maxPrescribed * (19.7D / 60D), 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LADprv, maxPrescribed * (19.7D / 50D), 000, 70);
            // esophagus
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, Esophagus, maxPrescribed, 0, 70);
            // spinal cord
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, SpinalCord, maxPrescribed * (35D / 60D), 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, SpinalCordPrv, maxPrescribed * (40D / 60D), 000, 70);

            ss.RemoveStructure(BodyShrinked);
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);

            MessageBox.Show("===DONE===");
        }

        private double checkFieldAngle(double CrossAngle)
        {
            CrossAngle = CrossAngle >= 360 ? CrossAngle - 360 : CrossAngle;
            CrossAngle = CrossAngle < 0 ? CrossAngle + 360 : CrossAngle;
            return CrossAngle;
        }
    }
}