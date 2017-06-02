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
using System.Windows.Threading;
using System.IO;
using System.Windows.Interop;
using System.Drawing;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Orders.xaml
	/// </summary>
	public partial class Win_Orders : Window {
		public static bool flagNewOrder = false;
		public static bool flagRecipes = false;

		public Win_Orders() {
			InitializeComponent();
			tabItemCurrentOrder.Visibility = Visibility.Collapsed;
			tabItemListOrders.Visibility = Visibility.Collapsed;
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			btnCurrentOrder.Background = Brushes.lightRed;
		}

		private void btnReturn_Click(object sender, RoutedEventArgs e) {
			flagNewOrder = false;
			Close();
		}
		private void btnNewOrder_Click(object sender, RoutedEventArgs e) {
			flagNewOrder = true;
			Close();
		}
		private void btnCurrentOrder_Click(object sender, RoutedEventArgs e) {
			SetCurrentOrderLayout();
		}
		private void btnListOrders_Click(object sender, RoutedEventArgs e) {
			SetOrdersListLayout();
			btnDeleteOrder.Visibility = Visibility.Visible;
			btnLoadOrder.Visibility = Visibility.Collapsed;
		}
		private void btnDeleteOrder_Click(object sender, RoutedEventArgs e) {
			OrderDetails datagridRow = GetDataFromGrid();
			try {
				var answer = MessageBox.Show("          Tem a certeza de que pretende\n" +
												 "             remover a seguinte ordem?\n\t" +
												 "              " + datagridRow.Name, "Confirmar?", MessageBoxButton.YesNo);
				if (answer == MessageBoxResult.Yes) {
					OrderDetails.DeactivateOrder(datagridRow.ID, Win_Main.path);
					datagridOrders.ItemsSource = null;
					datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(Win_Main.path);
				}
			}
			catch (NullReferenceException) {
				UpdateStatusBar("Selecione uma ordem para remover", 1);
			}
		}
		private void btnLoadNewOrder_Click(object sender, RoutedEventArgs e) {
			btnDeleteOrder.Visibility = Visibility.Collapsed;
			btnLoadOrder.Visibility = Visibility.Visible;
			SetOrdersListLayout();
			btnListOrders.ClearValue(BackgroundProperty);
			btnCurrentOrder.Background = Brushes.lightRed;
		}
		private void btnLoadOrder_Click(object sender, RoutedEventArgs e) {
			OrderDetails datagridRow = GetDataFromGrid();
			if (datagridRow != null) {
				General.currentOrder = datagridRow;
				var answer = MessageBox.Show("            Tem a certeza de que pretende\n" +
											 "               carregar a seguinte ordem?\n\t" +
											 "              " + General.currentOrder.Name +
											 "\n        A ordem em progresso vai ser terminada.", "Confirmar?", MessageBoxButton.YesNo);
				if (answer == MessageBoxResult.Yes) {
					if (General.currentOrder.Diameter == "") {
						gridRound.Visibility = Visibility.Collapsed;
						gridSquare.Visibility = Visibility.Visible;
						lblOrderWidth.Content = General.currentOrder.Width;
						lblOrderHeight.Content = General.currentOrder.Height;
					}
					else {
						gridRound.Visibility = Visibility.Visible;
						gridSquare.Visibility = Visibility.Collapsed;
						lblOrderDiam.Content = General.currentOrder.Diameter;
					}
					lblOrderName.Content = General.currentOrder.Name;
					lblOrderThick.Content = General.currentOrder.Thick;
					lblOrderLength.Content = General.currentOrder.Length;

					SetCurrentOrderLayout();
					UpdateStatusBar(General.currentOrder.Name + " " + General.currentOrder.Thick + " " + General.currentOrder.Length);
					// DispatcherTimer setup
					DispatcherTimer timer = new DispatcherTimer();
					timer.Tick += new EventHandler(Timer_Tick);
					timer.Interval = new TimeSpan(0, 0, 0, 3);
					timer.Start();
					lblLoadSuccess.Visibility = Visibility.Visible;
				}
			}
		}
		private void btnRecipes_Click(object sender, RoutedEventArgs e) {
			flagRecipes = true;
			Close();
		}
		private OrderDetails GetDataFromGrid() {
			OrderDetails datagridRow = null;
			try {
				datagridRow = (OrderDetails)datagridOrders.Items[datagridOrders.SelectedIndex];
			}
			catch (ArgumentOutOfRangeException) {
				UpdateStatusBar("Para continuar selecione uma ordem", 1);
			}
			return datagridRow;
		}
		// Green label "load success" timer
		private void Timer_Tick(object sender, EventArgs e) {
			DispatcherTimer timer = (DispatcherTimer)sender;
			lblLoadSuccess.Visibility = Visibility.Collapsed;
			timer.Stop();
		}
		// Status bar update
		private void SetStatusBarTimer() {
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(StatusBarTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 3);
			timer.Stop();
			timer.Start();
		}
		private void StatusBarTimer_Tick(object sender, EventArgs e) {
			DispatcherTimer timer = (DispatcherTimer)sender;
			timer.Stop();
			sbIcon.Visibility = Visibility.Collapsed;
			status.Content = "Pronto";
		}
		private void UpdateStatusBar(string msg) {
			status.Content = msg;
			SetStatusBarTimer();
		}
		private void UpdateStatusBar(string msg, byte error) {
			status.Content = msg;
			sbIcon.Visibility = Visibility.Visible;
			SetStatusBarTimer();
		}

		private void SetCurrentOrderLayout() {
			tabOrders.SelectedItem = tabItemCurrentOrder;
			lblTitle.Content = "Ordem atual";
			btnListOrders.ClearValue(BackgroundProperty);
			btnReturn.ClearValue(BackgroundProperty);
			btnNewOrder.ClearValue(BackgroundProperty);
			btnCurrentOrder.Background = Brushes.lightRed;
		}
		private void SetOrdersListLayout() {
			tabOrders.SelectedItem = tabItemListOrders;
			lblTitle.Content = "Ordens";
			try {
				datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(Win_Main.path);
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("Ficheiro das ordens não encontrado", 1);
			}
			btnListOrders.Background = Brushes.lightRed;
			btnReturn.ClearValue(BackgroundProperty);
			btnNewOrder.ClearValue(BackgroundProperty);
			btnCurrentOrder.ClearValue(BackgroundProperty);
		}
	}
}
