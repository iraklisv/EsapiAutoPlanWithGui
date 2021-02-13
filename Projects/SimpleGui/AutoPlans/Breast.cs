﻿using GalaSoft.MvvmLight.Messaging;
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
        private string BreastSide;
        private Structure LungIpsi;
        private Structure LungContra;
        private Structure Heart;
        private Structure BreastContra;
        private Structure LAD;
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
            pat.BeginModifications();
            presc = presc.OrderByDescending(x => x.Value).ToList(); // order prescription by descending value of dose per fraction

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

            Course Course = eps.Course;
            //var activePlan = Course.ExternalPlanSetups.);

            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc.FirstOrDefault().Value, DoseValue.DoseUnit.Gy), 1.0);


            VVector isocenter = new VVector();
            // if no supraclav, put isocenter in the middle of breast, if not, in put Z close to shoulder
            if (ptvSupra == null)
                isocenter = PTVe.CenterPoint; // if no supraclav, put iso in the middle of rest PTV?
            else {
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

            machinePars = new ExternalBeamMachineParameters(machinePars.MachineId,"6X", 600, "STATIC", ""); // for fif manually change energy to 6x/dr600, static!

            Beam mf0 = null;

            #region field placement
            if (!double.IsNaN(MfGantryAngle) && !double.IsNaN(MfColAngle))
            {
                if (ptvSupra==null)
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), MfColAngle, MfGantryAngle, 0, isocenter); // if no supra, use col angle
                else
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 0         , MfGantryAngle, 0, isocenter); // if supra, col angle = 0
            }
            #region medial field collimator and gantry angle optimization
            else
            {
                // try to find optimale angle
                if (ptvSupra == null)
                {
                    var optimalAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(ss, target, LungIpsi, 300, 330, 0.5, isocenter); // get optimal angle
                    // get optimal collimator rotation for optimal angle, use beam's eye view for dat
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100),20         , optimalAngle, 0, isocenter); 
                }
                else
                {
                    var optimalAngle = BeamHelpers.findBreastOptimalGantryAngleForMedialField(ss, target, LungIpsi, 300, 330, 0.5, isocenter);
                    mf0 = eps.AddMLCBeam(machinePars, null, new VRect<double>(-100, -100, 100, 100), 20, optimalAngle, 0, isocenter); // if supra, col angle = 0, that's it
                }

            }
            #endregion // field angle and col optimization
            #endregion // end of field placement overall

            #region fit medial field and generate opposing field
            mf0.FitMLCToStructure(BeamHelpers.breastFBmarginsMed, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
            Beam lf0 = BeamHelpers.buildOposingToJawPlan(machinePars, eps, target, mf0);
            lf0.FitMLCToStructure(BeamHelpers.breastFBmarginsLat, target, false, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
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
                foreach (double fifIDL in fifIDLs)
                {
                    List<Structure> hotspots = BeamHelpers.createBreastFifHotSpotContour(ss, eps, fifIDL);

                    foreach (Structure hotspot in hotspots)
                    {
                        Console.WriteLine($"hotspot id is {hotspot.Id}");
                        VVector hsCenter = hotspot.CenterPoint;
                        hsCenter.z = isocenter.z;
                        VVector deltaHS = hsCenter - lungVector;
                        VVector deltaIso = isocenter- lungVector;

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

            StructureHelpers.ClearAllEmtpyOptimizationContours(ss);
            //StructureHelpers.ClearAllOptimizationContours(ss);
            ss.RemoveStructure(BodyShrinked);
            ss.RemoveStructure(PTVe3mm);

            MessageBox.Show("All done");
        }

        public void PrepareIMRT(Patient p1, ExternalPlanSetup eps1, StructureSet ss1,
                ExternalBeamMachineParameters machinePars, string OptimizationAlgorithmModel, string DoseCalculationAlgo, string MlcId, 
                double MFAngle, double MFAngleCol, double CropFromBody,
                int nof, List<KeyValuePair<string, double>>  prescriptions,
                string SelectedBreastSide, string SelectedLungIpsi, string SelectedLungContra, string SelectedHeart, string SelectedBreastContra, string SelectedLAD, string SelectedSupraPTV)
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
            Messenger.Default.Send("Creating Optimization Structures");
            PTVe = StructureHelpers.createStructureIfNotExisting("0_ptve", ss, "PTV");
            PTVe3mm = StructureHelpers.createStructureIfNotExisting("0_ptve3mm", ss, "CONTROL");
            // segment helper structures
            foreach (var p in presc)
                PTVs.Add(StructureHelpers.getStructureFromStructureSet(p.Key, ss, true));
            foreach (var p in PTVs) if (PTVe.IsEmpty) PTVe.SegmentVolume = p.Margin(0); // make PTVe by summing all ptvs up
                else PTVe.SegmentVolume = PTVe.Or(p);

            foreach (var p in PTVs)
            {
                var txt = $"0_{p.Id}e";
                PTVse.Add(StructureHelpers.createStructureIfNotExisting(txt, ss, "PTV"));
                PTVse.FirstOrDefault(x => x.Id == txt).SegmentVolume = p.Margin(0);
            }
            foreach (var p in PTVse)
            {
                foreach (var t in PTVse.TakeWhile(x => x != p)) // iterate through all previous elements
                    p.SegmentVolume = p.Sub(t);
            }

            // create transition regions
            foreach (var p in PTVse.TakeWhile(x => x != PTVse.Last()))
            {
                var txt = $"{p.Id}i";
                var nextlevel = PTVse.SkipWhile(x => x != p).Skip(1).FirstOrDefault();
                // if next level has same prescription do not create intermediate?
                var origId = Strings.cropFirstNChar(p.Id, 2); origId = Strings.cropLastNChar(origId, 1);
                var nextId = Strings.cropFirstNChar(nextlevel.Id, 2); nextId = Strings.cropLastNChar(nextId, 1);

                var thisLevelPrescription = presc.First(x => x.Key == origId);
                var nextLevelPrescription = presc.First(x => x.Key == nextId);
                if (thisLevelPrescription.Value != nextLevelPrescription.Value)
                {
                    Structure tmp = StructureHelpers.createStructureIfNotExisting(txt, ss, "PTV");
                    StructureHelpers.clearAllContours(tmp, ss);
                    tmp.SegmentVolume = p.Margin(7);
                    tmp.SegmentVolume = tmp.And(PTVe);
                    tmp.SegmentVolume = tmp.Sub(p);
                    nextlevel.SegmentVolume = nextlevel.Sub(tmp);
                    PTVinters.Add(tmp);
                }
            }

            foreach (var p in PTVinters)
            {
                PTVse.Last().SegmentVolume = PTVse.Last().Sub(p); // check if this works
                foreach (var t in PTVinters.TakeWhile(x => x != p))
                    p.SegmentVolume = p.Sub(t);
            }
            PTVe3mm.SegmentVolume = PTVe.Margin(3);


            // ======================================================

            // plan specific structures and their cropped versions
            BreastSide = SelectedBreastSide;
            LungIpsi = StructureHelpers.getStructureFromStructureSet(SelectedLungIpsi,ss,true);
            LungContra= StructureHelpers.getStructureFromStructureSet(SelectedLungContra,ss,true);
            Heart = StructureHelpers.getStructureFromStructureSet(SelectedHeart, ss,true);
            BreastContra = StructureHelpers.getStructureFromStructureSet(SelectedBreastContra, ss,true);
            LAD = StructureHelpers.getStructureFromStructureSet(SelectedLAD, ss,true);
            ptvSupra = StructureHelpers.getStructureFromStructureSet(SelectedSupraPTV, ss,true);
            var abody = StructureHelpers.getStructureFromStructureSet("0_abody", ss,true);
            #endregion

            Course Course = eps.Course;
            eps = Course.AddExternalPlanSetup(ss);
            eps.SetPrescription(NOF, new DoseValue(presc[0].Value, DoseValue.DoseUnit.Gy), 1.0);

            // breast target has to be named CTV High
            var PTVbreast = PTVs.FirstOrDefault(x => x.Id == "CTV High");
            if (PTVbreast == null)
            {
                MessageBox.Show("PTVp NOT FOUND!");
                return;
            }

            // now carefully place isocenter
            var posZmax = PTVbreast.MeshGeometry.Bounds.Z + 200 - 20 ; // leave margin for skinflash
            var posZtop = PTVbreast.MeshGeometry.Bounds.Z + PTVbreast.MeshGeometry.Bounds.SizeZ;
            var lungTop = LungIpsi.MeshGeometry.Bounds.Z + LungIpsi.MeshGeometry.Bounds.SizeZ-50; // 5cm below top of lung
            double posZ;
            posZ = lungTop > posZtop ? lungTop: posZtop;
            posZ = posZ > posZmax ? posZmax : posZ;
            VVector iso = new VVector(PTVe.MeshGeometry.Bounds.X + PTVe.MeshGeometry.Bounds.SizeX / 2,
                PTVe.MeshGeometry.Bounds.Y + PTVe.MeshGeometry.Bounds.SizeY / 2,
                posZ);

            //Beam b1 = eps.AddStaticBeam(machinePars, new VRect<double>(-50, -50, 50, 50), 0, 0, 0, iso);
            
            // one iso found, find optimal angle of medial fiel!
            Beam mf0;
            if (MFAngle is double.NaN)
            {
                if (BreastSide=="Left")
                    mf0 = BeamHelpers.minimizeLungDoseByRunningDoseCalc(machinePars, eps, PTVe, LungIpsi, 300, 330, 5, 20, 40, 5, iso);
                else
                    mf0 = BeamHelpers.minimizeLungDoseByRunningDoseCalc(machinePars, eps, PTVe, LungIpsi, 30, 60, 5, 20, 40, 5, iso);
                double optimalGanAngle = mf0.ControlPoints[0].GantryAngle;
                double optimalColAngle = mf0.ControlPoints[0].CollimatorAngle;
                eps.RemoveBeam(mf0);
                Console.WriteLine("second minimize");
                mf0 = BeamHelpers.minimizeLungDoseByRunningDoseCalc(machinePars, eps, PTVbreast, LungIpsi,
                    optimalGanAngle - 2,
                    optimalGanAngle + 2,
                    1,
                    optimalColAngle - 2,
                    optimalColAngle + 2,
                    1,
                    iso);
                optimalGanAngle = mf0.ControlPoints[0].GantryAngle;
                optimalColAngle = mf0.ControlPoints[0].CollimatorAngle;
                mf0.FitMLCToStructure(BeamHelpers.breastFBmarginsMed, PTVbreast, true, BeamHelpers.jawFit, BeamHelpers.olmp, BeamHelpers.clmp);
                mf0.Id = "mf0";
            }
            else
                mf0 = eps.AddStaticBeam(machinePars, BeamHelpers.fs10x10, MFAngleCol, MFAngle, 0, iso);

            // now create all other fields
            Beam mf1 = eps.AddStaticBeam(machinePars, BeamHelpers.fs10x10, 0, mf0.ControlPoints[0].GantryAngle+ 20, 0, iso);
            Beam mf2 = eps.AddStaticBeam(machinePars, BeamHelpers.fs10x10, 0, mf0.ControlPoints[0].GantryAngle + 40, 0, iso);
            mf1.Id = "mf1";
            mf2.Id = "mf2";
            mf0.FitCollimatorToStructure(BeamHelpers.margins10, PTVbreast, true, true, false);
            mf1.FitCollimatorToStructure(BeamHelpers.margins10, PTVbreast, true, true, false);
            mf2.FitCollimatorToStructure(BeamHelpers.margins10, PTVbreast, true, true, false);

            BeamHelpers.BreastOptimizeCollimatorAndJawsForIMRT(mf1, ss, PTVbreast, LungIpsi);


            //Messenger.Default.Send("Script Running Ended");
            MessageBox.Show("Done");
        }
    }
}