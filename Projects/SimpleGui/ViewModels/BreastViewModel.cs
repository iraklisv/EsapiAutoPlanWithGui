using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SimpleGui.AutoPlans;
using SimpleGui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string SelectedBreastSide { get; set; }
        public string SelectedSupraPTV { get; set; }
        public double MFAngle { get; set; }
        public double MFCol { get; set; }
        public double CropFromBody { get; set; }
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
            ListOfPTVs = main.ListOfTargets;
            MLCid = main.SelectedMLCID;
            DoseCalculationAlgo = main.SelectedAlgorythm;
            OptimizationAlgorithmModel = main.SelectedOptimizationAlgorithmModel;
            BreastSide = new ObservableCollection<string>();
            //MFAngle = 315;
            //MFCol = 20;
            MFAngle = double.NaN;
            MFCol = double.NaN;
            CropFromBody = 4; // mm
            BreastSide.Add("Left");
            BreastSide.Add("Right");
            BreastSide.Add("Bilateral");
            SelectedBreastSide = BreastSide.FirstOrDefault();
            //SelectedBody = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("body"));
            SelectedLungIpsi = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("lung l"));
            SelectedLungContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("lung r"));
            SelectedHeart = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("heart"));
            SelectedBreastContra = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("breast"));
            SelectedLAD = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("lad"));
            //SelectedSupraCTV = ListOfCTVs.FirstOrDefault(x => x.ToLower().Contains("ctv ln"));
            SelectedSupraPTV = "";
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
                NumberOfFractions, IdDx,
                SelectedBreastSide, SelectedLungIpsi, SelectedLungContra, SelectedHeart, SelectedBreastContra, SelectedLAD, SelectedSupraPTV);
        }
        private void prepareIMRT()
        {
            brst.PrepareIMRT(Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                MFAngle, MFCol, CropFromBody,
                NumberOfFractions, IdDx,
                SelectedBreastSide, SelectedLungIpsi, SelectedLungContra, SelectedHeart, SelectedBreastContra, SelectedLAD, SelectedSupraPTV);
        }

    }
}
