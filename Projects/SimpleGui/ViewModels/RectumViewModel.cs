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

namespace SimpleGui.ViewModels
{
    public class RectumViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runRectumCommand { get; set; }
        public ICommand runRectumOptimizationCommand { get; set; }
        public string SelectedBladder { get; set; }
        public string SelectedBowel { get; set; }
        public string SelectedFemorL { get; set; }
        public string SelectedFemorR { get; set; }
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
        private RectumScript rct;

        public RectumViewModel(MainViewModel main)
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
            SelectedBladder = ListOfOARs.FirstOrDefault(x => x.Contains("Bladder"));
            SelectedBowel = ListOfOARs.FirstOrDefault(x => x.Contains("Bowel"));
            SelectedFemorL = ListOfOARs.FirstOrDefault(x => x.Contains("Femor_L"));
            SelectedFemorR = ListOfOARs.FirstOrDefault(x => x.Contains("Femor_R"));
            
            listOfTargets = new ObservableCollection<string>();
            foreach (var x in IdDx) listOfTargets.Add(x.Key);
            SelectedTargetForIso = listOfTargets.FirstOrDefault();
            listOfOffsetOrigins= new ObservableCollection<string>();
            listOfOffsetOrigins.Add("Selected PTV Cranial Bound");
            listOfOffsetOrigins.Add("Selected PTV Center");
            listOfOffsetOrigins.Add("Selected PTV Caudal Bound");
            listOfOffsetOrigins.Add("Overall PTV Center");
            SelectedOffsetOrigin = listOfOffsetOrigins.FirstOrDefault(x=>x.Contains("Caudal"));
            IsocenterOffset = 8;

            Messenger.Default.Register<string>(this, AddEntry);
            rct = new RectumScript();
            CollimatorAngle = 10;
            CropFromBody = 0;
            runRectumCommand = new RelayCommand(runRectum);
            runRectumOptimizationCommand = new RelayCommand(runRectumOptimization);
        }

        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runRectumOptimization()
        {
            rct.runOptimization();
        }
        private void runRectum()
        {
            rct.runRectumScript(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, CropFromBody, JawTrakingOn, SelectedNumberOfArcs,
                IsocenterOffset, SelectedTargetForIso, SelectedOffsetOrigin, 
                SelectedBladder, SelectedBowel, SelectedFemorL, SelectedFemorR);
        }
    }
}
