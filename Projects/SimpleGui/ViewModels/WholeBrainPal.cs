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
    public class WholeBrainPalViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runWholeBrainPalCommand { get; set; }
        public string SelectedLensL { get; set; }
        public string SelectedLensR { get; set; }
        public string SelectedBrain { get; set; }
        public double CollimatorAngle { get; set; }
        public ObservableCollection<string> ListOfOARs { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        private ExternalBeamMachineParameters machinePars;
        private string OptimizationAlgorithmModel;
        private string MLCid;
        private string DoseCalculationAlgo;
        private WholeBrainPalliativeScript wholeBrain;

        public WholeBrainPalViewModel(MainViewModel main)
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
          
            Messages = new ObservableCollection<Message>();
            SelectedLensL = ListOfOARs.FirstOrDefault(x => x.Contains("Lens_L"));
            SelectedLensR = ListOfOARs.FirstOrDefault(x => x.Contains("Lens_R"));
            Messenger.Default.Register<string>(this, AddEntry);
            wholeBrain = new WholeBrainPalliativeScript();
            CollimatorAngle = 50;
            runWholeBrainPalCommand = new RelayCommand(runWholeBrainPal);
        }

        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runWholeBrainPal()
        {
            wholeBrain.runWholeBrainPalliative(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, 
                SelectedLensL, SelectedLensR);
        }
    }
}
