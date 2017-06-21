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
		public enum Layout { Default, NewOrder, EditOrder, Recipes }
		public static Layout NextLayout;
		bool orderIsLoaded = false;
		Visibility _visible = Visibility.Visible;
		Visibility _collapsed = Visibility.Collapsed;

		public Win_Orders() {
			InitializeComponent();
			tabItemCurrentOrder.Visibility = _collapsed;
			tabItemListOrders.Visibility = _collapsed;
			FillControlsWithCurrentOrder();
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
		private void btnReturn_Click(object sender, RoutedEventArgs e) {
			NextLayout = Layout.Default;
			Close();
		}
		private void btnNewOrder_Click(object sender, RoutedEventArgs e) {
			NextLayout = Layout.NewOrder;
			Close();
		}
		private void btnCurrentOrder_Click(object sender, RoutedEventArgs e) {
			SetCurrentOrderLayout();
		}
		private void btnListOrders_Click(object sender, RoutedEventArgs e) {
			SetOrdersListLayout();
			btnDeleteOrder.Visibility = _visible;
			btnLoadOrder.Visibility = _collapsed;
		}
		private void btnDeleteOrder_Click(object sender, RoutedEventArgs e) {
			OrderDetails datagridRow = GetDataFromGrid();
			try {
				MessageBoxResult answer = MessageBox.Show("          Tem a certeza de que pretende\n" +
												 "             remover a seguinte ordem?\n\t" +
												 "              " + datagridRow.Name, "Confirmar?", MessageBoxButton.YesNo);
				if (answer != MessageBoxResult.Yes) return;
				OrderDetails.DeactivateOrder(datagridRow.ID, General.Path);
				datagridOrders.ItemsSource = null;
				datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.Path);
			}
			catch (NullReferenceException) {
				UpdateStatusBar("Selecione uma ordem para remover", 1);
			}
		}
		private void btnLoadNewOrder_Click(object sender, RoutedEventArgs e) {
			btnDeleteOrder.Visibility = _collapsed;
			btnLoadOrder.Visibility = _visible;
			SetOrdersListLayout();
			btnListOrders.ClearValue(BackgroundProperty);
			btnCurrentOrder.Background = Brushes.LightRed;
		}
		private void btnLoadOrder_Click(object sender, RoutedEventArgs e) {
			OrderDetails datagridRow = GetDataFromGrid();
			if (datagridRow == null) return;
			Win_Main.CurrentOrder = datagridRow;
			MessageBoxResult answer = MessageBox.Show("            Tem a certeza de que pretende\n" +
													  "               carregar a seguinte ordem?\n\t" +
													  "              " + Win_Main.CurrentOrder.Name +
													  "\n        A ordem em progresso vai ser terminada.", "Confirmar?", MessageBoxButton.YesNo);
			if (answer != MessageBoxResult.Yes) return;
			FillControlsWithCurrentOrder();

			SetCurrentOrderLayout();
			UpdateStatusBar(Win_Main.CurrentOrder.Name + " " + Win_Main.CurrentOrder.Thick + " " + Win_Main.CurrentOrder.Length);
			ShowGreenLabelLoadSuccessful();
		}
		private void btnEditOrder_Click(object sender, RoutedEventArgs e) {
			if (orderIsLoaded) {
				NextLayout = Layout.EditOrder;
				Close();
			}
			else
				UpdateStatusBar("Tem que carregar uma ordem antes de editar", 1);
		}
		private void btnRecipes_Click(object sender, RoutedEventArgs e) {
			NextLayout = Layout.Recipes;
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
			lblLoadSuccess.Visibility = _collapsed;
			timer.Stop();
		}

		#region StatusBar
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
			sbIcon.Visibility = _collapsed;
			status.Content = "Pronto";
		}
		private void UpdateStatusBar(string msg) {
			status.Content = msg;
			SetStatusBarTimer();
		}
		private void UpdateStatusBar(string msg, byte error) {
			status.Content = msg;
			sbIcon.Visibility = _visible;
			SetStatusBarTimer();
		}
		#endregion

		private void FillControlsWithCurrentOrder() {
			if (Win_Main.CurrentOrder.Diameter == "") {
				gridRound.Visibility = _collapsed;
				gridSquare.Visibility = _visible;
				lblOrderWidth.Content = Win_Main.CurrentOrder.Width;
				lblOrderHeight.Content = Win_Main.CurrentOrder.Height;
			}
			else {
				gridRound.Visibility = _visible;
				gridSquare.Visibility = _collapsed;
				lblOrderDiam.Content = Win_Main.CurrentOrder.Diameter;
			}
			lblOrderName.Content = Win_Main.CurrentOrder.Name;
			lblOrderThick.Content = Win_Main.CurrentOrder.Thick;
			lblOrderLength.Content = Win_Main.CurrentOrder.Length;
			orderIsLoaded = true;
		}
		private void ShowGreenLabelLoadSuccessful() {
			// DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(Timer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 3);
			timer.Start();
			lblLoadSuccess.Visibility = _visible;
		}
		private void SetCurrentOrderLayout() {
			tabOrders.SelectedItem = tabItemCurrentOrder;
			lblTitle.Content = "Ordem atual";
			btnListOrders.ClearValue(BackgroundProperty);
			btnReturn.ClearValue(BackgroundProperty);
			btnNewOrder.ClearValue(BackgroundProperty);
			btnCurrentOrder.Background = Brushes.LightRed;
		}
		private void SetOrdersListLayout() {
			tabOrders.SelectedItem = tabItemListOrders;
			lblTitle.Content = "Ordens";
			try {
				datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.Path);
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("Ficheiro das ordens não encontrado", 1);
			}
			btnListOrders.Background = Brushes.LightRed;
			btnReturn.ClearValue(BackgroundProperty);
			btnNewOrder.ClearValue(BackgroundProperty);
			btnCurrentOrder.ClearValue(BackgroundProperty);
		}
	}
}
