using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SimpleGui.AutoPlans;
using SimpleGui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SimpleGui.ViewModels
{
    public class BreastViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runBreastFifCommand { get; set; }

        public ICommand PrepareVMATCommand { get; set; }
        public ICommand PrepareIMRTCommand { get; set; }
        public ICommand Prepare3DfifCommand { get; set; }
        public string SelectedLeftLungIpsi { get; set; }
        public string SelectedLeftLungContra { get; set; }
        public string SelectedLeftBreastContra { get; set; }
        public string SelectedLeftBreastPTV { get; set; }
        public string SelectedLeftBoostPTV { get; set; }
        public string SelectedLeftSupraPTV { get; set; }
        public string SelectedLeftIMNPTV { get; set; }
        public double SelectedLeftMFAngle { get; set; }
        public double SelectedLeftMFCol { get; set; }
        public double SelectedLeftIsocenterX { get; set; }
        public double SelectedLeftIsocenterY { get; set; }
        public double SelectedLeftIsocenterZ { get; set; }
        public string SelectedRightLungIpsi { get; set; }
        public string SelectedRightLungContra { get; set; }
        public string SelectedRightBreastContra { get; set; }
        public string SelectedRightBreastPTV { get; set; }
        public string SelectedRightBoostPTV { get; set; }
        public string SelectedRightSupraPTV { get; set; }
        public string SelectedRightIMNPTV { get; set; }
        public double SelectedRightMFAngle { get; set; }
        public double SelectedRightMFCol { get; set; }
        public double SelectedRightIsocenterX { get; set; }
        public double SelectedRightIsocenterY { get; set; }
        public double SelectedRightIsocenterZ { get; set; }
        public string SelectedHeart { get; set; }
        public string SelectedLAD { get; set; }
        public string SelectedEsophagus { get; set; }
        public string SelectedSpinalCord { get; set; }
        public double SelectedCropFromBody { get; set; }
        public ObservableCollection<string> ListOfOARs { get; set; }
        public ObservableCollection<string> ListOfPTVs { get; set; }
        public ObservableCollection<Message> BrstMessages { get; set; }
        private ExternalBeamMachineParameters machinePars;
        private string OptimizationAlgorithmModel;
        private string MLCid;
        private string DoseCalculationAlgo;
        private Breast brst;
        public BreastViewModel(MainViewModel main)
        {
            Patient = main.Patient;
            ExternalPlanSetup = main.ExternalPlanSetup;
            StructureSet = main.StructureSet;
            NumberOfFractions = main.NumberOfFractions;
            IdDx = main.IdDx.ToList();
            machinePars = main.machinePars.LastOrDefault();
            ListOfOARs = main.ListOfOARs;
            ListOfPTVs = new ObservableCollection<string>();
            foreach (var p in IdDx)
                ListOfPTVs.Add(p.Key);
            MLCid = main.SelectedMLCID;
            DoseCalculationAlgo = main.SelectedAlgorythm;
            OptimizationAlgorithmModel = main.SelectedOptimizationAlgorithmModel;

            SelectedCropFromBody = double.NaN;

            SelectedLeftMFAngle = double.NaN;
            SelectedLeftMFCol = double.NaN;
            SelectedLeftIsocenterX = double.NaN;
            SelectedLeftIsocenterY = double.NaN;
            SelectedLeftIsocenterZ = double.NaN;
            SelectedLeftLungIpsi = "LungL";
            SelectedLeftLungContra = "LungR";
            SelectedRightMFAngle = double.NaN;
            SelectedRightMFCol = double.NaN;
            SelectedRightIsocenterX = double.NaN;
            SelectedRightIsocenterY = double.NaN;
            SelectedRightIsocenterZ = double.NaN;
            SelectedRightLungIpsi = "LungR";
            SelectedRightLungContra = "LungL";

            //ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            //configFileMap.ExeConfigFilename = "OARnaming.config";
            //Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            //var oarNaming = config.AppSettings.Settings;
            //SelectedHeart = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Heart"].Value));
            //SelectedLAD = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["LAD"].Value));
            //SelectedEsophagus = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Esophagus"].Value));
            //SelectedSpinalCord = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["SpinalCord"].Value));
            //SelectedLeftBreastContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains(oarNaming["Breast"].Value.ToLower()));
            //SelectedRightBreastContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains(oarNaming["Breast"].Value.ToLower()));
            
            SelectedHeart = ListOfOARs.FirstOrDefault(x => x.Equals("Heart"));
            SelectedLAD = ListOfOARs.FirstOrDefault(x => x.Equals("LAD"));
            SelectedEsophagus = ListOfOARs.FirstOrDefault(x => x.Equals("Esophagus"));
            SelectedSpinalCord = ListOfOARs.FirstOrDefault(x => x.Equals("SpinalCrd"));
            SelectedLeftBreastContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("Breast"));
            SelectedRightBreastContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("Breast"));

            SelectedLeftSupraPTV = "";
            SelectedLeftBreastPTV = "";
            SelectedLeftBoostPTV = "";
            SelectedLeftIMNPTV = "";
            SelectedRightSupraPTV = "";
            SelectedRightBreastPTV = "";
            SelectedRightBoostPTV = "";
            SelectedRightIMNPTV = "";

            PrepareIMRTCommand = new RelayCommand(prepareIMRT);
            PrepareVMATCommand = new RelayCommand(prepareVMAT);
            Prepare3DfifCommand = new RelayCommand(prepare3Dfif);
            brst = new Breast();
            BrstMessages = new ObservableCollection<Message>();
        }
        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            BrstMessages.Add(message);
            RaisePropertyChanged();
        }
        private void prepare3Dfif()
        {
            //brst.runBreastFif(Patient, ExternalPlanSetup, StructureSet,
            //    machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
            //    NumberOfFractions, IdDx,
            //    SelectedHeart, SelectedLAD, SelectedEsophagus, SelectedSpinalCord,
            //    SelectedLeftMFAngle, SelectedLeftMFCol, SelectedLeftCropFromBody,
            //    SelectedLeftIsocenterX, SelectedLeftIsocenterY, SelectedLeftIsocenterZ,
            //    SelectedLeftLungIpsi, SelectedLeftLungContra, SelectedLeftBreastContra,
            //    SelectedLeftSupraPTV, SelectedLeftBreastPTV, SelectedLeftBoostPTV, SelectedLeftIMNPTV,
            //    SelectedRightMFAngle, SelectedRightMFCol, SelectedRightCropFromBody,
            //    SelectedRightIsocenterX, SelectedRightIsocenterY, SelectedRightIsocenterZ,
            //    SelectedRightLungIpsi, SelectedRightLungContra, SelectedRightBreastContra,
            //    SelectedRightSupraPTV, SelectedRightBreastPTV, SelectedRightBoostPTV, SelectedRightIMNPTV
            //    );
        }
        private void prepareIMRT()
        {
            brst.PrepareIMRT(Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx,
                SelectedHeart, SelectedLAD, SelectedEsophagus, SelectedSpinalCord, SelectedCropFromBody,
                SelectedLeftMFAngle, SelectedLeftMFCol,
                SelectedLeftIsocenterX, SelectedLeftIsocenterY, SelectedLeftIsocenterZ,
                SelectedLeftLungIpsi, SelectedLeftLungContra, SelectedLeftBreastContra,
                SelectedLeftSupraPTV, SelectedLeftBreastPTV, SelectedLeftBoostPTV, SelectedLeftIMNPTV,
                SelectedRightMFAngle, SelectedRightMFCol,
                SelectedRightIsocenterX, SelectedRightIsocenterY, SelectedRightIsocenterZ,
                SelectedRightLungIpsi, SelectedRightLungContra, SelectedRightBreastContra,
                SelectedRightSupraPTV, SelectedRightBreastPTV, SelectedRightBoostPTV, SelectedRightIMNPTV
                );
        }
        private void prepareVMAT()
        {
            brst.PrepareVMAT(Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx,
                SelectedHeart, SelectedLAD, SelectedEsophagus, SelectedSpinalCord, SelectedCropFromBody,
                SelectedLeftMFAngle, SelectedLeftMFCol,
                SelectedLeftIsocenterX, SelectedLeftIsocenterY, SelectedLeftIsocenterZ,
                SelectedLeftLungIpsi, SelectedLeftLungContra, SelectedLeftBreastContra,
                SelectedLeftSupraPTV, SelectedLeftBreastPTV, SelectedLeftBoostPTV, SelectedLeftIMNPTV,
                SelectedRightMFAngle, SelectedRightMFCol,
                SelectedRightIsocenterX, SelectedRightIsocenterY, SelectedRightIsocenterZ,
                SelectedRightLungIpsi, SelectedRightLungContra, SelectedRightBreastContra,
                SelectedRightSupraPTV, SelectedRightBreastPTV, SelectedRightBoostPTV, SelectedRightIMNPTV
                );
        }
    }
}
