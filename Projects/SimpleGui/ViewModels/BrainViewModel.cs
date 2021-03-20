using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using SimpleGui.AutoPlans;
using SimpleGui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

using System.Configuration;

namespace SimpleGui.ViewModels
{
    public class BrainViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runBrainCommand { get; set; }
        public ICommand runBrainOptimizationCommand { get; set; }

        //public string SelectedMandible { get; set; }
        //public string SelectedParotidL { get; set; }
        //public string SelectedParotidR { get; set; }
        //public string SelectedSpinalCord { get; set; }
        public string SelectedBrainStem { get; set; }
        public string SelectedOpticNerveL { get; set; }
        public string SelectedOpticNerveR { get; set; }
        public string SelectedEyeL { get; set; }
        public string SelectedEyeR { get; set; }
        public string SelectedLensL { get; set; }
        public string SelectedLensR { get; set; }
        public string SelectedCochleaL { get; set; }
        public string SelectedCochleaR { get; set; }
        public string SelectedHippoL { get; set; }
        public string SelectedHippoR { get; set; }
        public string SelectedChiasm { get; set; }
        //public string SelectedEsophagus { get; set; }
        public double CollimatorAngle { get; set; }
        public double CropFromBody { get; set; }
        public bool JawTrakingOn { get; set; }
        public ObservableCollection<int> NumberOfArcs { get; set; }

        public double IsocenterOffset { get; set; }
        public ObservableCollection<string> listOfTargets { get; set; }
        public ObservableCollection<string> listOfOffsetOrigins { get; set; }
        public string SelectedTargetForIso { get; set; }
        public string SelectedOffsetOrigin { get; set; }

        public int SelectedNumberOfArcs { get; set; }
        public ObservableCollection<string> ListOfOARs { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        private ExternalBeamMachineParameters machinePars;
        private string OptimizationAlgorithmModel;
        private string MLCid;
        private string DoseCalculationAlgo;
        private BrainScript Brain;

        public BrainViewModel(MainViewModel main)
        {
            Patient = main.Patient;
            ExternalPlanSetup = main.ExternalPlanSetup;
            StructureSet = main.StructureSet;
            NumberOfFractions = main.NumberOfFractions;
            IdDx = main.IdDx.ToList();
            machinePars = main.machinePars.LastOrDefault();
            ListOfOARs = main.ListOfOARs;
            MLCid = main.SelectedMLCID;
            DoseCalculationAlgo = main.SelectedAlgorythm;
            OptimizationAlgorithmModel = main.SelectedOptimizationAlgorithmModel;
            JawTrakingOn = false;
            NumberOfArcs = new ObservableCollection<int>();
            NumberOfArcs.Add(1);
            NumberOfArcs.Add(2);
            NumberOfArcs.Add(3);
            SelectedNumberOfArcs = 2;
            Messages = new ObservableCollection<Message>();
            SelectedBrainStem = ListOfOARs.FirstOrDefault(x => x.Contains("BrainStem"));
            SelectedOpticNerveL = ListOfOARs.FirstOrDefault(x => x.Contains("ONerveL"));
            SelectedOpticNerveR = ListOfOARs.FirstOrDefault(x => x.Contains("ONerveR"));
            SelectedEyeL = ListOfOARs.FirstOrDefault(x => x.Contains("EyeL"));
            SelectedEyeR = ListOfOARs.FirstOrDefault(x => x.Contains("EyeR"));
            SelectedLensL = ListOfOARs.FirstOrDefault(x => x.Contains("LensL"));
            SelectedLensR = ListOfOARs.FirstOrDefault(x => x.Contains("LensR"));
            SelectedCochleaL = ListOfOARs.FirstOrDefault(x => x.Contains("CochleaL"));
            SelectedCochleaR = ListOfOARs.FirstOrDefault(x => x.Contains("CochleaR"));
            SelectedHippoL = ListOfOARs.FirstOrDefault(x => x.Contains("HippoL"));
            SelectedHippoR = ListOfOARs.FirstOrDefault(x => x.Contains("HippoR"));
            SelectedChiasm = ListOfOARs.FirstOrDefault(x => x.Contains("Chiasm"));

            listOfTargets = new ObservableCollection<string>();
            foreach (var x in IdDx) listOfTargets.Add(x.Key);
            SelectedTargetForIso = listOfTargets.FirstOrDefault();
            listOfOffsetOrigins = new ObservableCollection<string>();
            listOfOffsetOrigins.Add("Selected PTV Cranial Bound");
            listOfOffsetOrigins.Add("Selected PTV Center");
            listOfOffsetOrigins.Add("Selected PTV Caudal Bound");
            listOfOffsetOrigins.Add("Overall PTV Center");
            SelectedOffsetOrigin = listOfOffsetOrigins.Last();
            IsocenterOffset = 0;

            Messenger.Default.Register<string>(this, AddEntry);
            Brain = new BrainScript();
            CollimatorAngle = 10;
            CropFromBody = double.NaN;
            runBrainCommand = new RelayCommand(runBrain);
            runBrainOptimizationCommand = new RelayCommand(runBrainOptimization);
        }

        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runBrainOptimization()
        {
            Brain.runOptimization();
        }
        private void runBrain()
        {
            Brain.runBrainScript(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, CropFromBody, JawTrakingOn, SelectedNumberOfArcs,
                IsocenterOffset, SelectedTargetForIso, SelectedOffsetOrigin,
                //SelectedMandible, SelectedParotidL, SelectedParotidR, SelectedSpinalCord, 
                SelectedBrainStem, SelectedOpticNerveL, SelectedOpticNerveR, SelectedEyeL, SelectedEyeR,
                SelectedLensL,SelectedLensR,
                SelectedCochleaL, SelectedCochleaR, 
                SelectedHippoL, SelectedHippoR, 
                SelectedChiasm);
        }
    }
}
