using GalaSoft.MvvmLight.Messaging;
using SimpleGui.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleGui.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private static GynecologyView _gynecologyView = null;
        private static RectumView _rectumView = null;
        private static LungView _lungView = null;
        private static EsophagusView _esophagusView = null;
        private static HeadNeckView _headNeckView = null;
        private static ProstateView _prostateView = null;
        private static BreastView _breastView = null;
        private static WholeBrainPalView _wholeBrainPalView = null;
        public MainViewModel MainViewModel;
        public MainView(MainViewModel vm)
        {
            InitializeComponent();
            MainViewModel = vm;
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);
        }
        private void NotificationMessageReceived(NotificationMessage msg)
        {
            if (msg.Notification == "ShowGynecologyView")
            {
                _gynecologyView = new GynecologyView();
                _gynecologyView.DataContext = new GynecologyViewModel(MainViewModel);
                _gynecologyView.ShowDialog();
            }
            if (msg.Notification == "ShowRectumView")
            {
                _rectumView = new RectumView();
                _rectumView.DataContext = new RectumViewModel(MainViewModel);
                _rectumView.ShowDialog();
            }
            if (msg.Notification == "ShowLungView")
            {
                _lungView = new LungView();
                _lungView.DataContext = new LungViewModel(MainViewModel);
                _lungView.ShowDialog();
            }
            if (msg.Notification == "ShowEsophagusView")
            {
                _esophagusView = new EsophagusView();
                _esophagusView.DataContext = new EsophagusViewModel(MainViewModel);
                _esophagusView.ShowDialog();
            }
            if (msg.Notification == "ShowHeadNeckView")
            {
                _headNeckView= new HeadNeckView();
                _headNeckView.DataContext = new HeadNeckViewModel(MainViewModel);
                _headNeckView.ShowDialog();
            }
            if (msg.Notification == "ShowProstateView")
            {
                _prostateView = new ProstateView();
                _prostateView.DataContext = new ProstateViewModel(MainViewModel);
                _prostateView.ShowDialog();
            }
            if (msg.Notification == "ShowBreastView")
            {
                _breastView = new BreastView();
                _breastView.DataContext = new BreastViewModel(MainViewModel);
                _breastView.ShowDialog();
            }
            if (msg.Notification == "ShowWholeBrainView")
            {
                _wholeBrainPalView = new WholeBrainPalView();
                _wholeBrainPalView.DataContext = new WholeBrainPalViewModel(MainViewModel);
                _wholeBrainPalView.ShowDialog();
            }
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            Window window = (Window)sender;
            //window.Topmost = true;
        }

        private void Window_Activated(object sender, System.EventArgs e)
        {
            //this.Topmost = true;
            this.Top = 0;
            this.Left = 0;
        }

        private void SetAllClicked(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null) btn.Background = Brushes.LightGreen;
        }
    }
}
