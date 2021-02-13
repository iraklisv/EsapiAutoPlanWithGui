using SimpleGui.ViewModels;
using System.Windows;

namespace SimpleGui.Views
{
    public partial class EsophagusView : Window
    {
        public EsophagusView()
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
