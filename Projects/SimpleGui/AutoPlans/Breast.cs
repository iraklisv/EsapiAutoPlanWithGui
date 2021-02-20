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
        private Structure LungIpsi;
        private Structure LungContra;
        private Structure Heart;
        private Structure BreastContra;
        private Structure LAD;
        private Structure LADprv;
        private Structure SpinalCord;
        private Structure SpinalCordPrv;
        private Structure ptvSupra;
        private Structure PTVe;
        private Structure PTVe3mm;
        //private OptimizationOptionsIMRT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));
        public void runBreastFif(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
                double MfGantryAngle, double MfColAngle, double CropFromBody,
                int nof, List<KeyValuePair<string, double>> prescriptions,
                string SelectedBreastSide, string SelectedLungIpsi, string SelectedLungContra, string SelectedHeart, string SelectedBreastContra, string SelectedLAD, string SelectedSupraPTV)
        {
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
            bool tryFifBuild = true;
            pat.BeginModifications();

            // HERE REMOVE OLD OPTIMIZATION STRUCTURES!
            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            #region Prepare general Structures
            Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            BodyShrinked.SegmentVolume = body.Margin(-CropFromBody);
            LungIpsi = StructureHelpers.getStructureFromStructureSet(SelectedLungIpsi, ss, true);
            LungContra = StructureHelpers.getStructureFromStructureSet(SelectedLungContra, ss, true);
            Structure target = StructureHelpers.getStructureFromStructureSet(presc.FirstOrDefault().Key, ss, true);
            Structure targetCr = StructureHelpers.getStructureFromStructureSet("ctvcr", ss, false);
            Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss, true);
            BreastContra = StructureHelpers.getStructureFromStructureSet(SelectedBreastContra, ss, true);
            LAD = StructureHelpers.getStructureFromStructureSet(SelectedLAD, ss, true);
            ptvSupra = StructureHelpers.getStructureFromStructureSet(SelectedSupraPTV, ss, true);

            if (LungIpsi == null) { MessageBox.Show("Need ipsilateral lung"); return; }
            
            targetCr = StructureHelpers.createStructureIfNotExisting("CTVcr", ss, "PTV");
            targetCr.SegmentVolume = target.And(BodyShrinked);

            PTVe = StructureHelpers.createStructureIfNotExisting("0_ptve", ss, "PTV");
            PTVe3mm = StructureHelpers.createStructureIfNotExisting("0_ptve3mm", ss, "CONTROL");
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            // make all PTVs add to PTVe
            foreach (var p in PTVs) if (PTVe.IsEmpty) PTVe.SegmentVolume = p.Margin(0);
                else PTVe.SegmentVolume = PTVe.Or(p);
            PTVe.SegmentVolume = PTVe.And(BodyShrinked);
            PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked);
            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

            PTVe3mm.SegmentVolume = PTVe.Margin(3);
            PTVe3mm.SegmentVolume = PTVe3mm.And(body);
            #endregion

            Course Course = eps.Course;
            //var activePlan = Course.ExternalPlanSetups.);

            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);

            #region isocenter positioning
            VVector isocenter = new VVector();
            // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (ptvSupra == null)
                isocenter = PTVe.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
            else
            {
                var posZmax = PTVe.MeshGeometry.Bounds.Z + 200 - 20; // leave margin for skinflash
                var posZtop = PTVe.MeshGeometry.Bounds.Z + PTVe.MeshGeometry.Bounds.SizeZ;
                var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                double posZ;
                posZ = lungTop > posZtop ? lungTop : posZtop;
                posZ = posZ > posZmax ? posZmax : posZ;
                isocenter = new VVector(PTVe.MeshGeometry.Bounds.X + PTVe.MeshGeometry.Bounds.SizeX / 2,
                    PTVe.MeshGeometry.Bounds.Y + PTVe.MeshGeometry.Bounds.SizeY / 2,
                    posZ);
            }
            #endregion

            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 600, "STATIC", ""); // for fif manually change energy to 6x/dr600, static!

            Beam mf0 = null;

            #region field placement
            if (!double.IsNaN(MfGantryAngle) && !double.IsNaN(MfColAngle))
            {
                if (ptvSupra == null)
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), MfColAngle, MfGantryAngle, 0, isocenter); // if no supra, use col angle
                else
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, MfGantryAngle, 0, isocenter); // if supra, col angle = 0
            }
            #region medial field collimator and gantry angle optimization
            else
            {
                // try to find optimale angle
                if (ptvSupra == null)
                {
                    // get optimal gantry angle
                    var optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(ss, target, LungIpsi, 300, 330, 0.5, isocenter); // get optimal angle
                    // get optimal collimator rotation for optimal angle, use beam's eye view for dat
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, optimalGantryAngle, 0, isocenter);
                    var targetBEVcontour = mf0.GetStructureOutlines(target, true);
                    var lungBEVcontour = mf0.GetStructureOutlines(LungIpsi, true);
                    eps.RemoveBeam(mf0);
                    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(ss, targetBEVcontour, lungBEVcontour, optimalGantryAngle, 0, 40, 1, 0, 20); // get optimal angle
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
                }
                else
                {
                    var optimalAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(ss, target, LungIpsi, 300, 330, 0.5, isocenter);
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, optimalAngle, 0, isocenter); // if supra, col angle = 0, that's it
                }

            }
            #endregion // field angle and col optimization
            #endregion // end of field placement overall

            #region fit medial field and generate opposing field
            mf0.FitMLCToStructure(BeamHelpers.LeftBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            if (ptvSupra != null) // if supraclav, set Y2 to 0
            {
                var fieldPars = mf0.GetEditableParameters();

                var pars = mf0.GetEditableParameters();
                var setJawsTo = new VRect<double>(fieldPars.ControlPoints.FirstOrDefault().JawPositions.X1,
                    fieldPars.ControlPoints.FirstOrDefault().JawPositions.Y1,
                    fieldPars.ControlPoints.FirstOrDefault().JawPositions.X2,
                    0);
                pars.SetJawPositions(setJawsTo);
            }
            Beam lf0 = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, mf0);
            lf0.FitMLCToStructure(BeamHelpers.LeftBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            BeamHelpers.openMLCoutOfBody(mf0, false);
            BeamHelpers.openMLCoutOfBody(lf0, true);
            MessageBox.Show("created fields with MLC");
            #endregion

            #region calculate dose profile
            double mf0Angle = mf0.ControlPoints[0].GantryAngle - 180 - 90;
            double lf0Angle = lf0.ControlPoints[0].GantryAngle - 90;
            double profileAngle = (mf0Angle + lf0Angle) / 2 * Math.PI / 180; // and convert it to Radians
            eps.CalculateDose();
            MessageBox.Show("calculated dose, trying to measure profile");
            if (eps.IsDoseValid)
            {
                BeamHelpers.NormalizePlanToStructureCoverageRelAbs(eps, targetCr);
                //coverage = plan.GetDoseAtVolume(targetCr, 96, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                // get the dose profile in tangential direction is isocenter axial plane
                VVector profileStart = new VVector();
                VVector profileEnd = new VVector();
                double profileEndPointDistanceToIso = 150;
                profileEnd.x = isocenter.x + profileEndPointDistanceToIso * Math.Sin(profileAngle);
                profileEnd.y = isocenter.y + profileEndPointDistanceToIso * Math.Cos(profileAngle);
                profileEnd.z = isocenter.z;
                profileStart.x = isocenter.x - profileEndPointDistanceToIso * Math.Sin(profileAngle);
                profileStart.y = isocenter.y - profileEndPointDistanceToIso * Math.Cos(profileAngle);
                profileStart.z = isocenter.z;
                int binSize = Convert.ToInt32(2 * profileEndPointDistanceToIso); // make 1mm bin size
                double[] size = new double[binSize];

                List<double> maxProfileDoses = BeamHelpers.measureProfileLocalMaximums(size, eps, profileStart, profileEnd);
                MessageBox.Show("measured profiles");

                if (maxProfileDoses.Count() == 2)
                {
                    double localMaximaRatios = maxProfileDoses[1] / maxProfileDoses[0];
                    BeamParameters pars1 = mf0.GetEditableParameters();
                    BeamParameters pars2 = lf0.GetEditableParameters();
                    pars1.WeightFactor = (localMaximaRatios - 1) / 2 + 1;
                    pars2.WeightFactor = -(localMaximaRatios - 1) / 2 + 1;
                    mf0.ApplyParameters(pars1);
                    lf0.ApplyParameters(pars2);
                }
            }
            MessageBox.Show("corrected field weights");
            #endregion

            if (tryFifBuild)
            {
                #region format fields
                mf0.Id = string.Format("mf.0");
                lf0.Id = string.Format("lf.0");
                mf0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                lf0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                MessageBox.Show("created DRRs and named fields");
                #endregion

                #region convert isodoses to structures and operate on them
                Structure s_idv5p = StructureHelpers.createStructureIfNotExisting("0_id5p", ss, "Control");
                VVector lungVector = LungIpsi.CenterPoint;
                lungVector.z = isocenter.z;
                double fifBinWidthPercent = 3.5;
                if (eps.IsDoseValid)
                {
                    Dose dose = eps.Dose;
                    double maxDose = eps.Dose.DoseMax3D.Dose;
                    double fifRemovesAbove = 108.5;
                    double toBeRemovedPercent = maxDose - fifRemovesAbove;
                    int nBin = Convert.ToInt32(toBeRemovedPercent / fifBinWidthPercent) + 1;
                    List<double> fifIDLs = new List<double> { };
                    for (int bin = 0; bin < nBin; bin++) fifIDLs.Add(fifRemovesAbove + bin * fifBinWidthPercent);
                    fifIDLs.Sort();
                    fifIDLs.Reverse();
                    //List<double> fifIDLs = new List<double> { 115, 113, 111};
                    //List<double> fifIDLs = new List<double> {109};
                    BeamHelpers.findMLCEdgeXAndInitiateMap();
                    foreach (double fifIDL in fifIDLs)
                    {
                        List<Structure> hotspots = BeamHelpers.createBreastFifHotSpotContour(ss, eps, fifIDL);


                        foreach (Structure hotspot in hotspots)
                        {
                            Console.WriteLine($"hotspot id is {hotspot.Id}");
                            VVector hsCenter = hotspot.CenterPoint;
                            hsCenter.z = isocenter.z;
                            VVector deltaHS = hsCenter - lungVector;
                            VVector deltaIso = isocenter - lungVector;

                            double tanHS = deltaHS.y / deltaHS.x;
                            double tanIso = deltaIso.y / deltaIso.x;
                            if (tanHS <= tanIso)
                            {
                                //MessageBox.Show(string.Format("hs len {0}\n isolen {1}",deltaHS.Length.ToString(),deltaIso.Length.ToString()),"info",MessageBoxButton.OK,MessageBoxImage.Information);
                                if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, mf0, false);
                                else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, mf0, true);
                            }
                            else
                            {
                                if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lf0, true);
                                else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lf0, false);
                            }
                        }
                    }
                }
                PlanSetup np = Course.CopyPlanSetup(eps);
                np.Id = $"{eps.Id}c";
                MessageBox.Show("created isodose structures");
                #endregion

                #region create copy of plan and apply fif weights, recalculate
                ExternalPlanSetup newPlan = Course.ExternalPlanSetups.First(x => x.Id.Equals(np.Id));
                List<Beam> beams = newPlan.Beams.ToList();
                Beam mf = beams.FirstOrDefault(x => x.Id.Contains("mf") && x.Id.EndsWith(".0"));
                Beam lf = beams.FirstOrDefault(x => x.Id.Contains("lf") && x.Id.EndsWith(".0"));
                var medialBase = BeamHelpers.getBaseName(mf.Id, 1);
                var lateralBase = BeamHelpers.getBaseName(lf.Id, 1);
                foreach (Beam b in beams)
                {
                    if (b.Id.Contains(medialBase) && !b.Id.Equals(mf.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, mf);
                    if (b.Id.Contains(lateralBase) && !b.Id.Equals(lf.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, lf);
                }
                MessageBox.Show("copied plan and named fifs");
                #endregion


            }
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            StructureHelpers.ClearAllOptimizationContours(ss);
            //ss.RemoveStructure(BodyShrinked);
            //ss.RemoveStructure(PTVe3mm);

            MessageBox.Show("All done");
        }

        public void PrepareIMRT(Patient p1, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
                double MfGantryAngle, double MfColAngle, double CropFromBody,
                int nof, List<KeyValuePair<string, double>> prescriptions,
                string SelectedBreastSide, string SelectedLungIpsi, string SelectedLungContra, string SelectedHeart, string SelectedBreastContra, string SelectedLAD, string SelectedSpinalCord, string SelectedSupraPTV)
        {
            Messenger.Default.Send("Script Running Started");
            p = p1;
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

            p.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            #region Prepare general Structures

            Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            BodyShrinked.SegmentVolume = body.Margin(-CropFromBody);
            LungIpsi = StructureHelpers.getStructureFromStructureSet(SelectedLungIpsi, ss, true);
            LungContra = StructureHelpers.getStructureFromStructureSet(SelectedLungContra, ss, true);
            Structure target = StructureHelpers.getStructureFromStructureSet(presc.FirstOrDefault().Key, ss, true);
            Structure targetCr = StructureHelpers.getStructureFromStructureSet("ctvcr", ss, false);
            Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss, true);
            BreastContra = StructureHelpers.getStructureFromStructureSet(SelectedBreastContra, ss, true);
            LAD = StructureHelpers.getStructureFromStructureSet(SelectedLAD, ss, true);
            LADprv = StructureHelpers.createStructureIfNotExisting("0_LADprv", ss, "ORGAN");
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
            ptvSupra = StructureHelpers.getStructureFromStructureSet(SelectedSupraPTV, ss, true);
            Structure HD = StructureHelpers.createStructureIfNotExisting("HD", ss, "CONTROL");

            PTVe = StructureHelpers.createStructureIfNotExisting("0_ptve", ss, "PTV");
            PTVe3mm = StructureHelpers.createStructureIfNotExisting("0_ptve3mm", ss, "CONTROL");

            Structure PTVeBelowIsocenter = StructureHelpers.createStructureIfNotExisting("0_ptvSplit", ss, "PTV");

            // segment helper structures
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            // make all PTVs add to PTVe
            foreach (var p in PTVs) if (PTVe.IsEmpty) PTVe.SegmentVolume = p.Margin(0);
                else PTVe.SegmentVolume = PTVe.Or(p);
            PTVe.SegmentVolume = PTVe.And(BodyShrinked);

            PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked);
            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

            PTVinters = StructureHelpers.GenerateIntermediatePTVs(PTVse, PTVe, presc, ss, BodyShrinked);
            PTVinters = StructureHelpers.CleanIntermediatePTVs(PTVse, PTVinters, presc);

            PTVe3mm.SegmentVolume = PTVe.Margin(3);
            PTVe3mm.SegmentVolume = PTVe3mm.And(body);
            #endregion

            List<Structure> listOfOars = new List<Structure>();
            listOfOars.Add(LungContra);
            listOfOars.Add(LungIpsi);
            listOfOars.Add(Heart);
            listOfOars.Add(SpinalCord);
            listOfOars.Add(BreastContra);

            foreach (var p in PTVs)
                if (StructureHelpers.checkIfStructureIsNotOk(p)) return;
            //foreach (var p in listOfOars)
            //if (StructureHelpers.checkIfStructureIsNotOk(p)) return;

            Rings = StructureHelpers.CreateRings(PTVse, listOfOars, ss, body, PTVe3mm);

            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc[0].Value, DoseValue.DoseUnit.Gy), 1.0);

            #region isocenter positioning
            VVector isocenter = new VVector();
            // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (ptvSupra == null)
                isocenter = PTVe.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
            else
            {
                var posZmax = PTVe.MeshGeometry.Bounds.Z + 200 - 30; // leave margin for skinflash
                var posZtop = PTVe.MeshGeometry.Bounds.Z + PTVe.MeshGeometry.Bounds.SizeZ;
                var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                double posZ;
                posZ = lungTop > posZtop ? lungTop : posZtop;
                posZ = posZ > posZmax ? posZmax : posZ;
                isocenter = new VVector(PTVe.MeshGeometry.Bounds.X + PTVe.MeshGeometry.Bounds.SizeX / 2,
                    PTVe.MeshGeometry.Bounds.Y + PTVe.MeshGeometry.Bounds.SizeY / 2,
                    posZ);
            }
            #endregion
            StructureHelpers.CopyStructureInBounds(PTVeBelowIsocenter, PTVe, ss.Image, (PTVe.MeshGeometry.Bounds.Z, isocenter.z - 20)); // it is good idea to deliniate axilla seperately... right now ROs don't do that.. might be a problem?
            if (ptvSupra == null)
                PTVeBelowIsocenter = PTVe;

            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 1400, "STATIC", "FFF"); // for fif manually change energy to 6x/dr600, static!

            #region field placement
            // one iso found, find optimal angle of medial fiel!
            Beam mf0 = null;
            double optimalGantryAngle = double.NaN;

            // if medial field gantry and collimator angles defined by user, use them directly
            if (!double.IsNaN(MfGantryAngle) && !double.IsNaN(MfColAngle))
            {
                mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), MfColAngle, MfGantryAngle, 0, isocenter); // if supra, col angle = 0
                optimalGantryAngle = MfGantryAngle;
            }
            #region medial field collimator and gantry angle optimization
            else
            {
                // get optimal gantry angle
                optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(ss, PTVeBelowIsocenter, LungIpsi, 300, 330, 0.5, isocenter); // get optimal angle
                mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, optimalGantryAngle, 0, isocenter);
                var targetBEVcontour = mf0.GetStructureOutlines(PTVeBelowIsocenter, true);
                var lungBEVcontour = mf0.GetStructureOutlines(LungIpsi, true);
                eps.RemoveBeam(mf0);
                var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(ss, targetBEVcontour, lungBEVcontour, optimalGantryAngle, 0, 40, 1, 0, 20); // get optimal angle
                mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
            }
            #endregion // field angle and col optimization
            // place medial fields
            mf0.FitCollimatorToStructure(BeamHelpers.LeftBreastFBmarginsMed, PTVeBelowIsocenter, true, true, false);
            if (ptvSupra!=null) BeamHelpers.setY2OfStaticField(mf0, 20);
            Beam tmp = null;
            tmp = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, optimalGantryAngle + 20, 0, isocenter);
            Beam mf20 = BeamHelpers.optimizeCollimator(tmp, 0, PTVeBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, 0, 40, 1, SelectedBreastSide, true, 30);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(mf20, 20);
            eps.RemoveBeam(tmp);
            tmp = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, optimalGantryAngle + 40, 0, isocenter);
            Beam mf40 = BeamHelpers.optimizeCollimator(tmp, 0, PTVeBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, 0, 40, 1, SelectedBreastSide, true, 30);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(mf40, 20);
            eps.RemoveBeam(tmp);

            // place lateral fields
            tmp = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, mf0);
            Beam lf0 = BeamHelpers.optimizeCollimator(tmp, 10, PTVeBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, 320, 360, 1, SelectedBreastSide, false, 30);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lf0, 20);
            eps.RemoveBeam(tmp);
            var lf0gantryangle = lf0.ControlPoints.First().GantryAngle;

            tmp = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, lf0gantryangle - 20, 0, isocenter);
            Beam lf20 = BeamHelpers.optimizeCollimator(tmp, 0, PTVeBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, 320, 360, 1, SelectedBreastSide, false, 30);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lf20, 20);
            eps.RemoveBeam(tmp);

            tmp = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0, lf0gantryangle - 40, 0, isocenter);
            Beam lf40 = BeamHelpers.optimizeCollimator(tmp, 0, PTVeBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, 320, 360, 1, SelectedBreastSide, false, 30);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lf40, 20);
            eps.RemoveBeam(tmp);

            var medialCrossAngle = optimalGantryAngle + 100;
            medialCrossAngle = medialCrossAngle >= 360 ? medialCrossAngle - 360 : medialCrossAngle;
            Beam mfcross = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 355, medialCrossAngle, 0, isocenter);
            mfcross.FitCollimatorToStructure(BeamHelpers.margins5, PTVe, true, true, false);
            BeamHelpers.setX2OfStaticField(mfcross, 30);
            var lateralCrossAngle = lf0gantryangle - 70;
            lateralCrossAngle = lateralCrossAngle < 0 ? medialCrossAngle + 360 : medialCrossAngle;
            Beam lfcross = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 5, lf0gantryangle - 70, 0, isocenter);
            lfcross.FitCollimatorToStructure(BeamHelpers.margins5, PTVe, true, true, false);
            BeamHelpers.setX1OfStaticField(lfcross, 30);

            mf0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            mf20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            mf40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lf0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lf20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lf40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            mfcross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lfcross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            mf0.Id = "mf0";
            mf20.Id = "mf20";
            mf40.Id = "mf40";
            lf0.Id = "lf0";
            lf20.Id = "lf20";
            lf40.Id = "lf40";
            mfcross.Id = "mf cross";
            lfcross.Id = "lfcross";

            Beam scap = null;
            Beam scpa = null;
            Beam sclat = null;
            if (ptvSupra != null)
            {
                if (SelectedBreastSide == "Left")
                {
                    scap = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 340, 0, isocenter);
                    sclat = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 20, 0, isocenter);
                    scpa = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 170, 0, isocenter);
                }
                if (SelectedBreastSide == "Right")
                {
                    scap = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 20, 0, isocenter);
                    sclat = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 340, 0, isocenter);
                    scpa = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 90, 190, 0, isocenter);
                }
                if (scap != null) scap.FitCollimatorToStructure(BeamHelpers.margins5, ptvSupra, true, true, true);
                if (sclat != null) sclat.FitCollimatorToStructure(BeamHelpers.margins5, ptvSupra, true, true, true);
                if (scpa != null) scpa.FitCollimatorToStructure(BeamHelpers.margins5, ptvSupra, true, true, true);
            }
            if (scap != null)
            {
                scap.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                scap.Id = "scap";
            }
            if (sclat != null)
            {
                sclat.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                sclat.Id = "sclat";
            }
            if (scpa != null)
            {
                scpa.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                scpa.Id = "scpa";
            }

            #endregion

            #region prepare for optimization
            eps.SetCalculationModel(CalculationType.PhotonIMRTOptimization, OptimizationAlgorithmModel);
            var optSetup = eps.OptimizationSetup;
            optSetup.AddAutomaticNormalTissueObjective(40);
            
            BeamHelpers.SetTargetOptimization(optSetup, PTVse, presc, NOF);
            BeamHelpers.SetTransitionRegiontOptimization(optSetup, PTVinters, presc, NOF);
            BeamHelpers.SetRingsOptimization(optSetup, Rings, presc, NOF);
            double maxPrescribed = NOF * presc[0].Value;

            // lung
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * 1.01, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (4.7D / 60D), 060, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (9.7D / 60D), 040, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (19.7D / 60D), 020, 70);
            // heart
            BeamHelpers.SetOptimizationMeanObjectiveInGy(optSetup, Heart, maxPrescribed * 4.5 / 60D, 30);
            // lad
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LAD, maxPrescribed * (19.7D / 60D), 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LADprv, maxPrescribed * (19.7D / 50D), 000, 70);

            // spinal cord
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, SpinalCord, maxPrescribed * (35D / 60D), 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, SpinalCordPrv, maxPrescribed * (40D / 60D), 000, 70);

            #endregion
            ss.RemoveStructure(BodyShrinked);
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            //StructureHelpers.ClearAllOptimizationContours(ss);

            MessageBox.Show("===DONE===");
        }
    }
}