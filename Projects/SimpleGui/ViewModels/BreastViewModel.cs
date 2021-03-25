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

        public ICommand PrepareIMRTCommand { get; set; }
        public ICommand Prepare3DfifCommand { get; set; }
        //public string SelectedBody { get; set; }
        public string SelectedLungIpsi { get; set; }
        public string SelectedLungContra { get; set; }
        public string SelectedHeart { get; set; }
        public string SelectedBreastContra { get; set; }
        public string SelectedLAD { get; set; }
        public string SelectedEsophagus { get; set; }
        public string SelectedSpinalCord { get; set; }
        public string SelectedBreastSide { get; set; }
        public string SelectedSupraPTV { get; set; }
        public string SelectedBreastPTV { get; set; }
        public string SelectedBoostPTV { get; set; }
        public string SelectedCropStructure { get; set; }
        public double MFAngle { get; set; }
        public double MFCol { get; set; }
        public double CropFromBody { get; set; }
        public double IsocenterX { get; set; }
        public double IsocenterY { get; set; }
        public double IsocenterZ { get; set; }
        public ObservableCollection<string> ListOfOARs { get; set; }
        public ObservableCollection<string> ListOfPTVs { get; set; }
        public ObservableCollection<string> BreastSide { get; set; }
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
            BreastSide = new ObservableCollection<string>();
            MFAngle = double.NaN;
            MFCol = double.NaN;
            IsocenterX = double.NaN;
            IsocenterY = double.NaN;
            IsocenterZ = double.NaN;
            CropFromBody = double.NaN;
            BreastSide.Add("Left");
            BreastSide.Add("Right");
            BreastSide.Add("Bilateral");
            SelectedBreastSide = "";
            SelectedLungIpsi = "";
            SelectedLungContra = "";


            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = "OARnaming.config";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            var oarNaming = config.AppSettings.Settings;

            SelectedHeart           = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Heart"].Value));
            SelectedBreastContra    = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains(oarNaming["Breast"].Value.ToLower()));
            SelectedLAD             = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["LAD"].Value));
            SelectedEsophagus       = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Esophagus"].Value));
            SelectedSpinalCord      = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["SpinalCord"].Value));
            SelectedSupraPTV = "";
            SelectedBreastPTV = "";
            SelectedBoostPTV = "";
            PrepareIMRTCommand = new RelayCommand(prepareIMRT);
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
            brst.runBreastFif(Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                MFAngle, MFCol, CropFromBody,
                IsocenterX, IsocenterY, IsocenterZ,
                NumberOfFractions, IdDx,
                SelectedBreastSide, SelectedLungIpsi, SelectedLungContra, SelectedHeart, SelectedBreastContra, SelectedLAD, SelectedSupraPTV, SelectedBreastPTV);
        }
        private void prepareIMRT()
        {
            brst.PrepareIMRT(Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                MFAngle, MFCol, CropFromBody,
                IsocenterX, IsocenterY, IsocenterZ,
                NumberOfFractions, IdDx,
                SelectedBreastSide, SelectedLungIpsi, SelectedLungContra, SelectedHeart, SelectedBreastContra, SelectedLAD, SelectedEsophagus, SelectedSpinalCord, SelectedSupraPTV, SelectedBreastPTV, SelectedBoostPTV);
        }
    }
}
