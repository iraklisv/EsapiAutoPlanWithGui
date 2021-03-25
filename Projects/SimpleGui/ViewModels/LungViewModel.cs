using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using SimpleGui.AutoPlans;
using SimpleGui.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SimpleGui.ViewModels
{
    public class LungViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runLungCommand { get; set; }
        public ICommand runLungOptimizationCommand { get; set; }
        public string SelectedHeart { get; set; }
        public string SelectedLungL { get; set; }
        public string SelectedLungR { get; set; }
        public string SelectedSpinalCord { get; set; }
        public string SelectedEsophagus { get; set; }
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
        private LungScript lung;

        public LungViewModel(MainViewModel main)
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
            
            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = "OARnaming.config";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            var oarNaming = config.AppSettings.Settings;

            SelectedHeart       = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Heart"].Value));
            SelectedLungL       = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["LungL"].Value));
            SelectedLungR       = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["LungR"].Value));
            SelectedSpinalCord  = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["SpinalCord"].Value));
            SelectedEsophagus   = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Esophagus"].Value));

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
            lung = new LungScript();
            CollimatorAngle = 10;
            CropFromBody = 0;
            runLungCommand = new RelayCommand(runLung);
            runLungOptimizationCommand = new RelayCommand(runLungOptimization);
        }

        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runLungOptimization()
        {
            lung.runOptimization();
        }
        private void runLung()
        {
            lung.runLungScript(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, CropFromBody, JawTrakingOn, SelectedNumberOfArcs,
                IsocenterOffset, SelectedTargetForIso, SelectedOffsetOrigin,
                SelectedHeart, SelectedLungL, SelectedLungR, SelectedSpinalCord, SelectedEsophagus);
        }
    }
}
