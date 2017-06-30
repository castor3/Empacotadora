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
using System.Collections.ObjectModel;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Orders.xaml
	/// </summary>
	public partial class Win_Orders : Window {
		Visibility _visible = Visibility.Visible;
		Visibility _collapsed = Visibility.Collapsed;

		public Win_Orders() {
			InitializeComponent();
			tabItemCurrentOrder.Visibility = _collapsed;
			tabItemListOrders.Visibility = _collapsed;
			FillControlsWithCurrentOrder();
			btnCurrentOrder.Background = Brushes.IndianRed;
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
		private void btnReturn_Click(object sender, RoutedEventArgs e) {
			General.CurrentLayout = General.Layout.Wrapper;
			Close();
		}
		private void btnNewOrder_Click(object sender, RoutedEventArgs e) {
			General.CurrentLayout = General.Layout.NewOrder;
			Close();
		}
		private void btnCurrentOrder_Click(object sender, RoutedEventArgs e) {
			SetCurrentOrderLayout();
			General.CurrentLayout = General.Layout.Wrapper;
		}
		private void btnEditCurrentOrder_Click(object sender, RoutedEventArgs e) {
			if (Win_Main.CurrentOrder == null) {
				UpdateStatusBar("Nenhuma ordem carregada", 1);
				return;
			}
			General.CurrentLayout = General.Layout.EditCurrentOrder;
			Close();
		}
		private void btnListOrders_Click(object sender, RoutedEventArgs e) {
			SetOrdersListLayout();
			General.CurrentLayout = General.Layout.Wrapper;
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
			OrderDetails.DeactivateOrder(General.OrdersPath, datagridRow.ID);
			datagridOrders.ItemsSource = null;
			datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.OrdersPath);
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
			UpdateStatusBar(Win_Main.CurrentOrder.Name + " " + Win_Main.CurrentOrder.Thickness + " " + Win_Main.CurrentOrder.Length);
			ShowGreenLabelLoadSuccessful();
		}
		private void EditOrder(OrderDetails datagridRow) {
			Win_Main.TempOrder = datagridRow;
			General.CurrentLayout = General.Layout.EditOrder;
			Close();
		}
		private void btnRecipes_Click(object sender, RoutedEventArgs e) {
			General.CurrentLayout = General.Layout.Recipes;
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
			lblOrderThick.Content = Win_Main.CurrentOrder.Thickness;
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
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnListOrders, btnReturn, btnNewOrder };
			ClearButtonBackground(buttonsToClear);
			btnCurrentOrder.Background = Brushes.IndianRed;
		}
		private void SetOrdersListLayout() {
			tabOrders.SelectedItem = tabItemListOrders;
			lblTitle.Content = "Ordens";
			datagridOrders.ItemsSource = OrderDetails.ReadOrdersFromFile(General.OrdersPath);
			btnListOrders.Background = Brushes.IndianRed;
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnReturn, btnNewOrder, btnCurrentOrder };
			ClearButtonBackground(buttonsToClear);
		}
		private void ClearButtonBackground(IEnumerable<Button> buttonsToClear) {
			foreach (Button item in buttonsToClear)
				item.ClearValue(BackgroundProperty);
		}
		private void datagridOrders_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) {
			switch (e.Column.Header.ToString()) {
				case "ID":
					e.Column.Visibility = _collapsed;
					break;
				case "Name":
					e.Column.Header = "Nome";
					break;
				case "Diameter":
					e.Column.Header = "Diâmetro";
					break;
				case "Width":
					e.Column.Header = "Largura";
					break;
				case "Height":
					e.Column.Header = "Altura";
					break;
				case "Thickness":
					e.Column.Header = "Espess.";
					break;
				case "Length":
					e.Column.Header = "Comprim.";
					break;
				case "Density":
					e.Column.Visibility = _collapsed;
					break;
				case "TubeAm":
					e.Column.Header = "Tubos";
					break;
				case "TubeType":
					e.Column.Header = "Tubos";
					break;
				case "PackageType":
					e.Column.Header = "Pacote";
					break;
				case "Weight":
					e.Column.Header = "Peso";
					break;
				case "Created":
					e.Column.Header = "Criado";
					break;
				case "Straps":
					e.Column.Header = "Cintas";
					break;
				case "StrapsPosition":
					e.Column.Visibility = _collapsed;
					break;
				default:
					break;
			}
		}
	}
}