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
		public static Layout ordersLayout;
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
			ordersLayout = Layout.Default;
			Close();
		}
		private void btnNewOrder_Click(object sender, RoutedEventArgs e) {
			ordersLayout = Layout.NewOrder;
			Close();
		}
		private void btnCurrentOrder_Click(object sender, RoutedEventArgs e) {
			SetCurrentOrderLayout();
			ordersLayout = Layout.Default;
		}
		private void btnListOrders_Click(object sender, RoutedEventArgs e) {
			SetOrdersListLayout();
			ordersLayout = Layout.Default;
		}
		private void btnOrdersList_Click(object sender, RoutedEventArgs e) {
			OrderDetails datagridRow = GetDataFromGrid();
			if (datagridRow == null) {
				UpdateStatusBar("Selecione uma ordem para remover", 1);
				return;
			}
			Button btn = (Button)sender;
			switch (btn.Name) {
				case "btnDeleteOrder":
					DeleteOrder(datagridRow);
					break;
				case "btnLoadOrder":
					LoadOrder(datagridRow);
					break;
				case "btnEditOrder":
					EditOrder(datagridRow);
					break;
				default:
					break;
			}
		}
		private void DeleteOrder(OrderDetails datagridRow) {
			MessageBoxResult answer = MessageBox.Show("          Tem a certeza de que pretende\n" +
											 "             remover a seguinte ordem?\n\t" +
											 "              " + datagridRow.Name, "Confirmar?", MessageBoxButton.YesNo);
			if (answer != MessageBoxResult.Yes) return;
			OrderDetails.DeactivateOrder(datagridRow.ID, General.Path);
			datagridOrders.ItemsSource = null;
			datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.Path);
		}
		private void LoadOrder(OrderDetails datagridRow) {
			MessageBoxResult answer = MessageBoxResult.None;
			if (Win_Main.CurrentOrder != null) {
				answer = MessageBox.Show("            Tem a certeza de que pretende\n" +
														  "               carregar a seguinte ordem?\n\t" +
														  "              " + Win_Main.CurrentOrder.Name +
														  "\n        A ordem em progresso vai ser terminada.", "Confirmar?", MessageBoxButton.YesNo);
			}
			else
				answer = MessageBoxResult.Yes;
			if (answer != MessageBoxResult.Yes) return;
			Win_Main.CurrentOrder = datagridRow;
			FillControlsWithCurrentOrder();

			SetCurrentOrderLayout();
			UpdateStatusBar(Win_Main.CurrentOrder.Name + " " + Win_Main.CurrentOrder.Thick + " " + Win_Main.CurrentOrder.Length);
			ShowGreenLabelLoadSuccessful();
		}
		private void EditOrder(OrderDetails datagridRow) {
			Win_Main.CurrentOrder = datagridRow;
			ordersLayout = Layout.EditOrder;
			Close();
		}
		private void btnRecipes_Click(object sender, RoutedEventArgs e) {
			ordersLayout = Layout.Recipes;
			Close();
		}
		private OrderDetails GetDataFromGrid() {
			OrderDetails datagridRow = null;
			if (datagridOrders.SelectedIndex < 0) return datagridRow;
			datagridRow = (OrderDetails)datagridOrders.Items[datagridOrders.SelectedIndex];
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
			if (Win_Main.CurrentOrder == null) return;
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
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnListOrders, btnReturn, btnNewOrder };
			ClearButtonBackground(buttonsToClear);
			btnCurrentOrder.Background = Brushes.LightRed;
		}
		private void SetOrdersListLayout() {
			tabOrders.SelectedItem = tabItemListOrders;
			lblTitle.Content = "Ordens";
			datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.Path);
			btnListOrders.Background = Brushes.LightRed;
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnReturn, btnNewOrder, btnCurrentOrder };
			ClearButtonBackground(buttonsToClear);
		}
		private void ClearButtonBackground(IEnumerable<Button> buttonsToClear) {
			foreach (Button item in buttonsToClear)
				item.ClearValue(BackgroundProperty);
		}
	}
}