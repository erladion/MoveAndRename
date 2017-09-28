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

namespace MoveAndRename
{
	/// <summary>
	/// Interaction logic for ChoiceWindow.xaml
	/// </summary>
	public partial class ChoiceWindow : Window
	{
		public string choice = "";
		public ChoiceWindow(HashSet<string> s)
		{
			InitializeComponent();

			foreach (var item in s)
			{
				RadioButton r = new RadioButton();
				r.Content = item;
				r.Checked += R_Checked;
				this.ChoicePanel.Children.Add(r);
			}
			
		}

		private void R_Checked(object sender, RoutedEventArgs e)
		{
			if ((bool)((RadioButton)sender).IsChecked){
				RadioButton rb = (RadioButton)sender;
				choice = rb.Content.ToString();
			}
		}
	}
}
