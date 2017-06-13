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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Empacotadora
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class Win_Init_class : Window
    {
        public Win_Init_class()
        {
            InitializeComponent();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
			MessageBox.Show("Abrir Main");
            Win_Main WMain = new Win_Main();
			WMain.Show();
			MessageBox.Show("Fechar Init");
			Close();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            //var answer = MessageBox.Show("Terminar o programa?", "Confirmar", MessageBoxButton.YesNo);
            //if(answer == MessageBoxResult.Yes)
            Application.Current.Shutdown();
        }
    }
}
