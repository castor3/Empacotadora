using System;
using System.Windows;
using System.Windows.Controls;

namespace Empacotadora
{
	/// <summary>
	/// Lógica interna para Win_Calculator.xaml
	/// </summary>
	public partial class Win_Calculator : Window
    {
        bool end = false;

        public Win_Calculator()
        {
            InitializeComponent();
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if (end == true)
            {
                tbResult.Text = "";
                end = false;
            }
            tbResult.Text += b.Content.ToString();
        }
        private void Del_Click(object sender, RoutedEventArgs e)
        {
            if (tbResult.Text.Length > 0)
            {
                tbResult.Text = tbResult.Text.Substring(0, tbResult.Text.Length - 1);
            }
        }
        private void C_Click(object sender, RoutedEventArgs e)
        {
            tbResult.Text = "";
        }
        private void Result()
        {
            String op;
            int iOp = 0;
            if (tbResult.Text.Contains("+"))
            {
                iOp = tbResult.Text.IndexOf("+");
            }
            else if (tbResult.Text.Contains("-"))
            {
                iOp = tbResult.Text.IndexOf("-");
            }
            else if (tbResult.Text.Contains("*"))
            {
                iOp = tbResult.Text.IndexOf("*");
            }
            else if (tbResult.Text.Contains("/"))
            {
                iOp = tbResult.Text.IndexOf("/");
            }
            else
            {
                //error    
            }

            op = tbResult.Text.Substring(iOp, 1);
            Double.TryParse(tbResult.Text.Substring(0, iOp), out double op1);
            Double.TryParse(tbResult.Text.Substring(iOp + 1, tbResult.Text.Length - iOp - 1), out double op2);

            if (op == "+")
            {
                tbResult.Text += "=" + (op1 + op2);
            }
            else if (op == "-")
            {
                tbResult.Text += "=" + (op1 - op2);
            }
            else if (op == "*")
            {
                tbResult.Text += "=" + (op1 * op2);
            }
            else
            {
                tbResult.Text += "=" + (op1 / op2);
            }
        }
        private void Result_click(object sender, RoutedEventArgs e)
        {
            try
            {
                Result();
            }
            catch (Exception)
            {
                tbResult.Text = "Error!";
            }
            finally
            {
                end = true;
            }

        }
    }
}
