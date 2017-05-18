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

namespace Empacotadora
{
    /// <summary>
    /// Lógica interna para Win_Keypad.xaml
    /// </summary>
    public partial class Win_Keypad : Window
    {
        public string value;
        public bool enter = false, edited = false;

        public Win_Keypad()
        {
            InitializeComponent();
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            enter = false;
            edited = false;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            tbResult.Text += b.Content.ToString();
            edited = true;
        }

        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (tbResult.Text.Length > 0)
            {
                tbResult.Text = tbResult.Text.Substring(0, tbResult.Text.Length - 1);
            }
            edited = true;
        }

        private void C_Click(object sender, RoutedEventArgs e)
        {
            tbResult.Text = "";
            edited = true;
        }

        private void Enter_click(object sender, RoutedEventArgs e)
        {
            if (edited == true)
                value = tbResult.Text;
            enter = true;
            Close();
        }
    }
}
