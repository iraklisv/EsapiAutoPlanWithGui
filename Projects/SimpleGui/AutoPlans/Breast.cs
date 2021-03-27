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
        private Structure Esophagus;
        private Structure SpinalCord;
        private Structure SpinalCordPrv;
        private Structure ptvSupra;
        private Structure ptvBreast;
        private Structure ptvBoost;
        private Structure ptvEval;
        private Structure ptvEval3mm;
        //private OptimizationOptionsIMRT optimizationOptions;
        private Patient p;
        private ExternalPlanSetup eps;
        private StructureSet ss;
        private List<KeyValuePair<string, double>> presc;
        private int NOF;
        private string mlcId;
        private VVector isocenter;
        private static void AddToHistory() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("AddMessage"));
        public void runBreastFif(Patient pat, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
                double medGantryAngle, double medColAngle, double CropFromBody,
                double IsocenterX, double IsocenterY, double IsocenterZ,
                int nof, List<KeyValuePair<string, double>> prescriptions,
                string SelectedBreastSide, string SelectedLungIpsi, string SelectedLungContra, string SelectedHeart, string SelectedBreastContra, string SelectedLAD,
                string SelectedSupraPTV, string SelectedBreastPTV)
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

            bool doCrop = true;
            if (double.IsNaN(CropFromBody)) doCrop = false;

            pat.BeginModifications();

            // HERE REMOVE OLD OPTIMIZATION STRUCTURES!
            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            #region Prepare general Structures
            Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            if (doCrop) BodyShrinked.SegmentVolume = body.Margin(-CropFromBody);
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
            if (doCrop)
                targetCr.SegmentVolume = target.And(BodyShrinked);

            ptvEval = StructureHelpers.createStructureIfNotExisting("0_ptvEval", ss, "PTV");
            ptvEval3mm = StructureHelpers.createStructureIfNotExisting("0_ptvEval3mm", ss, "CONTROL");
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            // make all PTVs add to ptvEval
            foreach (var p in PTVs) if (ptvEval.IsEmpty) ptvEval.SegmentVolume = p.Margin(0);
                else ptvEval.SegmentVolume = ptvEval.Or(p);


            if (doCrop)
                ptvEval.SegmentVolume = ptvEval.And(BodyShrinked);
            PTVse = StructureHelpers.CreatePTVsEval(PTVs, ss, BodyShrinked, doCrop);


            if (PTVse == null) { MessageBox.Show("something is wrong with PTV eval creation"); return; }

            ptvEval3mm.SegmentVolume = ptvEval.Margin(3);
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(body);
            #endregion

            Course Course = eps.Course;
            //var activePlan = Course.ExternalPlanSetups.);

            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);

            #region isocenter positioning
            isocenter = new VVector();
            // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (ptvSupra == null)
                isocenter = ptvEval.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
            else
            {
                var posZmax = ptvEval.MeshGeometry.Bounds.Z + 200 - 20; // leave margin for skinflash
                var posZtop = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ;
                var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                double posZ;
                posZ = lungTop > posZtop ? lungTop : posZtop;
                posZ = posZ > posZmax ? posZmax : posZ;
                isocenter = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                    ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                    posZ);
            }
            if (!double.IsNaN(IsocenterX)) isocenter.x = IsocenterX;
            if (!double.IsNaN(IsocenterY)) isocenter.y = IsocenterY;
            if (!double.IsNaN(IsocenterZ)) isocenter.z = IsocenterZ;
            #endregion

            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 600, "STATIC", ""); // for fif manually change energy to 6x/dr600, static!

            Beam med0 = null;
            double medstartCA = 0;
            double medendCA = 0;
            //double latstartCA = 0;
            //double latendCA = 0; //for imrt
            double stepCA = 1;
            if (SelectedBreastSide == "Right")
            {
                medstartCA = 320;
                medendCA = 360;
                //latstartCA = 20;
                //latendCA = 40;
            }
            if (SelectedBreastSide == "Left")
            {
                medstartCA = 20;
                medendCA = 40;
                //latstartCA = 320;
                //latendCA = 360;

            }

            #region field placement
            if (!double.IsNaN(medGantryAngle) && !double.IsNaN(medColAngle))
            {
                if (ptvSupra == null)
                    med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, medColAngle, medGantryAngle, 0, isocenter); // if no supra, use col angle
                else
                    med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, 0, medGantryAngle, 0, isocenter); // if supra, col angle = 0
            }
            #region medial field collimator and gantry angle optimization
            else
            {
                // try to find optimale angle
                if (ptvSupra == null)
                {
                    // get optimal gantry angle
                    var optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, target, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
                    // get optimal collimator rotation for optimal angle, use beam's eye view for dat
                    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, isocenter, ss, target, LungIpsi, optimalGantryAngle, medstartCA, medendCA, stepCA, SelectedBreastSide, true); // get optimal angle
                    med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
                }
                else
                {
                    var optimalAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, target, LungIpsi, isocenter, SelectedBreastSide);
                    med0 = eps.AddMLCBeam(machinePars, null, BeamHelpers.defaultJawPositions, 0, optimalAngle, 0, isocenter); // if supra, col angle = 0, that's it
                }

            }
            #endregion // field angle and col optimization
            #endregion // end of field placement overall

            #region fit medial field and generate opposing field
            if (SelectedBreastSide == "Left") med0.FitMLCToStructure(BeamHelpers.LeftBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            if (SelectedBreastSide == "Right") med0.FitMLCToStructure(BeamHelpers.RighBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            if (ptvSupra != null) // if supraclav, set Y2 to 0
            {
                var fieldPars = med0.GetEditableParameters();

                var pars = med0.GetEditableParameters();
                var setJawsTo = new VRect<double>(fieldPars.ControlPoints.FirstOrDefault().JawPositions.X1,
                    fieldPars.ControlPoints.FirstOrDefault().JawPositions.Y1,
                    fieldPars.ControlPoints.FirstOrDefault().JawPositions.X2,
                    0);
                pars.SetJawPositions(setJawsTo);
            }
            Beam lat0 = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, med0, SelectedBreastSide);
            lat0.FitMLCToStructure(BeamHelpers.LeftBreastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            BeamHelpers.openMLCoutOfBody(med0, false);
            BeamHelpers.openMLCoutOfBody(lat0, true);
            MessageBox.Show("created fields with MLC");
            #endregion

            #region calculate dose profile
            double med0Angle = med0.ControlPoints[0].GantryAngle - 180 - 90;
            double lat0Angle = lat0.ControlPoints[0].GantryAngle - 90;
            double profileAngle = (med0Angle + lat0Angle) / 2 * Math.PI / 180; // and convert it to Radians
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
                    BeamParameters pars1 = med0.GetEditableParameters();
                    BeamParameters pars2 = lat0.GetEditableParameters();
                    pars1.WeightFactor = (localMaximaRatios - 1) / 2 + 1;
                    pars2.WeightFactor = -(localMaximaRatios - 1) / 2 + 1;
                    med0.ApplyParameters(pars1);
                    lat0.ApplyParameters(pars2);
                }
            }
            MessageBox.Show("corrected field weights");
            #endregion

            if (tryFifBuild)
            {
                #region format fields
                med0.Id = string.Format("med.0");
                lat0.Id = string.Format("lat.0");
                med0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
                lat0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
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
                                if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, med0, false);
                                else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, med0, true);
                            }
                            else
                            {
                                if (!hotspot.Id.Contains("nl")) BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lat0, true);
                                else BeamHelpers.generateFifForHotSpot(machinePars, ss, eps, hotspot, lat0, false);
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
                Beam med = beams.FirstOrDefault(x => x.Id.Contains("med") && x.Id.EndsWith(".0"));
                Beam lat = beams.FirstOrDefault(x => x.Id.Contains("lat") && x.Id.EndsWith(".0"));
                var medialBase = BeamHelpers.getBaseName(med.Id, 1);
                var lateralBase = BeamHelpers.getBaseName(lat.Id, 1);
                foreach (Beam b in beams)
                {
                    if (b.Id.Contains(medialBase) && !b.Id.Equals(med.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, med);
                    if (b.Id.Contains(lateralBase) && !b.Id.Equals(lat.Id)) BeamHelpers.substractFif(fifBinWidthPercent * 0.025, b, lat);
                }
                MessageBox.Show("copied plan and named fifs");
                #endregion
            }

            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            StructureHelpers.ClearAllOptimizationContours(ss);
            //ss.RemoveStructure(BodyShrinked);
            //ss.RemoveStructure(ptvEval3mm);

            MessageBox.Show("All done");
        }
        public void PrepareIMRT(Patient p1, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId,
                double medGantryAngle, double medColAngle, double CropFromBody,
                double IsocenterX, double IsocenterY, double IsocenterZ,
                int nof, List<KeyValuePair<string, double>> prescriptions,
                string SelectedBreastSide, string SelectedLungIpsi, string SelectedLungContra, string SelectedHeart, string SelectedBreastContra, string SelectedLAD, string SelectedEsophagus,
                string SelectedSpinalCord, string SelectedSupraPTV, string SelectedBreastPTV, string SelectedBoostPTV)
        {
            Messenger.Default.Send("Script Running Started");
            p = p1;
            eps = eps1;
            ss = ss1;
            presc = prescriptions;
            NOF = nof;
            mlcId = MlcId;

            if (SelectedBreastSide == "")
            {
                MessageBox.Show("Select breast side");
                return;
            }
            if (presc.Count == 0)
            {
                MessageBox.Show("Please add target");
                return;
            }
            if (SelectedLungIpsi == "")
            {
                MessageBox.Show("Please seleng lung ipsilateral");
                return;
            }
            bool doCrop = true;
            if (double.IsNaN(CropFromBody)) doCrop = false;


            p.BeginModifications();
            StructureHelpers.ClearAllOptimizationContours(ss);
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

            #region Prepare general Structures

            Structure body = StructureHelpers.getStructureFromStructureSet("BODY", ss, true);
            Structure BodyShrinked = StructureHelpers.createStructureIfNotExisting("0_BodyShrinked", ss, "ORGAN");
            if (doCrop) BodyShrinked.SegmentVolume = body.Margin(-CropFromBody);
            LungIpsi = StructureHelpers.getStructureFromStructureSet(SelectedLungIpsi, ss, true);
            LungContra = StructureHelpers.getStructureFromStructureSet(SelectedLungContra, ss, true);
            Structure target = StructureHelpers.getStructureFromStructureSet(presc.FirstOrDefault().Key, ss, true);
            Structure targetCr = StructureHelpers.getStructureFromStructureSet("ctvcr", ss, false);
            Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss, true);
            BreastContra = StructureHelpers.getStructureFromStructureSet(SelectedBreastContra, ss, true);
            LAD = StructureHelpers.getStructureFromStructureSet(SelectedLAD, ss, true);
            Esophagus= StructureHelpers.getStructureFromStructureSet(SelectedEsophagus, ss, true);
            
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
            ptvBreast = StructureHelpers.getStructureFromStructureSet(SelectedBreastPTV, ss, true);

            //Structure HD = StructureHelpers.createStructureIfNotExisting("HD", ss, "CONTROL");

            ptvEval = StructureHelpers.createStructureIfNotExisting("0_ptvEval", ss, "PTV");
            ptvEval3mm = StructureHelpers.createStructureIfNotExisting("0_ptvEval3mm", ss, "CONTROL");

            Structure ptvEvalBelowIsocenter = StructureHelpers.createStructureIfNotExisting("0_ptvSplit", ss, "PTV");

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
            ptvEval3mm.SegmentVolume = ptvEval3mm.And(body);
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

            //Rings = StructureHelpers.CreateRings(PTVse, ss, body, ptvEval, 50);
            Rings = StructureHelpers.CreateRingsForBreastSIB(PTVse, listOfOars, ss, body, ptvEval, 50, SelectedBoostPTV);

            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc[0].Value, DoseValue.DoseUnit.Gy), 1.0);

            #region isocenter positioning
            isocenter = new VVector();
            // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (ptvSupra == null)
                isocenter = ptvEval.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
            else
            {
                var posZmax = ptvEval.MeshGeometry.Bounds.Z + 200 - 30; // leave margin for skinflash
                var posZtop = ptvEval.MeshGeometry.Bounds.Z + ptvEval.MeshGeometry.Bounds.SizeZ;
                var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ - 50; // 5cm below top of lung
                double posZ;
                posZ = lungTop > posZtop ? lungTop : posZtop;
                posZ = posZ > posZmax ? posZmax : posZ;
                isocenter = new VVector(ptvEval.MeshGeometry.Bounds.X + ptvEval.MeshGeometry.Bounds.SizeX / 2,
                    ptvEval.MeshGeometry.Bounds.Y + ptvEval.MeshGeometry.Bounds.SizeY / 2,
                    posZ);
            }
            if (!double.IsNaN(IsocenterX)) isocenter.x = IsocenterX;
            if (!double.IsNaN(IsocenterY)) isocenter.y = IsocenterY;
            if (!double.IsNaN(IsocenterZ)) isocenter.z = IsocenterZ;

            #endregion
            //StructureHelpers.CopyStructureInBounds(ptvEvalBelowIsocenter, ptvEval, ss.Image, (ptvEval.MeshGeometry.Bounds.Z, isocenter.z - 20)); // it is good idea to deliniate axilla seperately... right now ROs don't do that.. might be a problem?
            //if (ptvSupra == null)
            ptvEvalBelowIsocenter = ptvBreast;

            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId, "6X", 1400, "STATIC", "FFF"); // for fif manually change energy to 6x/dr600, static!

            #region field placement
            // one iso found, find optimal angle of medial fiel!
            Beam med0 = null;
            double optimalGantryAngle = double.NaN;
            double medstartCA = 0;
            double medendCA = 0;
            double latstartCA = 0;
            double latendCA = 0;
            double stepCA = 1;
            if (SelectedBreastSide == "Right")
            {
                medstartCA = 320;
                medendCA = 360;
                latstartCA = 20;
                latendCA = 40;
            }
            if (SelectedBreastSide == "Left")
            {
                medstartCA = 20;
                medendCA = 40;
                latstartCA = 320;
                latendCA = 360;

            }

            // if medial field gantry and collimator angles defined by user, use them directly
            if (!double.IsNaN(medGantryAngle))
            {
                if (!double.IsNaN(medColAngle))
                {
                    optimalGantryAngle = medGantryAngle;
                    med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, medColAngle, optimalGantryAngle, 0, isocenter);
                }
                else
                {
                    var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, isocenter, ss, ptvEvalBelowIsocenter, LungIpsi, medGantryAngle, medstartCA, medendCA, stepCA, SelectedBreastSide, true); // get optimal angle
                    optimalGantryAngle = medGantryAngle;
                    med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
                }
            }
            #region medial field collimator and gantry angle optimization
            else
            {
                // get optimal gantry angle
                if (SelectedBreastSide == "Left")
                    optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, ptvEvalBelowIsocenter, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
                if (SelectedBreastSide == "Right")
                    optimalGantryAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(machinePars, eps, ss, ptvEvalBelowIsocenter, LungIpsi, isocenter, SelectedBreastSide); // get optimal angle
                var ColAndJaw = BeamHelpers.findBreastOptimalCollAndJawIntoLung(machinePars, eps, isocenter, ss, ptvEvalBelowIsocenter, LungIpsi, optimalGantryAngle, medstartCA, medendCA, stepCA, SelectedBreastSide, true); // get optimal angle
                med0 = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, ColAndJaw.Item1, optimalGantryAngle, 0, isocenter);
            }
            MessageBox.Show(string.Format("found optimal angles: G{0:0.0}, Col{1:0.0}", med0.ControlPoints.First().GantryAngle, med0.ControlPoints.First().CollimatorAngle));
            #endregion // field angle and col optimization
            // place medial fields
            if (SelectedBreastSide == "Left") med0.FitCollimatorToStructure(BeamHelpers.LeftBreastFBmarginsMed, ptvEvalBelowIsocenter, true, true, false);
            if (SelectedBreastSide == "Right") med0.FitCollimatorToStructure(BeamHelpers.RighBreastFBmarginsMed, ptvEvalBelowIsocenter, true, true, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(med0, 20);
            //Beam tmp = null;

            double RotationDirection = 0;
            if (SelectedBreastSide == "Left") RotationDirection = 1;
            if (SelectedBreastSide == "Right") RotationDirection = -1;
            Beam med20 = BeamHelpers.optimizeCollimator(optimalGantryAngle + RotationDirection * 20, ptvEvalBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, medstartCA, medendCA, stepCA, SelectedBreastSide, true, 30, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(med20, 20);

            Beam med40 = BeamHelpers.optimizeCollimator(optimalGantryAngle + RotationDirection * 40, ptvEvalBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, medstartCA, medendCA, stepCA, SelectedBreastSide, true, 30, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(med40, 20);

            // place lateral fields
            //tmp = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, med0, SelectedBreastSide);
            //var lp0Angle = tmp.ControlPoints.First().GantryAngle;
            var lp0Angle = optimalGantryAngle - 180 * RotationDirection;

            Beam lat0 = BeamHelpers.optimizeCollimator(lp0Angle + RotationDirection * 30, ptvEvalBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, latstartCA, latendCA, stepCA, SelectedBreastSide, false, 30, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lat0, 20);
            //eps.RemoveBeam(tmp);

            Beam lat20 = BeamHelpers.optimizeCollimator(lp0Angle + RotationDirection * 10, ptvEvalBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, latstartCA, latendCA, stepCA, SelectedBreastSide, false, 30, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lat20, 20);

            Beam lat40 = BeamHelpers.optimizeCollimator(lp0Angle - RotationDirection * 10, ptvEvalBelowIsocenter, LungIpsi, ss, eps, machinePars, isocenter, latstartCA, latendCA, stepCA, SelectedBreastSide, false, 30, false);
            if (ptvSupra != null) BeamHelpers.setY2OfStaticField(lat40, 20);

            var medialCrossAngle = optimalGantryAngle + RotationDirection * 100;
            medialCrossAngle = medialCrossAngle >= 360 ? medialCrossAngle - 360 : medialCrossAngle;
            medialCrossAngle = medialCrossAngle < 0 ? medialCrossAngle + 360 : medialCrossAngle;
            Beam medcross = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 355, medialCrossAngle, 0, isocenter);
            medcross.FitCollimatorToStructure(BeamHelpers.margins5, ptvBreast, true, true, false);
            BeamHelpers.setX2OfStaticField(medcross, 30);
            var lateralCrossAngle = lp0Angle - RotationDirection * 70;
            lateralCrossAngle = lateralCrossAngle < 0 ? medialCrossAngle + 360 : medialCrossAngle;
            lateralCrossAngle = lateralCrossAngle > 360 ? medialCrossAngle - 360 : medialCrossAngle;
            Beam latcross = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 5, lp0Angle - RotationDirection * 70, 0, isocenter);
            latcross.FitCollimatorToStructure(BeamHelpers.margins5, ptvBreast, true, true, false);
            BeamHelpers.setX1OfStaticField(latcross, 30);

            med0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            med20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            med40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lat0.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lat20.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            lat40.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            medcross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            latcross.CreateOrReplaceDRR(BeamHelpers.breastDrrPars);
            med0.Id = "med0";
            med20.Id = "med20";
            med40.Id = "med40";
            lat0.Id = "lat0";
            lat20.Id = "lat20";
            lat40.Id = "lat40";
            medcross.Id = "med cross";
            latcross.Id = "latcross";

            Beam scap = null;
            Beam scpa = null;
            Beam sclat = null;
            if (ptvSupra != null)
            {
                if (SelectedBreastSide == "Left")
                {
                    scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 340, 0, isocenter);
                    sclat = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 20, 0, isocenter);
                    scpa = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 170, 0, isocenter);
                }
                if (SelectedBreastSide == "Right")
                {
                    scap = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 20, 0, isocenter);
                    sclat = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 340, 0, isocenter);
                    scpa = eps.AddStaticBeam(machinePars, BeamHelpers.defaultJawPositions, 90, 190, 0, isocenter);
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

            // lung ipsi
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * 1.01, 000, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (4.7D / 60D), 060, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (9.7D / 60D), 040, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungIpsi, maxPrescribed * (19.7D / 60D), 020, 70);
            // lung contra
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungContra, maxPrescribed * (20D / 60D), 001, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungContra, maxPrescribed * (10D / 60D), 005, 70);
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, LungContra, maxPrescribed * (5D / 60D), 010, 70);
            // breast contra
            BeamHelpers.SetOptimizationUpperObjectiveInGy(optSetup, BreastContra, maxPrescribed * (10D / 60D), 01, 70);

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

            #endregion
            ss.RemoveStructure(BodyShrinked);
            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            //StructureHelpers.ClearAllOptimizationContours(ss);

            MessageBox.Show("===DONE===");
        }
    }
}