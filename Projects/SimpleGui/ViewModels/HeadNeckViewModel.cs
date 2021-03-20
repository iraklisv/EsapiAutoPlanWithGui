﻿using GalaSoft.MvvmLight;
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
    //public class UrlsSection : ConfigurationSection
    //{
    //    [ConfigurationProperty("name", DefaultValue = "Contoso",
    //        IsRequired = true, IsKey = true)]
    //    public string Name
    //    {
    //        get
    //        {
    //            return (string)this["name"];
    //        }
    //        set
    //        {
    //            this["name"] = value;
    //        }
    //    }

    //    [ConfigurationProperty("url", DefaultValue = "http://www.contoso.com",
    //        IsRequired = true)]
    //    [RegexStringValidator(@"\w+:\/\/[\w.]+\S*")]
    //    public string Url
    //    {
    //        get
    //        {
    //            return (string)this["url"];
    //        }
    //        set
    //        {
    //            this["url"] = value;
    //        }
    //    }

    //    [ConfigurationProperty("port", DefaultValue = (int)0, IsRequired = false)]
    //    [IntegerValidator(MinValue = 0, MaxValue = 8080, ExcludeRange = false)]
    //    public int Port
    //    {
    //        get
    //        {
    //            return (int)this["port"];
    //        }
    //        set
    //        {
    //            this["port"] = value;
    //        }
    //    }
    //}

    public class HeadNeckViewModel : ViewModelBase
    {
        private Patient Patient;
        private ExternalPlanSetup ExternalPlanSetup;
        private StructureSet StructureSet;
        private int NumberOfFractions;
        private List<KeyValuePair<string, double>> IdDx;
        public ICommand runHeadNeckCommand { get; set; }
        public ICommand runHeadNeckOptimizationCommand { get; set; }

        public string SelectedMandible { get; set; }
        public string SelectedParotidL { get; set; }
        public string SelectedParotidR { get; set; }
        public string SelectedSpinalCord { get; set; }
        public string SelectedBrainStem { get; set; }
        public string SelectedOpticNerveL { get; set; }
        public string SelectedOpticNerveR { get; set; }
        public string SelectedEyeL { get; set; }
        public string SelectedEyeR { get; set; }
        public string SelectedCochleaL { get; set; }
        public string SelectedCochleaR { get; set; }
        public string SelectedChiasm { get; set; }
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
        private HeadNeckScript HeadNeck;

        public HeadNeckViewModel(MainViewModel main)
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
            SelectedNumberOfArcs = 3;
            Messages = new ObservableCollection<Message>();
            SelectedParotidL = ListOfOARs.FirstOrDefault(x => x.Contains("Parotid L"));
            SelectedParotidR = ListOfOARs.FirstOrDefault(x => x.Contains("Parotid R"));
            SelectedMandible = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("mandible"));
            SelectedSpinalCord = ListOfOARs.FirstOrDefault(x => x.ToLower().Contains("spinalcrd"));
            SelectedBrainStem = ListOfOARs.FirstOrDefault(x => x.Contains("BrainStem"));
            SelectedOpticNerveL = ListOfOARs.FirstOrDefault(x => x.Contains("OpticNerveL"));
            SelectedOpticNerveR = ListOfOARs.FirstOrDefault(x => x.Contains("OpticNerveR"));
            SelectedEyeL = ListOfOARs.FirstOrDefault(x => x.Contains("EyeL"));
            SelectedEyeR = ListOfOARs.FirstOrDefault(x => x.Contains("EyeR"));
            SelectedCochleaL = ListOfOARs.FirstOrDefault(x => x.Contains("CochleaL"));
            SelectedCochleaR = ListOfOARs.FirstOrDefault(x => x.Contains("CochleaR"));
            SelectedChiasm = ListOfOARs.FirstOrDefault(x => x.Contains("Chiasm"));
            SelectedEsophagus= ListOfOARs.FirstOrDefault(x => x.Contains("Esophagus"));

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
            HeadNeck = new HeadNeckScript();
            CollimatorAngle = 10;
            CropFromBody = double.NaN;
            runHeadNeckCommand = new RelayCommand(runHeadNeck);
            runHeadNeckOptimizationCommand = new RelayCommand(runHeadNeckOptimization);
        }

        //static void CreateCustomSection()
        //{
        //    try
        //    {
        //        // Create a custom configuration section.
        //        UrlsSection customSection = new UrlsSection();

        //        // Get the current configuration file.
        //        System.Configuration.Configuration config =
        //                ConfigurationManager.OpenExeConfiguration(
        //                ConfigurationUserLevel.None);

        //        // Add the custom section to the application
        //        // configuration file.
        //        if (config.Sections["CustomSection"] == null)
        //        {
        //            config.Sections.Add("CustomSection", customSection);
        //        }

        //        // Save the application configuration file.
        //        customSection.SectionInformation.ForceSave = true;
        //        config.Save(ConfigurationSaveMode.Modified);

        //        Console.WriteLine("Created custom section in the application configuration file: {0}",
        //            config.FilePath);
        //        Console.WriteLine();
        //    }
        //    catch (ConfigurationErrorsException err)
        //    {
        //        Console.WriteLine("CreateCustomSection: {0}", err.ToString());
        //    }
        //}


        private void AddEntry(string msg)
        {
            Message message = new Message();
            message.MessageContent = msg;
            message.PublishDate = DateTime.Now;
            Messages.Add(message);
            RaisePropertyChanged();
        }
        private void runHeadNeckOptimization()
        {
            HeadNeck.runOptimization();
        }
        private void runHeadNeck()
        {
            HeadNeck.runHeadNeckScript(this.Patient, ExternalPlanSetup, StructureSet,
                machinePars, OptimizationAlgorithmModel, DoseCalculationAlgo, MLCid,
                NumberOfFractions, IdDx, CollimatorAngle, CropFromBody, JawTrakingOn, SelectedNumberOfArcs,
                IsocenterOffset, SelectedTargetForIso, SelectedOffsetOrigin,
                SelectedMandible, SelectedParotidL, SelectedParotidR, SelectedSpinalCord, SelectedBrainStem, SelectedOpticNerveL, SelectedOpticNerveR, SelectedEyeL, SelectedEyeR, SelectedCochleaL, SelectedCochleaR, SelectedChiasm, SelectedEsophagus);
        }
    }
}
