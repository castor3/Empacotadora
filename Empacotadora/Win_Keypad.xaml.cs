using System.Windows;
using System.Windows.Controls;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Keypad.xaml
	/// </summary>
	public partial class Win_Keypad : Window
    {
        public string Value;
        public static bool Enter = false, Edited = false;

        public Win_Keypad()
        {
            InitializeComponent();
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Enter = false;
            Edited = false;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            tbResult.Text += b.Content.ToString();
            Edited = true;
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (tbResult.Text.Length > 0)
            {
                tbResult.Text = tbResult.Text.Substring(0, tbResult.Text.Length - 1);
            }
            Edited = true;
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            tbResult.Text = "";
            Edited = true;
        }

        private void Enter_click(object sender, RoutedEventArgs e)
        {
            if (Edited == true)
                Value = tbResult.Text;
            Enter = true;
            Close();
        }
    }
}
