using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleGui.Views
{
    /// <summary>
    /// Interaction logic for BreastView.xaml
    /// </summary>
    public partial class BreastView : Window
    {
        public BreastView()
        {
            InitializeComponent();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            this.Top = 0;
            this.Left = 1000;
            //this.Topmost = true;
        }
    }
}
