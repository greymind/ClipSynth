using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClipSynth
{
	public partial class Favorites : Window
	{
        public bool CancelClose;

		public Favorites()
		{
            CancelClose = true;

			this.InitializeComponent();
		}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = CancelClose;
            Hide();
        }
	}
}