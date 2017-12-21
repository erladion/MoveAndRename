using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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

        public ChoiceWindow(HashSet<Series> s)
        {
            InitializeComponent();

            foreach (var item in s)
            {
                Button b = new Button();
                b.Content = item.Name;
                b.Click += B_Click;
                this.ChoicePanel.Children.Add(b);
            }
        }

		private void R_Checked(object sender, RoutedEventArgs e)
		{
			if ((bool)((RadioButton)sender).IsChecked){
				RadioButton rb = (RadioButton)sender;
				choice = rb.Content.ToString();
                Debug.WriteLine("Setting choice to: " + rb.Content.ToString());
			}
		}

        private void B_Click(object sender, RoutedEventArgs e)
        {
            choice = ((Button)sender).Content.ToString();
            this.Close();
        }

        public static string GetChoice(HashSet<Series> s)
        {
            ChoiceWindow cw = new ChoiceWindow(s);
            cw.ShowDialog();

            return cw.choice;
        }
	}
}
