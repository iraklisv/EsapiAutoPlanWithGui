﻿using GalaSoft.MvvmLight;
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
    public class ProstateViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runProstateCommand { get; set; }
        public ICommand runProstateOptimizationCommand { get; set; }
        public string SelectedRectum { get; set; }
        public string SelectedBladder { get; set; }
        public string SelectedBowel { get; set; }
        public string SelectedFemorL { get; set; }
        public string SelectedFemorR { get; set; }
        public double CollimatorAngle { get; set; }
        public double CropFromBody { get; set; }
        public bool JawTrakingOn { get; set; }
        public bool PostOpFlag { get; set; }
        public ObservableCollection<int> NumberOfArcs { get; set; }
        public int SelectedNumberOfArcs { get; set; }

        public double IsocenterOffset { get; set; }
        public ObservableCollection<string> listOfTargets { get; set; }
        public ObservableCollection<string> listOfOffsetOrigins { get; set; }
        public string SelectedTargetForIso { get; set; }
        public string SelectedOffsetOrigin { get; set; }

        public ObservableCollection<string> ListOfOARs { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        private ExternalBeamMachineParameters machinePars;
        private string OptimizationAlgorithmModel;
        private string MLCid;
        private string DoseCalculationAlgo;
        private ProstateScript prostate;

        public ProstateViewModel(MainViewModel main)
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
            PostOpFlag = false;
            NumberOfArcs = new ObservableCollection<int>();
            NumberOfArcs.Add(1);
            NumberOfArcs.Add(2);
            NumberOfArcs.Add(3);
            SelectedNumberOfArcs = 2;
            Messages = new ObservableCollection<Message>();

            //ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            //configFileMap.ExeConfigFilename = "OARnaming.config";
            //Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            //var oarNaming = config.AppSettings.Settings;
            //SelectedBladder = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Bladder"].Value));
            //SelectedRectum = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Rectum"].Value));
            //SelectedBowel = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["Bowel"].Value));
            //SelectedFemorL = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["FemurL"].Value));
            //SelectedFemorR = ListOfOARs.FirstOrDefault(x => x.Equals(oarNaming["FemurR"].Value));
            
            SelectedBladder = ListOfOARs.FirstOrDefault(x => x.Equals("Bladder"));
            SelectedRectum = ListOfOARs.FirstOrDefault(x => x.Equals("Rectum"));
            SelectedBowel = ListOfOARs.FirstOrDefault(x => x.Equals("Bowel"));
            SelectedFemorL = ListOfOARs.FirstOrDefault(x => x.Equals("FemurL"));
            SelectedFemorR = ListOfOARs.FirstOrDefault(x => x.Equals("FemurR"));

            listOfTargets = new ObservableCollection<string>();
            foreach (var x in IdDx) listOfTargets.Add(x.Key);
            SelectedTargetForIso = listOfTargets.FirstOrDefault();
            listOfOffsetOrigins = new ObservableCollection<string>();
            listOfOffsetOrigins.Add("Selected PTV Cranial Bound");
            listOfOffsetOrigins.Add("Selected PTV Center");
            listOfOffsetOrigins.Add("Selected PTV Caudal Bound");
            listOfOffsetOrigins.Add("Overall PTV Center");
            SelectedOffsetOrigin = listOfOffsetOrigins.FirstOrDefault(x => x.Contains("Caudal"));
            IsocenterOffset = 8;

            Messenger.Default.Register<string>(this, AddEntry);
            prostate = new ProstateScript();
            CollimatorAngle = 10;
            CropFromBody= 0;
            runProstateCommand = new RelayCommand(runProstate);
            runProstateOptimizationCommand = new RelayCommand(runProstateOptimization);
        }

        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runProstateOptimization()
        {
            prostate.runOptimization();
        }
        private void runProstate()
        {
            prostate.runProstateScript(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, CropFromBody, JawTrakingOn, PostOpFlag, SelectedNumberOfArcs,
                IsocenterOffset, SelectedTargetForIso, SelectedOffsetOrigin,
                SelectedRectum, SelectedBladder, SelectedBowel, SelectedFemorL, SelectedFemorR);
        }
    }
}
