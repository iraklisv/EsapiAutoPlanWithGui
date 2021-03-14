using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using SimpleGui.AutoPlans;
using SimpleGui.Models;
using SimpleGui.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace SimpleGui.ViewModels
{
    public class MainViewModel: ViewModelBase
    {
        private GynecologyViewModel GynecologyViewModel;
        private RectumViewModel RectumViewModel;
        private LungViewModel LungViewModel;
        private EsophagusViewModel EsophagusViewModel;
        private HeadNeckViewModel HeadNeckViewModel;
        private ProstateViewModel ProstateViewModel;
        public Patient Patient;
        public ExternalPlanSetup ExternalPlanSetup;
        public StructureSet StructureSet;
        public string SelectedTargetId { get; set; }
        public double DX { get; set; }
        public int SelectedFromIdDx { get; set; }
        public int NumberOfFractions { get; set; }
        public PrescriptionList[] PrescriptionsList { get; set; }
        public ObservableCollection<KeyValuePair<string, double>> IdDx { get; set; } = new ObservableCollection<KeyValuePair<string, double>>();
        public ObservableCollection<StructureList> StructuresList { get; set; }
        public ObservableCollection<string> ListOfTargets { get; set; }
        public ObservableCollection<string> ListOfCTVs { get; set; }
        public ObservableCollection<string> ListOfOARs { get; set; }
        public ICommand ShowGynecologyCommand { get; set; } = new RelayCommand(ShowGynecology);
        public ICommand ShowRectumCommand { get; set; } = new RelayCommand(ShowRectum);
        public ICommand ShowLungCommand { get; set; } = new RelayCommand(ShowLung);
        public ICommand ShowEsophagusCommand { get; set; } = new RelayCommand(ShowEsophagus);
        public ICommand ShowHeadNeckCommand { get; set; } = new RelayCommand(ShowHeadNeck);
        public ICommand ShowProstateCommand { get; set; } = new RelayCommand(ShowProstate);
        public ICommand ShowBreastCommand { get; set; } = new RelayCommand(ShowBreast);
        public ICommand ShowWholeBrainCommand { get; set; } = new RelayCommand(ShowWholeBrain);
        private static void ShowGynecology() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowGynecologyView"));
        private static void ShowRectum() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowRectumView"));
        private static void ShowLung() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowLungView"));
        private static void ShowEsophagus() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowEsophagusView"));
        private static void ShowHeadNeck() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowHeadNeckView"));
        private static void ShowProstate() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowProstateView"));
        private static void ShowBreast() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowBreastView"));
        private static void ShowWholeBrain() => Messenger.Default.Send<NotificationMessage>(new NotificationMessage("ShowWholeBrainView"));
        public ICommand AddToPrescriptionCommand => new RelayCommand(AddToPrescription);
        private void AddToPrescription() => IdDx.Add(new KeyValuePair<string, double>(this.SelectedTargetId, this.DX));
        public ICommand RemoveFromPrescriptionCommand => new RelayCommand(RemoveFromPrescription);
        private void RemoveFromPrescription() => IdDx.RemoveAt(SelectedFromIdDx);
        public ObservableCollection<string> MachineIDs { get; set; }
        public string SelectedMachineID { get; set; }
        public ObservableCollection<string> BeamEnergies { get; set; }
        public string SelectedBeamEnergy { get; set; }
        public bool FFFon { get; set; }
        public ObservableCollection<string> TechniqueIDs { get; set; }
        public string SelectedTechniqueID { get; set; }
        public ObservableCollection<string> Algorythms { get; set; }
        public string SelectedAlgorythm { get; set; }
        public ObservableCollection<string> OptimizationAlgorithmModels { get; set; }
        public string SelectedOptimizationAlgorithmModel { get; set; }
        public ObservableCollection<string> MLCIDs { get; set; }
        public string SelectedMLCID { get; set; }


        public ICommand SetModelsCommand => new RelayCommand(SetAllModels);
        public ObservableCollection<ExternalBeamMachineParameters> machinePars { get; set; } = new ObservableCollection<ExternalBeamMachineParameters>();

        private void SetAllModels()
        {
            machinePars.Clear();
            machinePars.Add(new ExternalBeamMachineParameters(SelectedMachineID, SelectedBeamEnergy, FFFon ? 1400 : 600, SelectedTechniqueID, FFFon ? "FFF" : string.Empty));
        }

        public MainViewModel(Patient patient, ExternalPlanSetup eps, StructureSet ss)
        {
            this.Patient = patient;
            this.ExternalPlanSetup = eps;
            this.StructureSet = ss;
            this.DX = 2;
            MachineIDs = new ObservableCollection<string>();
            MachineIDs.Add("TB3377");
            MachineIDs.Add("VB3380");
            SelectedMachineID = MachineIDs.FirstOrDefault();
            BeamEnergies = new ObservableCollection<string>();
            BeamEnergies.Add("6X");
            BeamEnergies.Add("10X");
            SelectedBeamEnergy = BeamEnergies.FirstOrDefault();
            
            TechniqueIDs = new ObservableCollection<string>();
            TechniqueIDs.Add("ARC");
            TechniqueIDs.Add("STATIC");
            SelectedTechniqueID = TechniqueIDs.FirstOrDefault();
            Algorythms = new ObservableCollection<string>();
            Algorythms.Add("AAA15.5.12");
            Algorythms.Add("Acuros15.5.11");
            SelectedAlgorythm = Algorythms.FirstOrDefault();
            FFFon = false;

            OptimizationAlgorithmModels = new ObservableCollection<string>();
            OptimizationAlgorithmModels.Add("PO15.5.11");
            SelectedOptimizationAlgorithmModel = OptimizationAlgorithmModels.FirstOrDefault();

            MLCIDs = new ObservableCollection<string>();
            MLCIDs.Add("1535");
            SelectedMLCID = MLCIDs.FirstOrDefault();

            StructuresList = new ObservableCollection<StructureList>();
            ListOfTargets = new ObservableCollection<string>();
            ListOfOARs = new ObservableCollection<string>();
            ListOfCTVs = new ObservableCollection<string>();
            getSSList(ss);
            getPrescriptionsList(eps);
            GynecologyViewModel = new GynecologyViewModel(this);
            RectumViewModel = new RectumViewModel(this);
            LungViewModel = new LungViewModel(this);
            EsophagusViewModel = new EsophagusViewModel(this);
            HeadNeckViewModel = new HeadNeckViewModel(this);
            ProstateViewModel = new ProstateViewModel(this);
            SelectedTargetId = ListOfTargets.FirstOrDefault();
            NumberOfFractions = 25;
        }
        public void getSSList(StructureSet ss)
        {
            var structs = ss.Structures.ToList();
            foreach (var x in structs) // populate structure list in the view
                if (!x.Id.Contains("0_"))
                StructuresList.Add(new StructureList
                {
                    StructureIds = x.Id,
                    StructureTypes = x.DicomType,
                    StructureVolumeCC = string.Format("{0}", x.HasSegment ? x.Volume.ToString("0.0") : "nan")
                });
            foreach (var s in structs) // populate Target combobox with PTV list
            {
                if (!s.Id.Contains("0_"))
                {
                    if (s.Id.Length > 10)
                    {
                        MessageBox.Show($"{s.Id} has too many chars (max 10)");
                    }

                    if (s.DicomType.Equals("PTV")) ListOfTargets.Add(s.Id);
                    if (s.DicomType.Equals("ORGAN")) ListOfOARs.Add(s.Id);
                    if (s.DicomType.Equals("CTV")) ListOfCTVs.Add(s.Id);
                }
            }
        }
        public void getPrescriptionsList(ExternalPlanSetup eps)
        {
            RTPrescription rtp = eps.RTPrescription?.LatestRevision;
            if (rtp != null)
                PrescriptionsList = rtp.Targets.Select(x => new PrescriptionList
                {
                    TargetId = x.TargetId,
                    dx = x.DosePerFraction.ToString(),
                    dr = rtp.NumberOfFractions.ToString()
                }).ToArray() ?? new PrescriptionList[0];
            else PrescriptionsList = new PrescriptionList[0];
        }
    }
}
