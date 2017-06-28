using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Calculator.xaml
	/// </summary>
	public partial class Win_Calculator : Window {
		bool _end = false;

		public Win_Calculator() {
			InitializeComponent();
		}

		private void btnSair_Click(object sender, RoutedEventArgs e) {
			Close();
		}
		private void Button_Click_1(object sender, RoutedEventArgs e) {
			Button b = (Button)sender;
			if (_end == true) {
				tbResult.Text = "";
				_end = false;
			}
			tbResult.Text += b.Content.ToString();
		}
		private void Del_Click(object sender, RoutedEventArgs e) {
			if (tbResult.Text.Length > 0) {
				tbResult.Text = tbResult.Text.Substring(0, tbResult.Text.Length - 1);
			}
		}
		private void C_Click(object sender, RoutedEventArgs e) {
			tbResult.Text = "";
		}
		private void Result() {
			int indexOfOperation = 0;
			if (tbResult.Text.Contains("+"))
				indexOfOperation = tbResult.Text.IndexOf("+");
			else if (tbResult.Text.Contains("-"))
				indexOfOperation = tbResult.Text.IndexOf("-");
			else if (tbResult.Text.Contains("*"))
				indexOfOperation = tbResult.Text.IndexOf("*");
			else if (tbResult.Text.Contains("/"))
				indexOfOperation = tbResult.Text.IndexOf("/");
			else {
				//error    
			}

			string operation = tbResult.Text.Substring(indexOfOperation, 1);
			double.TryParse(tbResult.Text.Substring(0, indexOfOperation), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double op1);
			double.TryParse(tbResult.Text.Substring(indexOfOperation + 1, tbResult.Text.Length - indexOfOperation - 1), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double op2);

			switch (operation) {
				case "+":
					tbResult.Text += "=" + (op1 + op2);
					break;
				case "-":
					tbResult.Text += "=" + (op1 - op2);
					break;
				case "*":
					tbResult.Text += "=" + (op1 * op2);
					break;
				default:
					tbResult.Text += "=" + (op1 / op2);
					break;
			}
		}
		private void Result_click(object sender, RoutedEventArgs e) {
			try {
				Result();
			}
			catch (Exception) {
				tbResult.Text = "Error!";
			}
			finally {
				_end = true;
			}

		}
	}
}
