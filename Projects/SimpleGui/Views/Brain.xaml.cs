﻿using SimpleGui.ViewModels;
using System.Windows;

namespace SimpleGui.Views
{
    public partial class BrainView : Window
    {
        public BrainView()
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
