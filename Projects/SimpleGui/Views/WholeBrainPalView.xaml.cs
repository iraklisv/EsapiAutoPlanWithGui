using SimpleGui.ViewModels;
using System.Windows;

namespace SimpleGui.Views
{
    public partial class WholeBrainPalView : Window
    {
        public WholeBrainPalView()
        {
            InitializeComponent();
        }

        private void Window_Activated(object sender, System.EventArgs e)
        {
            this.Top = 0;
            this.Left = 1000;
            //this.Topmost = true;
        }
    }
}
