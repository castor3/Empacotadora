using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Globalization;
using Empacotadora.Address;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace Empacotadora
{
	/// <summary>
	/// Lógica interna para Win_Main.xaml
	/// </summary>
	public partial class Win_Main : Window
	{
		// tb -> textbox / lbl -> label / btn -> button / rb -> radiobutton / sb -> statusbar
		// cal -> calendar / vb -> viewbox / lb -> listbox
		// General
		const string SaveSuccessful = "Sucesso ao gravar no ficheiro";
		const string SaveError = "Erro ao tentar gravar no ficheiro";
		FERP_MairCOMS7 PLC = new FERP_MairCOMS7();
		public static OrderDetails CurrentOrder = null;
		public static OrderDetails TempOrder = null;
		Visibility _visible = Visibility.Visible;
		Visibility _collapsed = Visibility.Collapsed;
		// Wrapper
		static int _lastTube = 0;
		public static int LastTube { get => _lastTube; }
		int _currentPackage = 0;
		// New Order
		General.ActiveTubeType _currentTubeType;
		General.ActiveWrapType _currentWrapType;
		// Strapper
		bool _textChanged = false, _cellsArePopulated = false, _editingRecipe = false, _isStrapsModifyActive = false;
		double[] _ecoStraps;
		double _ecoLength, _ecoStrapsNumber;
		// History
		General.ActiveDate _currentDate;
		// Recipe
		General.ActiveRecipe _currentRecipe;
		// PLC
		int[] PLCArrayPackageRows;
		int _tubeNumber;
		bool _changeOn, _tubeChange, _pageActive;
		struct StructTubeChange
		{
			public string OldLength;
			public string OldThickness;
			public string OldTi;
			public string OldWidth;
			public string OldHeight;
		}
		StructTubeChange _oldTube = new StructTubeChange();

		readonly int _defaultRoundTubeNmbr = 37, _defaultDiameter = 120;
		readonly int _defaultSquareTubeNmbr = 80, _defaultWidth = 60, _defaultHeight = 40;
		readonly byte _defaultStrapsNumber = 4;

		public Win_Main()
		{
			// Set initial layout
			InitializeComponent();
			ShowInitialScreen();
			InitializeLayout();
		}
		/*
		* Wrapper
		* Wrapper-Jog
		* Strapper -> verificar caixas
		* Strapper-Jog
		* Storage
		* Storage-Jog
		* New Order.-.
		* 
		*/
		#region General
		private void btnEnter_Click(object sender, RoutedEventArgs e)
		{
			ShowMainLayout();
			SetWrapperLayout();
		}
		private void lblDateTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			vbCalendar.Visibility = vbCalendar.IsVisible ? _collapsed : _visible;
		}
		private void logoCalculator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Win_Calculator wnCalculator = new Win_Calculator();
			wnCalculator.ShowDialog();
		}
		private void btnOrders_Click(object sender, RoutedEventArgs e)
		{
			Win_Orders wnOrders = new Win_Orders();
			btnOrders.Background = Brushes.IndianRed;
			btnWrapper.ClearValue(BackgroundProperty);
			wnOrders.ShowDialog();
			switch (General.CurrentLayout) {
				case General.Layout.Wrapper:
					SetWrapperLayout();
					ShowCurrentOrderOnWrapperLayout();
					ShowStrapsOnStrapperLayout();
					break;
				case General.Layout.NewOrder:
					SetNewOrderEnvironment();
					break;
				case General.Layout.EditOrder:
					SetEditOrderLayout();
					FillEditOrderLayoutWithOrder(TempOrder);
					break;
				case General.Layout.EditCurrentOrder:
					SetEditOrderLayout();
					FillEditOrderLayoutWithOrder(CurrentOrder);
					break;
				case General.Layout.Recipes:
					HideGeneralControls();
					SetRecipesLayout();
					break;
			}
		}
		private void FillEditOrderLayoutWithOrder(OrderDetails order)
		{
			if (order.TubeType == "R")
				SetRoundTube();
			else if (order.TubeType == "Q")
				SetSqrTube();
			if (order.PackageType == "H")
				SetHexaWrap();
			else if (order.PackageType == "Q")
				SetSqrWrap();
			tbNrOrdem.Text = order.Name;
			tbDiam.Text = order.Diameter;
			tbWidth.Text = order.Width;
			tbHeight.Text = order.Height;
			tbThickness.Text = order.Thickness;
			tbLength.Text = order.Length;
			tbDensity.Text = order.Density;
			tbTubeNmbr.Text = order.TubeAm;
			tbWeight.Text = order.Weight;
			lblID.Content = order.ID;
			lblID1.Content = "ID:";
		}
		private void SetNewOrderEnvironment()
		{
			if (!Document.ReadFromFile(General.OrdersPath, out IEnumerable<string> linesFromFile)) return;
			SetEditOrderLayout();
			SetSqrWrap();
			SetSqrTube();
			General.ClearTextBoxes(gridNewOrder);
			lblID1.Content = "ID (auto):";
			tbDensity.Text = "7.85";
			string[] array = linesFromFile.Last().Split(',');
			int.TryParse(array[0], out int id);
			lblID.Content = (++id).ToString();
		}
		private void btnSaveOrder_Click(object sender, RoutedEventArgs e)
		{
			OrderDetails order = GatherOrderTextBoxesValues();
			if (order == null) {
				UpdateStatusBar("Para gravar tem que preencher todos os campos");
				return;
			}
			IList<string> strapsPosition = GetStrapsPositionFromCurrentGrid();
			if (strapsPosition == null) {
				UpdateStatusBar("Campos inválidos ou vazios", 1);
				return;
			}
			if (_isStrapsModifyActive) {
				UpdateStatusBar("Tem que gravar as cintas antes de sair", 1);
				return;
			}
			string stringStraps = strapsPosition.Aggregate("", (current, item) => current + (item + ","));
			order.StrapsPosition = stringStraps.Remove(stringStraps.Count() - 1);
			order.Straps = numKeypadUpDown.Value.ToString();
			string msg = "";
			string[] arrayFromOrder;
			switch (General.CurrentLayout) {
				case General.Layout.StrapsNewOrder:
					//foreach (string item in arrayFromOrder)
					//	valuesToWrite += item;
					arrayFromOrder = OrderDetails.CreateArrayFromOrder(order);
					string valuesToWrite = arrayFromOrder.Aggregate("", (current, item) => current + (item + ","));
					valuesToWrite = valuesToWrite.Remove(valuesToWrite.Count() - 1);
					msg = Document.AppendToFile(General.OrdersPath, valuesToWrite) ? SaveSuccessful : SaveError;
					break;
				case General.Layout.StrapsEditOrder:
					arrayFromOrder = OrderDetails.CreateArrayFromOrder(order);
					msg = OrderDetails.EditOrder(General.OrdersPath, order.ID, arrayFromOrder) ?
																			SaveSuccessful : SaveError;
					break;
				case General.Layout.StrapsEditCurrentOrder:
					CurrentOrder = order;
					arrayFromOrder = OrderDetails.CreateArrayFromOrder(order);
					msg = OrderDetails.EditOrder(General.OrdersPath, CurrentOrder.ID, arrayFromOrder) ?
																			SaveSuccessful : SaveError;
					break;
				default:
					break;
			}
			UpdateStatusBar(msg);
			SetWrapperLayout();
		}
		private void btnWrapper_Click(object sender, RoutedEventArgs e)
		{
			SetWrapperLayout();
		}
		private void btnStrapper_Click(object sender, RoutedEventArgs e)
		{
			SetStrapperLayout();
			if (CurrentOrder == null)
				ClearStrapsTextBoxes();
			DisableModifyStrapsTextBoxes();
		}
		private void btnStorage_Click(object sender, RoutedEventArgs e)
		{
			SetStorageLayout();
		}
		private void btnPLCConnection_Click(object sender, RoutedEventArgs e)
		{
			ShowPLCConnectionLayout();
		}
		private void btnExit_Click(object sender, RoutedEventArgs e)
		{
			//var answer = MessageBox.Show("Terminar o programa?", "Confirmar", MessageBoxButton.YesNo);
			//if(answer == MessageBoxResult.Yes)
			Application.Current.Shutdown();
		}
		private void btnAbout_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("              Desenvolvedor: Rui Santos\n" +
							"                Supervisor: José Mendes\n" +
							"      Desenvolvido no dpt. de informática\n" +
							"       em Ferpinta, S.A. - Vale de Cambra\n" +
							"                                   2017",
							"Sobre");
		}
		private void btnReturn_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult answer;
			switch (General.CurrentLayout) {
				case General.Layout.StrapsNewOrder:
					SetEditOrderLayout();
					General.CurrentLayout = General.Layout.NewOrder;
					break;
				case General.Layout.StrapsEditOrder:
					SetEditOrderLayout();
					General.CurrentLayout = General.Layout.EditOrder;
					break;
				case General.Layout.StrapsEditCurrentOrder:
					SetEditOrderLayout();
					General.CurrentLayout = General.Layout.EditCurrentOrder;
					break;
				case General.Layout.NewOrder:
				case General.Layout.EditOrder:
				case General.Layout.EditCurrentOrder:
					answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
					if (answer == MessageBoxResult.Yes)
						SetWrapperLayout();
					break;
				case General.Layout.History:
					SetStorageLayout();
					break;
				case General.Layout.NewRecipe:
					answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
					if (answer == MessageBoxResult.Yes)
						SetRecipesLayout();
					break;
				default:
					SetWrapperLayout();
					break;
			}
		}
		private void btnManual_Click(object sender, RoutedEventArgs e)
		{
			Visibility value = borderManualWrap.Visibility == _visible ? _collapsed : _visible;
			if (value == _visible) {
				if (General.CurrentLayout == General.Layout.Wrapper)
					tabManual.SelectedItem = tabManualWrapper;
				if (General.CurrentLayout == General.Layout.Strapper)
					tabManual.SelectedItem = tabManualStrapper;
				if (General.CurrentLayout == General.Layout.Storage)
					tabManual.SelectedItem = tabManualStorage;
			}
			borderManualWrap.Visibility = value;
		}
		private void SetOneSecondTimer()
		{
			// Used in:
			// - date & time label
			// - update canvas (cnvAtado -> PLC_UpdateTubesOnPackage())
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(OneSecondTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 1);
			timer.Start();
		}
		private void OneSecondTimer_Tick(object sender, EventArgs e)
		{
			lblDateTime.Text = DateTime.Now.ToString("HH\\hmm:ss \n ddd dd/MM/yyyy"); ;
			// Controlli_di_pagina F900000_Kernel, "POLMONE_1"
			switch (General.CurrentLayout) {
				case General.Layout.Wrapper:
					//UpdateWrapperPage();
					break;
				case General.Layout.Strapper:
					//UpdateStrapperPage();
					break;
				case General.Layout.Storage:
					//UpdateStoragePage();
					break;
				case General.Layout.History:

					break;
			}
		}
		private void tb_PreviewMouseDoubleClick_Keypad(object sender, MouseButtonEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			Win_Keypad wnKeypad = new Win_Keypad();
			wnKeypad.ShowDialog();
			if (Win_Keypad.Enter == true)
				tb.Text = wnKeypad.tbResult.Text;
		}
		private void ResetTextBox(TextBox item)
		{
			item.ClearValue(BackgroundProperty);
			item.IsReadOnly = true;
			item.Focusable = false;
		}
		private void ClearButtonBackground(IEnumerable<Button> buttonsToClear)
		{
			foreach (Button item in buttonsToClear)
				item.ClearValue(BackgroundProperty);
		}
		private void DrawShape()
		{
			if (CurrentOrder != null && CurrentOrder.PackageType != null) {
				if (CurrentOrder.PackageType == "H")
					DrawHexagonalWrap(Convert.ToInt32(CurrentOrder.TubeAm), Convert.ToInt32(CurrentOrder.Diameter));
				else
					DrawSquareWrap(Convert.ToInt32(CurrentOrder.TubeAm), Convert.ToInt32(CurrentOrder.Width),
																		Convert.ToInt32(CurrentOrder.Height));
			}
			else
				DrawSquareWrap(_defaultSquareTubeNmbr, _defaultWidth, _defaultHeight);
			//DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		#region StatusBar
		// Status bar update
		private void SetStatusBarTimer()
		{
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(StatusBarTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 3, 500);
			timer.Stop();
			timer.Start();
		}
		private void StatusBarTimer_Tick(object sender, EventArgs e)
		{
			DispatcherTimer timer = (DispatcherTimer)sender;
			timer.Stop();
			sbIcon.Visibility = _collapsed;
			status.Content = "Pronto";
		}
		private void UpdateStatusBar(string msg)
		{
			status.Content = msg;
			SetStatusBarTimer();
		}
		private void UpdateStatusBar(string msg, byte error)
		{
			// pass any number (0 <-> 255) through "error" to show the error icon with the message
			status.Content = msg;
			sbIcon.Visibility = _visible;
			SetStatusBarTimer();
		}
		#endregion

		#endregion

		#region UI control methods
		// New order > define type of shape being drawn
		private void btnHexaWrap_Click(object sender, RoutedEventArgs e)
		{
			SetHexaWrap();
		}
		private void btnSqrWrap_Click(object sender, RoutedEventArgs e)
		{
			SetSqrWrap();
		}
		private void btnRoundTube_Click(object sender, RoutedEventArgs e)
		{
			SetRoundTube();
		}
		private void btnSqrTube_Click(object sender, RoutedEventArgs e)
		{
			SetSqrTube();
		}
		private void SetHexaWrap()
		{
			if (General.CurrentLayout == General.Layout.NewRecipe) {
				gridNewRecipeHexa.Visibility = _visible;
				gridNewRecipeSquare.Visibility = _collapsed;
			}
			btnHexaWrap.Background = Brushes.IndianRed;
			btnHexaWrap.BorderBrush = Brushes.ActiveBorder;
			btnSqrWrap.ClearValue(BackgroundProperty);
			btnSqrWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = General.ActiveWrapType.Hexagonal;
			if (CurrentOrder != null) {
				int.TryParse(CurrentOrder.Diameter, out int diameter);
				DrawHexagonalWrap(_defaultRoundTubeNmbr, diameter);
			}
			else
				DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		private void SetSqrWrap()
		{
			if (General.CurrentLayout == General.Layout.NewRecipe) {
				gridNewRecipeHexa.Visibility = _collapsed;
				gridNewRecipeSquare.Visibility = _visible;
			}
			btnSqrWrap.Background = Brushes.IndianRed;
			btnSqrWrap.BorderBrush = Brushes.ActiveBorder;
			btnHexaWrap.ClearValue(BackgroundProperty);
			btnHexaWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = General.ActiveWrapType.Square;
			if (CurrentOrder != null) {
				int.TryParse(CurrentOrder.Width, out int width);
				int.TryParse(CurrentOrder.Height, out int height);
				DrawSquareWrap(_defaultSquareTubeNmbr, width, height);
			}
			else
				DrawSquareWrap(_defaultSquareTubeNmbr, _defaultWidth, _defaultHeight);
		}
		private void SetRoundTube()
		{
			btnRoundTube.Background = Brushes.IndianRed;
			btnRoundTube.BorderBrush = Brushes.ActiveBorder;
			btnSqrTube.ClearValue(BackgroundProperty);
			btnSqrTube.BorderBrush = Brushes.NonActiveBorder;
			tbDiam.IsEnabled = true;
			tbDiam.Focus();
			tbHeight.IsEnabled = false;
			tbWidth.IsEnabled = false;
			rbNewRecipeRectangleShape.Visibility = _collapsed;
			rbNewRecipeSquareShape.Visibility = _collapsed;
			_currentTubeType = General.ActiveTubeType.Round;
		}
		private void SetSqrTube()
		{
			btnSqrTube.Background = Brushes.IndianRed;
			btnSqrTube.BorderBrush = Brushes.ActiveBorder;
			btnRoundTube.ClearValue(BackgroundProperty);
			btnRoundTube.BorderBrush = Brushes.NonActiveBorder;
			tbDiam.IsEnabled = false;
			tbWidth.IsEnabled = true;
			tbWidth.Focus();
			tbHeight.IsEnabled = true;
			rbNewRecipeRectangleShape.Visibility = _visible;
			rbNewRecipeSquareShape.Visibility = _visible;
			_currentTubeType = General.ActiveTubeType.Square;
		}
		// "Set" methods call "Show"/"Hide" methods to combine the desired controls on the window
		private void SetWrapperLayout()
		{
			ShowGeneralControls();
			ShowWrapperControls();
			if (CurrentOrder != null)
				ShowCurrentOrderOnWrapperLayout();
			HideEditOrderControls();
			DrawShape();
		}
		private void SetEditOrderLayout()
		{
			HideGeneralControls();
			HideWrapperControls();
			ShowEditOrderControls();
		}
		private void SetStrapperLayout()
		{
			ShowGeneralControls();
			HideWrapperControls();
			ShowStrapperControls();
			General.CurrentLayout = General.Layout.Strapper;
		}
		private void SetDefineStrapsLayout()
		{
			HideWrapperControls();
			HideGeneralControls();
			ShowStrapperControls();
			btnSaveOrder.Visibility = _visible;
			btnReturn.Visibility = _visible;
			btnSaveStraps.Visibility = _collapsed;
		}
		private void SetStorageLayout()
		{
			ShowGeneralControls();
			HideWrapperControls();
			ShowStorageControls();
		}
		private void SetHistoryLayout()
		{
			tabLayout.SelectedItem = tabItemHistory;
			FillHistoryDataGrid();
			HideGeneralControls();
			btnReturn.Visibility = _visible;
			General.CurrentLayout = General.Layout.History;
		}
		private void SetRecipesLayout()
		{
			lblTitle.Content = "Receitas";
			tabLayout.SelectedItem = tabItemRecipes;
			btnRecipeRoundTube.Background = Brushes.IndianRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			btnReturn.Visibility = _visible;
			btnManual.Visibility = _collapsed;
			btnOrders.Visibility = _collapsed;
			gridRecipesSquareTube.Visibility = _collapsed;
			gridRecipesRoundTube.Visibility = _visible;
			borderManualWrap.Visibility = _collapsed;
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathRoundTubes);
			_currentRecipe = General.ActiveRecipe.RoundTube;
			General.CurrentLayout = General.Layout.Recipes;
		}
		// "Show"/"Hide" methods show or hide layout controls
		private void ShowGeneralControls()
		{
			IEnumerable<Button> buttons = new Collection<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnExit, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _visible;
		}
		private void HideGeneralControls()
		{
			IEnumerable<Button> buttons = new Collection<Button>() {
				btnDefineStraps, btnWrapper, btnStrapper, btnStorage, btnExit, btnReturn, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _collapsed;
		}
		private void ShowWrapperControls()
		{
			lblTitle.Content = "Empacotadora";
			btnOrders.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 78);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperMain;
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnOrders, btnStrapper, btnStorage, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			btnWrapper.Background = Brushes.IndianRed;
			borderManualWrap.Visibility = _collapsed;
			gridNewRecipeDrawnShapes.Visibility = _collapsed;
			General.CurrentLayout = General.Layout.Wrapper;
		}
		private void HideWrapperControls()
		{
			btnOrders.Visibility = _collapsed;
		}
		private void ShowEditOrderControls()
		{
			btnReturn.Visibility = _visible;
			btnDefineStraps.Visibility = _visible;
			borderWrapTubeType.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 4);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperEditOrder;
			borderManualWrap.Visibility = _collapsed;
		}
		private void HideEditOrderControls()
		{
			btnReturn.Visibility = _collapsed;
			btnDefineStraps.Visibility = _collapsed;
			borderWrapTubeType.Visibility = _collapsed;
			btnSaveOrder.Visibility = _collapsed;
		}
		private void ShowStrapperControls()
		{
			lblTitle.Content = "Cintadora";
			tabLayout.SelectedItem = tabItemStrapper;
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnWrapper, btnStorage, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			if (CurrentOrder != null)
				lblPackageLength.Content = CurrentOrder.Length;
			btnStrapper.Background = Brushes.IndianRed;
			_isStrapsModifyActive = false;
			ToogleModifyStrapsTextBoxes();
			borderManualWrap.Visibility = _collapsed;
			btnSaveStraps.Visibility = _visible;
		}
		private void ShowStorageControls()
		{
			lblTitle.Content = "Armazém";
			tabLayout.SelectedItem = tabItemStorage;
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnWrapper, btnStrapper, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			btnStorage.Background = Brushes.IndianRed;
			borderManualWrap.Visibility = _collapsed;
			FillLastHistory();
			General.CurrentLayout = General.Layout.Storage;
		}
		private void ShowPLCConnectionLayout()
		{
			IEnumerable<Button> buttonsToClear = new Collection<Button>() {
				btnWrapper, btnStorage, btnStorage };
			ClearButtonBackground(buttonsToClear);
			btnPLCConnection.Background = Brushes.IndianRed;
			tabLayout.SelectedItem = tabItemPLCConnection;
		}
		// General Layout
		private void ShowInitialScreen()
		{
			IEnumerable<Button> buttons = new Collection<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _collapsed;
			borderDateTime.Visibility = _collapsed;
			status.Visibility = _collapsed;
			logoCalculator.Visibility = _collapsed;
			btnOrders.Visibility = _collapsed;
			lblTitle.Visibility = _collapsed;
			statusBar.Visibility = _collapsed;
			tabLayout.SelectedItem = tabItemInit;
		}
		private void ShowMainLayout()
		{
			IEnumerable<Button> buttons = new Collection<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _visible;
			borderDateTime.Visibility = _visible;
			status.Visibility = _visible;
			logoCalculator.Visibility = _visible;
			btnOrders.Visibility = _visible;
			lblTitle.Visibility = _visible;
			statusBar.Visibility = _visible;
		}
		private void InitializeLayout()
		{
			IEnumerable<TabItem> buttons = new Collection<TabItem>() {
				tabItemInit, tabItemWrapper,tabItemStrapper, tabItemStorage, tabItemWrapperMain,
				tabItemWrapperEditOrder, tabItemHistory, tabItemRecipes, tabItemPLCConnection,
				tabManualWrapper, tabManualStrapper,  tabManualStorage, tabItemWrapperNewRecipe };
			foreach (var item in buttons)
				item.Visibility = Visibility.Hidden;
			SetOneSecondTimer();
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
		#endregion

		#region Draw shapes in canvas
		private void DrawHexagonalWrap(int tubeAmount, double diameter)
		{
			CheckIfIsLastTubeAndIncreasesPackage(tubeAmount);
			Dictionary<string, int> recipeValues = Recipes.GetRoundTubeRecipe(tubeAmount);
			int shapeDiameter = recipeValues["shapeSize"];
			if (shapeDiameter == 0) return;
			int columns = 0, rows = 0;
			ICollection<Ellipse> listEllipses = new Collection<Ellipse>();
			General.CreateEllipseShapesToBeDrawn(tubeAmount, recipeValues, ref columns, ref rows, listEllipses);
			General.PutShapesInCanvas(listEllipses, cnvAtado);
			double packageWidth = diameter * columns;
			double packageHeight = diameter * rows;
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void DrawHexagonalWrap(int tubeAmount, double diameter, int tubeAmountBigLine, int tubeAmountSmallLine, int vPosInit, int hPosInit, int shapeSize)
		{
			Dictionary<string,int> recipeValues = new Dictionary<string, int>()
			{
				{ "bigRowSize", tubeAmountBigLine },
				{ "smallRowSize", tubeAmountSmallLine },
				{ "vPos", vPosInit },
				{ "hPos", hPosInit },
				{ "shapeSize", shapeSize }
			};
			if (shapeSize == 0) return;
			int columns = 0, rows = 0;
			ICollection<Ellipse> listEllipses = new Collection<Ellipse>();
			General.CreateEllipseShapesToBeDrawn(tubeAmount, recipeValues, ref columns, ref rows, listEllipses);
			General.PutShapesInCanvas(listEllipses, cnvAtado);

			double packageWidth = diameter * columns;
			double packageHeight = diameter * rows;
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();
		}
		private void DrawSquareWrap(int tubeAmount, int width, int height)
		{
			CheckIfIsLastTubeAndIncreasesPackage(tubeAmount);
			Dictionary<string, int> recipeValues;
			if (width == height) {
				recipeValues = Recipes.GetSquareTubeRecipe(tubeAmount);
				recipeValues["shapeWidth"] = recipeValues["shapeSize"];
				recipeValues["shapeHeight"] = recipeValues["shapeSize"];
			}
			else {
				recipeValues = Recipes.GetRectTubeRecipe(tubeAmount);
				recipeValues["shapeWidth"] = recipeValues["shapeSize"]+ (recipeValues["shapeSize"]/5);
				recipeValues["shapeHeight"] = recipeValues["shapeSize"] - (recipeValues["shapeSize"] / 5);
			}
			if (recipeValues["shapeSize"] == 0 || recipeValues["shapeSize"] == 0) {
				cnvAtado.Children.Clear();
				return;
			}
			if (recipeValues["columns"] == 0 || recipeValues["rows"] == 0) {
				Tuple<double, double> values = General.CalculateNumberOfRowsAndColummsFromTubeAmount(tubeAmount, width, height);
				recipeValues["columns"] = Convert.ToInt32(values.Item1);
				recipeValues["rows"] = Convert.ToInt32(values.Item2);
			}
			int packageWidth = width * recipeValues["columns"];
			int packageHeight = height * recipeValues["rows"];
			ICollection<Rectangle> listRectangles = new Collection<Rectangle>();
			General.CreateRectangleShapesToBeDrawn(tubeAmount, recipeValues, listRectangles);
			General.PutShapesInCanvas(listRectangles, cnvAtado);
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void DrawSquareWrap(int tubeAmount, int width, int height, int rows, int columns, int vPosInit, int hPosInit, int shapeSize)
		{
			int shapeWidth, shapeHeight;
			if (width == height) {
				shapeWidth = shapeSize;
				shapeHeight = shapeSize;
			}
			else {
				shapeWidth = shapeSize + (shapeSize / 5);
				shapeHeight = shapeSize - (shapeSize / 5);
			}
			if (shapeWidth == 0 || shapeHeight == 0) return;
			Dictionary<string, int> recipeValues = new Dictionary<string, int>()
			{
				{ "shapeWidth", shapeWidth },
				{ "shapeHeight", shapeHeight },
				{ " hPosInit" , hPosInit },
				{ " vPosInit", vPosInit },
				{ " columns", columns },
				{ " rows", rows }
			};
			ICollection<Rectangle> listRectangles = new Collection<Rectangle>();
			General.CreateRectangleShapesToBeDrawn(tubeAmount, recipeValues, listRectangles);
			General.PutShapesInCanvas(listRectangles, cnvAtado);
			int packageWidth = width * columns;
			int packageHeight = height * rows;
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void CheckIfIsLastTubeAndIncreasesPackage(int tubeAmount)
		{
			if (_lastTube == (tubeAmount + 1)) {
				_lastTube = 0;
				++_currentPackage;
			}
		}
		private void UpdateLabelsValues(int tubeAmount, double packageWidth, double packageHeight)
		{
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();

			lblCurrentTubes.Content = _lastTube.ToString();
			lblTotalTubes.Content = tubeAmount.ToString();
			lblCurrentPackage.Content = _currentPackage.ToString();
		}
		#endregion

		#region Order TextBoxe's Values
		private void tb_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			if (!tb.IsLoaded) return;
			CheckIfTextBoxeValueIsValid(sender, tb);
			OrderDetails order = GatherOrderTextBoxesValues();
			if (order == null) return;
			double weight = order.CalculateWeight(order);
			if (!int.TryParse(tbTubeNmbr.Text, out int tubes)) return;
			UpdateDrawnShapes(order, tubes);
			if (!int.TryParse(order.Length, out int length)) return;
			weight *= length / 1000;
			if (tubes > 0)
				weight *= tubes;
			tbWeight.Text = Math.Round(weight).ToString();
		}
		private void UpdateDrawnShapes(OrderDetails order, int tubes)
		{
			if (_currentTubeType == General.ActiveTubeType.Round) {
				double.TryParse(order.Diameter, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double diameter);
				DrawHexagonalWrap(tubes, diameter);
			}
			if (_currentTubeType == General.ActiveTubeType.Square) {
				int.TryParse(order.Width, out int width);
				int.TryParse(order.Height, out int height);
				DrawSquareWrap(tubes, width, height);
			}
		}
		private void CheckIfTextBoxeValueIsValid(object sender, TextBox tb)
		{
			if (tb.Text == "") return;
			if (!double.TryParse(tb.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double value)) {// Non-numeric value
				tb.Background = Brushes.NonActiveBack;
				MessageBox.Show("- Apenas são aceites números\n" +
								"- Medida não pode ser igual 0", "Valor inserido inválido");
				return;
			}
			else {
				tb.ClearValue(BackgroundProperty);
				if (value == 0.0) {
					MessageBox.Show("Medida não pode ser igual a 0");
					tb.Background = Brushes.NonActiveBack;
				}
			}
		}
		private void tb_isEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			tb.ClearValue(BackgroundProperty);
			tb.Clear();
		}
		private OrderDetails GatherOrderTextBoxesValues()
		{
			// Gathers value of Order textboxes and concatenates them into a string
			OrderDetails newOrder = new OrderDetails {
				ID = lblID.Content.ToString(),
				Active = "1",
				Name = tbNrOrdem.Text,
				Diameter = tbDiam.Text,
				Width = tbWidth.Text,
				Height = tbHeight.Text,
				Thickness = tbThickness.Text,
				Length = tbLength.Text,
				Density = tbDensity.Text,
				TubeAm = tbTubeNmbr.Text,
				TubeType = (_currentTubeType == General.ActiveTubeType.Round ? "R" : "Q"),
				PackageType = (_currentWrapType == General.ActiveWrapType.Hexagonal ? "H" : "Q"),
				Weight = tbWeight.Text,
				Created = DateTime.Now.ToString("dd/MM/yyyy HH\\hmm")
			};
			if (CheckEmptyNewOrderTextBoxes())
				return null;
			return newOrder;
		}
		private bool CheckEmptyNewOrderTextBoxes()
		{
			bool boxIsEmpty = false;
			ICollection<TextBox> textBoxes = new Collection<TextBox>() {tbNrOrdem, tbDiam, tbWidth, tbHeight,
															tbThickness,tbLength, tbTubeNmbr };
			foreach (TextBox box in textBoxes) {
				if (_currentTubeType == General.ActiveTubeType.Square && box.Name == tbDiam.Name) continue;
				if (_currentTubeType == General.ActiveTubeType.Round &&
										(box.Name == tbHeight.Name || box.Name == tbWidth.Name)) continue;
				if (box.Text == "")
					boxIsEmpty = true;
			}
			return boxIsEmpty;
		}
		#endregion

		#region Wrapper
		private void btnAddTube_Click(object sender, RoutedEventArgs e)
		{
			++_lastTube;
			DrawShape();
		}
		private void btnResetPackages_Click(object sender, RoutedEventArgs e)
		{
			_currentPackage = 0;
			lblCurrentPackage.Content = _currentPackage.ToString();
		}
		private void tbPackage_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			if (!tb.IsInitialized) return;
			if (!int.TryParse(tbPackagePerimeter.Text, out int packagePerimeter) ||
				!int.TryParse(tbPackageWeight.Text, out int packageWeight)) return;
			FillListBoxOfAllowedRopeStraps(packagePerimeter, packageWeight);
		}
		private void tbPackage_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			if (tb.Text == "") tb.Text = "0";
		}
		private void FillListBoxOfAllowedRopeStraps(int packagePerimeter, int packageWeight)
		{
			ICollection<string> ropeStraps = new Collection<string>();
			foreach (string rope in General.GetAllRopeStrapsFromFile(General.PathRopeStraps)) {
				string[] values = rope.Split(',');
				bool ropeIsValid = General.CheckIfRopeIsValid(packagePerimeter, packageWeight, values);
				if (ropeIsValid)
					ropeStraps.Add(rope);
			}
			lbAllowedRopeStraps.ItemsSource = ropeStraps;
		}
		private void ShowCurrentOrderOnWrapperLayout()
		{
			if (CurrentOrder == null) return;
			int.TryParse(CurrentOrder.TubeAm, out int amount);
			if (CurrentOrder.Diameter == "") {
				gridRound.Visibility = _collapsed;
				gridSquare.Visibility = _visible;
				lblOrderWidth.Content = CurrentOrder.Width;
				lblOrderHeight.Content = CurrentOrder.Height;
			}
			else {
				gridRound.Visibility = _visible;
				gridSquare.Visibility = _collapsed;
				lblOrderDiam.Content = CurrentOrder.Diameter;
			}
			lblOrderName.Content = CurrentOrder.Name;
			lblOrderThick.Content = CurrentOrder.Thickness;
			lblOrderLength.Content = CurrentOrder.Length;
			lblPackageLength.Content = CurrentOrder.Length;
		}
		private void PLC_UpdateTubesAndPackageData()
		{
			// checks if tubes per row has changed
			_changeOn = (PLCArrayPackageRows != PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2));
			PLCArrayPackageRows = PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2);
			// check if tube data has changed
			_tubeChange = false || (_oldTube.OldHeight != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1).ToString() ||
								   _oldTube.OldWidth != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1).ToString() ||
								   _oldTube.OldThickness != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1).ToString());

			_oldTube.OldHeight = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1).ToString();
			_oldTube.OldWidth = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1).ToString();
			_oldTube.OldThickness = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1).ToString();

			// tube number in package
			lblCurrentTubes.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1);
			lblCurrentPackage.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iPackageNumber.Item1);
			lblTotalTubes.Content = PLC.ReadInt(Accumulator_1.DBNumber, Accumulator_1.Order.Package.bTubeNumber.Item1);
			/*
			da lasciare??
			bundleCounter = tools.OpenTextFile(path_Reports & "BundleNumber.txt", False)
			BundlesTotalDisplay.Caption = bundleCounter
			*/

			if (_tubeNumber != PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1) ||
				/*pageActive ||*/ _changeOn || _tubeChange) {
				//RefreshCanvas();
				//pageActive = false;
			}
		}
		#endregion

		#region Strapper
		// Number of straps, its position and modification
		private void UpdateImageAndNumberOfTextBoxes()
		{
			int.TryParse(numKeypadUpDown.Value.ToString(), out int straps);
			ICollection<Grid> boxesGrids = new Collection<Grid>() { grid2Straps, grid3Straps, grid4Straps, grid5Straps, grid6Straps };
			Grid currentGrid = null;
			// Show/Hide grid according to number of straps on the text box
			switch (straps) {
				case 2:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado2.png", UriKind.Relative));
					currentGrid = grid2Straps;
					break;
				case 3:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado3.png", UriKind.Relative));
					currentGrid = grid3Straps;
					break;
				case 4:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado4.png", UriKind.Relative));
					currentGrid = grid4Straps;
					break;
				case 5:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado5.png", UriKind.Relative));
					currentGrid = grid5Straps;
					break;
				case 6:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado6.png", UriKind.Relative));
					currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			foreach (Grid item in boxesGrids)
				item.Visibility = (item == currentGrid ? _visible : _collapsed);
		}
		private void tbStrapPosition_LostFocus(object sender, RoutedEventArgs e)
		{
			// Finds number of active textboxes (nmbr of straps)
			// and calls method passing the nmbr of straps
			if (!byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value)) return;
			switch (value) {
				case 2:
					if ((tbstrap2_1.Text == "" || tbstrap2_2.Text == "")) return;
					break;
				case 3:
					if ((tbstrap3_1.Text == "" || tbstrap3_2.Text == "" || tbstrap3_3.Text == "")) return;
					break;
				case 4:
					if ((tbstrap4_1.Text == "" || tbstrap4_2.Text == "" || tbstrap4_3.Text == "" || tbstrap4_4.Text == "")) return;
					break;
				case 5:
					if ((tbstrap5_1.Text == "" || tbstrap5_2.Text == "" || tbstrap5_3.Text == "" || tbstrap5_4.Text == "" || tbstrap5_5.Text == "")) return;
					break;
				case 6:
					if ((tbstrap6_1.Text == "" || tbstrap6_2.Text == "" || tbstrap6_3.Text == "" || tbstrap6_4.Text == "" || tbstrap6_5.Text == "" || tbstrap6_6.Text == "")) return;
					break;
				default:
					break;
			}
			GetStrapsPositionFromCurrentGrid();
		}
		private IList<string> GetStrapsPositionFromCurrentGrid()
		{
			IEnumerable<TextBox> textBoxes = GetCurrentActiveStrapsTextBoxes();
			if (textBoxes == Enumerable.Empty<TextBox>()) return new List<string>();
			IList<string> strapsValues = General.GetStrapsValuesFromTextBoxes(textBoxes).ToList();
			int temp = 0;
			foreach (var item in strapsValues) {
				if (item == "")
					return null;
				else {
					int.TryParse(item, out int value);
					if (value > temp)
						temp = value;
					else return null;
				}
			}
			return strapsValues;
		}
		private void numKeypadUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
		{
			_textChanged = true;
			if (!numKeypadUpDown.IsInitialized) return;
			UpdateImageAndNumberOfTextBoxes();
			ToogleModifyStrapsTextBoxes();
		}
		private void btnSaveStraps_Click(object sender, RoutedEventArgs e)
		{
			if (CurrentOrder == null) {
				UpdateStatusBar("Tem que carregar uma ordem antes de alterar as cintas");
				return;
			}
			if (!byte.TryParse(numKeypadUpDown.Value.ToString(), out byte straps)) return;
			if (_isStrapsModifyActive) {
				UpdateStatusBar("Tem que terminar antes de gravar", 1);
				return;
			}
			CurrentOrder.Straps = numKeypadUpDown.Value.ToString();
			IList<string> strapsPosition = GetStrapsPositionFromCurrentGrid();
			if (strapsPosition == null) return;
			string stringStraps = strapsPosition.Aggregate("", (current, item) => current + (item + ","));
			CurrentOrder.StrapsPosition = stringStraps.Remove(stringStraps.Count() - 1);
			IList<string> valuesToWrite = OrderDetails.CreateArrayFromOrder(CurrentOrder).ToList();
			UpdateStatusBar(OrderDetails.EditOrder(General.OrdersPath, CurrentOrder.ID, valuesToWrite.ToArray()) ?
																						SaveSuccessful : SaveError);
		}
		private void btnLoadStrapsDefaultValues_Click(object sender, RoutedEventArgs e)
		{
			LoadStrapsDefaultValues();
		}
		private void LoadStrapsDefaultValues()
		{
			int length = 6000;
			if (General.CurrentLayout == General.Layout.Strapper && (CurrentOrder != null) &&
				int.TryParse(CurrentOrder.Length, out length))
				UpdateStrapsTextBoxesValues(length);
			else if ((General.CurrentLayout == General.Layout.StrapsNewOrder ||
					General.CurrentLayout == General.Layout.StrapsEditOrder ||
					General.CurrentLayout == General.Layout.StrapsEditCurrentOrder) &&
					int.TryParse(tbLength.Text, out length))
				UpdateStrapsTextBoxesValues(length);
			else
				UpdateStrapsTextBoxesValues(6000);
			lblPackageLength.Content = length;
		}
		private void tb_PreviewMouseDoubleClickStrapper_Keypad(object sender, MouseButtonEventArgs e)
		{
			if (!_isStrapsModifyActive) return;
			TextBox tb = (TextBox)sender;
			Win_Keypad wnKeypad = new Win_Keypad();
			wnKeypad.ShowDialog();
			if (Win_Keypad.Enter == true)
				tb.Text = wnKeypad.tbResult.Text;
		}
		private void btnModifyStraps_Click(object sender, RoutedEventArgs e)
		{
			_isStrapsModifyActive ^= true;
			btnModifyStraps.Content = (_isStrapsModifyActive ? "Guardar" : "Modificar");
			ToogleModifyStrapsTextBoxes();
		}
		private void btnDefineStraps_Click(object sender, RoutedEventArgs e)
		{
			if (GatherOrderTextBoxesValues() == null) {
				UpdateStatusBar("Para continuar tem que preencher todos os campos");
				return;
			}
			SetDefineStrapsLayout();
			lblPackageLength.Content = tbLength.Text;
			switch (General.CurrentLayout) {
				case General.Layout.NewOrder:
					General.CurrentLayout = General.Layout.StrapsNewOrder;
					LoadStrapsDefaultValues();
					break;
				case General.Layout.EditOrder:
					General.CurrentLayout = General.Layout.StrapsEditOrder;
					FillStrapsTextBoxesWithOrderValues(TempOrder);
					break;
				case General.Layout.EditCurrentOrder:
					General.CurrentLayout = General.Layout.StrapsEditCurrentOrder;
					FillStrapsTextBoxesWithOrderValues(CurrentOrder);
					break;
				default:
					UpdateStatusBar("Estado inválido", 1);
					return;
			}
		}
		private void FillStrapsTextBoxesWithOrderValues(OrderDetails order)
		{
			numKeypadUpDown.Value = int.Parse(order.Straps);
			IEnumerable<TextBox> textBoxes = GetCurrentActiveStrapsTextBoxes();
			string[] array = order.StrapsPosition.Split(',');
			byte i = 0;
			foreach (TextBox box in textBoxes) {
				box.Text = array[i];
				++i;
			}
		}
		private void ToogleModifyStrapsTextBoxes()
		{
			// Changes "strap position" textboxes (activate or deactivate modification)
			// according to current state of program
			if (General.CurrentLayout == General.Layout.Strapper ||
				General.CurrentLayout == General.Layout.StrapsNewOrder ||
				General.CurrentLayout == General.Layout.StrapsEditOrder ||
				General.CurrentLayout == General.Layout.StrapsEditCurrentOrder) {
				foreach (TextBox item in GetCurrentActiveStrapsTextBoxes()) {
					if (_isStrapsModifyActive)
						General.SetTextBoxForEdit(item);
					else {
						ResetTextBox(item);
						if (_textChanged)
							item.Text = "";
					}
				}
			}
			_textChanged = false;
		}
		private void DisableModifyStrapsTextBoxes()
		{
			if (General.CurrentLayout != General.Layout.Strapper) return;
			foreach (TextBox item in GetCurrentActiveStrapsTextBoxes())
				ResetTextBox(item);
		}
		private IEnumerable<TextBox> GetCurrentActiveStrapsTextBoxes()
		{
			if (!byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value)) value = _defaultStrapsNumber;
			IEnumerable<TextBox> controlsCollection = null;
			Grid currentGrid = null;
			switch (value) {
				case 2:
					currentGrid = grid2Straps;
					break;
				case 3:
					currentGrid = grid3Straps;
					break;
				case 4:
					currentGrid = grid4Straps;
					break;
				case 5:
					currentGrid = grid5Straps;
					break;
				case 6:
					currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			return controlsCollection = General.GetTFromControl<TextBox>(currentGrid);
		}
		private void UpdateStrapsTextBoxesValues(int length)
		{
			if (General.CurrentLayout != General.Layout.Strapper &&
				General.CurrentLayout != General.Layout.StrapsNewOrder &&
				General.CurrentLayout != General.Layout.StrapsEditOrder &&
				General.CurrentLayout != General.Layout.StrapsEditCurrentOrder) return;
			if (!byte.TryParse(numKeypadUpDown.Value.ToString(), out byte nmbr))
				nmbr = _defaultStrapsNumber;
			IEnumerable<TextBox> controlsCollection = Enumerable.Empty<TextBox>();
			Grid currentGrid = null;
			switch (nmbr) {
				case 2:
					if (grid2Straps.Visibility == _visible) currentGrid = grid2Straps;
					break;
				case 3:
					if (grid3Straps.Visibility == _visible) currentGrid = grid3Straps;
					break;
				case 4:
					if (grid4Straps.Visibility == _visible) currentGrid = grid4Straps;
					break;
				case 5:
					if (grid5Straps.Visibility == _visible) currentGrid = grid5Straps;
					break;
				case 6:
					if (grid6Straps.Visibility == _visible) currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			if (currentGrid == null) return;
			controlsCollection = General.GetTFromControl<TextBox>(currentGrid);
			if (controlsCollection == Enumerable.Empty<TextBox>()) return;
			int[] values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
			byte i = 0;
			foreach (TextBox item in controlsCollection) {
				item.Text = values[i].ToString();
				++i;
			}
		}
		private void UpdateStrapperPage()
		{
			// From old program -> update_controlli_standard()
			double oldWidth = new double();
			double oldHeight = new double();
			double oldThickness = new double();
			bool oldTubeType = new bool(), toChange;

			if (oldWidth != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1) ||
				oldHeight != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1) ||
				oldThickness != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1) ||
				oldTubeType != Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube))) {
				oldWidth = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1);
				oldHeight = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1);
				oldThickness = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1);
				oldTubeType = Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube));
			}
			lblPackPosition.Content = PLC.ReadReal(LateralConveyor.DBNumber, LateralConveyor.PCData.rPackagePositionInStrapper.Item1);
			numKeypadUpDown.Value = Convert.ToDouble(PLC.ReadInt(Strapper.DBNumber, Strapper.Strap.iNumberOfStraps.Item1));

			toChange = ((_ecoStraps != PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2)) ||
						(_ecoLength != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1)) ||
						(_ecoStrapsNumber != PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1)));

			_ecoStraps = PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2);
			_ecoLength = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1);
			_ecoStrapsNumber = PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1);

			lblCurrentStrap.Content = _ecoStrapsNumber.ToString();
			if (toChange && _ecoStraps.Length > 0) {
				// update package state in Strapping -> "aggiorna stato pacco in reggiatura"
			}
		}
		private void ClearStrapsTextBoxes()
		{
			IEnumerable<TextBox> textBoxes = GetCurrentActiveStrapsTextBoxes();
			foreach (TextBox box in textBoxes)
				box.Text = "";
			numKeypadUpDown.Value = 4;
		}
		private void ShowStrapsOnStrapperLayout()
		{
			if (CurrentOrder == null) return;
			if (!int.TryParse(CurrentOrder.Straps, out int strapsNmbr)) return;
			numKeypadUpDown.Value = strapsNmbr;
			FillStrapsTextBoxesWithOrderValues(CurrentOrder);
		}
		#endregion

		#region Storage
		// Storage
		private void btnEvacuatePackage_Click(object sender, RoutedEventArgs e)
		{
			PLC.WriteBool(Storage.DBNumber, Storage.Setup.bEvacuateLastPackage, true);
		}
		private void FillLastHistory()
		{
			IList<History> history = History.ReadHistoryFromFile(General.HistoryPath);
			if (history == null) return;
			IList<Label> weightLabels = new List<Label>() { lblWeight1, lblWeight2, lblWeight3 };
			try {
				lblTubesHistory.Content = history[(history.Count) - 1].TubeAm;
				for (byte i = 1; i <= 3; i++)
					weightLabels[i - 1].Content = history[(history.Count) - i].Weight;
				IList<Label> dateLabels = new List<Label>() { lblDate1, lblDate2, lblDate3 };
				for (byte i = 1; i <= 3; i++)
					dateLabels[i - 1].Content = history[(history.Count) - i].Created;
			}
			catch (ArgumentOutOfRangeException) { }
		}
		private void UpdateStoragePage()
		{
			FillLastHistory();
			lblPackageHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iTubesPresent.Item1);
			lblTubesHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iProgressiveNumber.Item1);
			UpdateWeightLabel();
			UpdateDrainLabel();
		}
		private void UpdateWeightLabel()
		{
			if (/*PAR.ReadBool(setup) &&*/ PLC.ReadBool(PCPLC.DBNumber, PCPLC.Weight.bInsertedWeight)) {
				lblWeightHistory.Content = Convert.ToDouble(PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1)) == -9999 ? "BAD" :
											PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1).ToString();
			}
			else
				lblWeightHistory.Content = "OFF";
		}
		private void UpdateDrainLabel()
		{
			if (PLC.ReadBool(Storage.DBNumber, Storage.Setup.bEnableDrain)) {
				lblDrain.Content = "ON";
				lblDrain.Foreground = Brushes.Green;
			}
			else {
				lblDrain.Content = "OFF";
				lblDrain.Foreground = Brushes.IndianRed;
			}
		}
		// History
		private void btnHistory_Click(object sender, RoutedEventArgs e)
		{
			SetHistoryLayout();
			tbHistoryDayInit.Text = DateTime.Now.ToString("dd");
			tbHistoryMonthInit.Text = DateTime.Now.ToString("MM");
			tbHistoryYearInit.Text = DateTime.Now.ToString("yyyy");
			rbNoFilter.IsChecked = true;
			comboboxShift.SelectedIndex = 0;
			calHistory.SelectedDate = DateTime.Today;
		}
		private void calHistory_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
		{
			if ((bool)rbSelectedDate.IsChecked) {
				tbHistoryDayInit.Text = calHistory.SelectedDate.Value.Day.ToString();
				tbHistoryMonthInit.Text = calHistory.SelectedDate.Value.Month.ToString();
				tbHistoryYearInit.Text = calHistory.SelectedDate.Value.Year.ToString();
			}
			else if ((bool)rbInitialFinal.IsChecked) {
				if (_currentDate == General.ActiveDate.Initial) {
					tbHistoryDayInit.Text = calHistory.SelectedDate.Value.Day.ToString();
					tbHistoryMonthInit.Text = calHistory.SelectedDate.Value.Month.ToString();
					tbHistoryYearInit.Text = calHistory.SelectedDate.Value.Year.ToString();
				}
				else {
					tbHistoryDayEnd.Text = calHistory.SelectedDate.Value.Day.ToString();
					tbHistoryMonthEnd.Text = calHistory.SelectedDate.Value.Month.ToString();
					tbHistoryYearEnd.Text = calHistory.SelectedDate.Value.Year.ToString();
				}
			}
			FillHistoryDataGrid();
		}
		private void comboboxShift_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			FillHistoryDataGrid();
		}
		private void rbNoFilter_Checked(object sender, RoutedEventArgs e)
		{
			lblInitialDate.Content = "Data";
			tbHistoryDayInit.IsEnabled = false;
			tbHistoryMonthInit.IsEnabled = false;
			tbHistoryYearInit.IsEnabled = false;
			gridFinalDate.Visibility = _collapsed;
			comboboxShift.SelectedIndex = 0;
			comboboxShift.IsEnabled = false;
			FillHistoryDataGrid();
		}
		private void rbSelectedDate_Checked(object sender, RoutedEventArgs e)
		{
			lblInitialDate.Content = "Data";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			gridFinalDate.Visibility = _collapsed;
			comboboxShift.IsEnabled = true;
			FillHistoryDataGrid();
		}
		private void rbInitialFinal_Checked(object sender, RoutedEventArgs e)
		{
			lblInitialDate.Content = "Data inicial";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			comboboxShift.IsEnabled = true;
			gridFinalDate.Visibility = _visible;
		}
		private void InitialDate_GotFocus(object sender, RoutedEventArgs e)
		{
			_currentDate = General.ActiveDate.Initial;
		}
		private void FinalDate_GotFocus(object sender, RoutedEventArgs e)
		{
			_currentDate = General.ActiveDate.End;
		}
		private void FillHistoryDataGrid()
		{
			if ((bool)rbNoFilter.IsChecked)
				datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath);
			else if ((bool)rbSelectedDate.IsChecked) {
				try {
					if (comboboxShift.SelectedIndex == 0)
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, calHistory.SelectedDate.Value.Date);
					else
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, calHistory.SelectedDate.Value.Date, calHistory.SelectedDate.Value.Date, (byte)comboboxShift.SelectedIndex);
				}
				catch (InvalidOperationException) { }
			}
			else if ((bool)rbInitialFinal.IsChecked) {
				string sInitDate = tbHistoryDayInit.Text + " " + tbHistoryMonthInit.Text + " " + tbHistoryYearInit.Text;
				string sEndDate = tbHistoryDayEnd.Text + " " + tbHistoryMonthEnd.Text + " " + tbHistoryYearEnd.Text;
				DateTime.TryParse(sInitDate, out DateTime initialDate);
				if (!DateTime.TryParse(sEndDate, out DateTime endDate)) return;
				if (comboboxShift.SelectedIndex == 0)
					datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, initialDate, endDate);
				else
					datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, initialDate, endDate, (byte)comboboxShift.SelectedIndex);
			}
		}
		#endregion

		#region Recipes
		// Recipes
		private void btnRecipeRoundTube_Click(object sender, RoutedEventArgs e)
		{
			ShowTubeRecipesOnDataGrid(General.PathRoundTubes);
			General.ClearTextBoxes(gridRecipes);
			_currentRecipe = General.ActiveRecipe.RoundTube;
		}
		private void btnRecipeSquareTube_Click(object sender, RoutedEventArgs e)
		{
			ShowTubeRecipesOnDataGrid(General.PathSquareTubes, General.PathRectTubes);
			General.ClearTextBoxes(gridRecipes);
			_currentRecipe = General.ActiveRecipe.SquareTube;
		}
		private void ShowTubeRecipesOnDataGrid(string pathRoundTube)
		{
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTube);
			btnRecipeRoundTube.Background = Brushes.IndianRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			gridRecipesSquareTube.Visibility = _collapsed;
			gridRecipesRoundTube.Visibility = _visible;
		}
		private void ShowTubeRecipesOnDataGrid(string pathSquareTube, string pathRectTube)
		{
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathSquareTube, pathRectTube);
			btnRecipeSquareTube.Background = Brushes.IndianRed;
			btnRecipeRoundTube.ClearValue(BackgroundProperty);
			gridRecipesRoundTube.Visibility = _collapsed;
			gridRecipesSquareTube.Visibility = _visible;
		}
		private void datagridRecipes_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			if (!_editingRecipe)
				_cellsArePopulated = GetDataFromSelectedCells();
		}
		private void btnRecipeEdit_Click(object sender, RoutedEventArgs e)
		{
			if (_cellsArePopulated) {
				_editingRecipe = true;
				foreach (TextBox item in General.GetTFromControl<TextBox>(gridRecipes))
					General.SetTextBoxForEdit(item);
				DisableRecipeUIButtons();
			}
			else
				UpdateStatusBar("Para editar selecione uma ordem", 1);
		}
		private void DisableRecipeUIButtons()
		{
			btnRecipeEdit.IsEnabled = false;
			btnRecipeRoundTube.IsEnabled = false;
			btnRecipeSquareTube.IsEnabled = false;
			btnReturn.IsEnabled = false;
			btnRecipeSave.Visibility = _visible;
			btnRecipeCancel.Visibility = _visible;
		}
		private void datagridRecipes_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!_editingRecipe) return;
			e.Handled = true;
			UpdateStatusBar("Para mudar de receita termine de editar a atual", 1);
		}
		private bool GetDataFromSelectedCells()
		{
			if (_currentRecipe == General.ActiveRecipe.RoundTube) {
				RoundTubeRecipe datagridRow = GetRoundTubeRecipeFromGrid();
				if (datagridRow == null) return false;
				tbRecipeTubes.Text = datagridRow.TubeNumber;
				tbRecipeBigRow.Text = datagridRow.BigRow;
				tbRecipeSmallRow.Text = datagridRow.SmallRow;
				return true;
			}
			else {
				SquareTubeRecipe datagridRow = GetSquareTubeRecipeFromGrid();
				if (datagridRow == null) return false;
				tbRecipeTubes.Text = datagridRow.TubeNumber;
				tbRecipecolumns.Text = datagridRow.Columns;
				tbRecipeRows.Text = datagridRow.Rows;
				return true;
			}
		}
		private RoundTubeRecipe GetRoundTubeRecipeFromGrid()
		{
			RoundTubeRecipe datagridRow = null;
			if (datagridRecipes.SelectedIndex < 0) return datagridRow;
			datagridRow = (RoundTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			return datagridRow;
		}
		private SquareTubeRecipe GetSquareTubeRecipeFromGrid()
		{
			SquareTubeRecipe datagridRow = null;
			if (datagridRecipes.SelectedIndex < 0) return datagridRow;
			datagridRow = (SquareTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			return datagridRow;
		}
		private void btnRecipeSave_Click(object sender, RoutedEventArgs e)
		{
			bool found = false;
			string msg = "";
			DisableTextBoxesModification();
			ICollection<string> newFileContent = new Collection<string>();
			if (_currentRecipe == General.ActiveRecipe.RoundTube) {
				General.EditRoundTubeRecipesTextFile(newFileContent, tbRecipeTubes.Text, tbRecipeBigRow.Text, tbRecipeSmallRow.Text);
				msg = Document.WriteToFile(General.PathRoundTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
				UpdateStatusBar(msg);
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathRoundTubes);
			}
			else {
				found = General.EditSquareTubeRecipesTextFile(newFileContent, General.PathSquareTubes, tbRecipeTubes.Text, tbRecipecolumns.Text, tbRecipeRows.Text);
				if (found) {
					msg = Document.WriteToFile(General.PathSquareTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				else {
					General.EditSquareTubeRecipesTextFile(newFileContent, General.PathRectTubes, tbRecipeTubes.Text, tbRecipecolumns.Text, tbRecipeRows.Text);
					msg = Document.WriteToFile(General.PathRectTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathSquareTubes, General.PathRectTubes);
			}
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void DisableTextBoxesModification()
		{
			foreach (TextBox item in General.GetTFromControl<TextBox>(gridRecipes))
				ResetTextBox(item);
		}
		private void btnAddRecipe_Click(object sender, RoutedEventArgs e)
		{
			General.CurrentLayout = General.Layout.NewRecipe;
			borderWrapTubeType.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 4);
			SetSqrTube();
			SetSqrWrap();
			rbNewRecipeSquareShape.IsChecked = true;
			gridNewRecipeSquare.Visibility = _visible;
			gridNewRecipeHexa.Visibility = _collapsed;
			btnExit.Visibility = _collapsed;
			btnReturn.Visibility = _visible;
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperNewRecipe;
			gridNewRecipeDrawnShapes.Visibility = _visible;
			lblNewRecipeDrawnShapes.Content = "";
			borderManualWrap.Visibility = _collapsed;
		}
		private void btnRecipeCancel_Click(object sender, RoutedEventArgs e)
		{
			foreach (TextBox item in General.GetTFromControl<TextBox>(gridRecipes))
				ResetTextBox(item);
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void ResetRecipeUIButtons()
		{
			btnRecipeEdit.IsEnabled = true;
			btnRecipeRoundTube.IsEnabled = true;
			btnRecipeSquareTube.IsEnabled = true;
			btnReturn.IsEnabled = true;
			btnRecipeSave.Visibility = _collapsed;
			btnRecipeCancel.Visibility = _collapsed;
		}
		private void tbNewRecipe_TextChanged(object sender, TextChangedEventArgs e)
		{
			int.TryParse(tbNewRecipeTubeNmbr.Text, out int tubeNmbr);
			int.TryParse(tbNewRecipeX.Text, out int xValue);
			int.TryParse(tbNewRecipeY.Text, out int yValue);
			byte.TryParse(tbNewRecipeScale.Text, out byte scale);
			if (_currentWrapType == General.ActiveWrapType.Square) {
				int.TryParse(tbNewRecipeColumns.Text, out int columns);
				int.TryParse(tbNewRecipeRows.Text, out int rows);
				if ((bool)rbNewRecipeSquareShape.IsChecked)
					DrawSquareWrap(tubeNmbr, _defaultWidth, _defaultWidth, rows, columns, yValue, xValue, scale);
				else if ((bool)rbNewRecipeRectangleShape.IsChecked)
					DrawSquareWrap(tubeNmbr, _defaultWidth, _defaultHeight, rows, columns, yValue, xValue, scale);
				lblNewRecipeDrawnShapes.Content = cnvAtado.Children.OfType<Rectangle>().Count().ToString();
			}
			else {
				int.TryParse(tbNewRecipeBigRow.Text, out int bigRow);
				int.TryParse(tbNewRecipeSmallRow.Text, out int smallRow);
				DrawHexagonalWrap(tubeNmbr, _defaultDiameter, bigRow, smallRow, yValue, xValue, scale);
				lblNewRecipeDrawnShapes.Content = cnvAtado.Children.OfType<Ellipse>().Count().ToString();
			}
		}
		#endregion

		#region Manual
		//Manual
		private void btnInsideManualBorder_Click(object sender, RoutedEventArgs e)
		{
			Button origin = (Button)sender;
			Image image = new Image();
			string button = origin.Name;
			bool noImageUpdate = false;
			switch (button) {
				case "btnTransportChain":
					image = (origin.Content == (Image)FindResource("TransportChain") ? (Image)FindResource("A_TransportChain") : (Image)FindResource("TransportChain"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bTransportChain);
					break;
				case "btnAlignmentRolls":
					image = (origin.Content == (Image)FindResource("AlignmentRolls") ? (Image)FindResource("A_AlignmentRolls") : (Image)FindResource("AlignmentRolls"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bAlignmentRolls);
					break;
				case "btnTrasportQueue":
					image = (origin.Content == (Image)FindResource("TrasportQueue") ? (Image)FindResource("A_TrasportQueue") : (Image)FindResource("TrasportQueue"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bTrasportQueue);
					break;
				case "btnAlignQueue":
					image = (origin.Content == (Image)FindResource("AlignQueue") ? (Image)FindResource("A_AlignQueue") : (Image)FindResource("AlignQueue"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bAlignQueue);
					break;
				case "btnLoader":
					image = (origin.Content == (Image)FindResource("Loader") ? (Image)FindResource("A_Loader") : (Image)FindResource("Loader"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLoader);
					break;
				case "btnMechanicalCounterblocks":
					image = (origin.Content == (Image)FindResource("MechanicalCounterblocks") ? (Image)FindResource("A_MechanicalCounterblocks") : (Image)FindResource("MechanicalCounterblocks"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bMechanicalCounterblocks);
					break;
				case "btnLowerPneumaticCounterblocks":
					noImageUpdate = true;
					gridLowerActive.Visibility = gridLowerActive.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLowerPneumaticCounterblocks);
					break;
				case "btnSuperiorPneumaticCounterblocks":
					noImageUpdate = true;
					gridUpperActive.Visibility = gridUpperActive.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bSuperiorPneumaticCounterblocks);
					break;
				case "btnLateralPneumaticCounterblocks":
					noImageUpdate = true;
					gridLateralActive.Visibility = gridLateralActive.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLateralPneumaticCounterblocks);
					break;
				case "btnShelves":
					image = (origin.Content == (Image)FindResource("Shelves") ? (Image)FindResource("A_Shelves") : (Image)FindResource("Shelves"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bShelves);
					break;
				case "btnCar":
					image = (origin.Content == (Image)FindResource("Car") ? (Image)FindResource("A_Car") : (Image)FindResource("Car"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCar);
					break;
				case "btnLateralTransp":
					image = (origin.Content == (Image)FindResource("LateralTransp") ? (Image)FindResource("A_LateralTransp") : (Image)FindResource("LateralTransp"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bLateralTransp);
					break;
				case "btnUpperRolls":
					noImageUpdate = true;
					if (tbUpperRolls.Background == Brushes.YellowBrush)
						tbUpperRolls.ClearValue(BackgroundProperty);
					else
						tbUpperRolls.Background = Brushes.YellowBrush;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bUpperRolls);
					break;
				case "btnCarRolls":
					image = (origin.Content == (Image)FindResource("CarRolls") ? (Image)FindResource("A_CarRolls") : (Image)FindResource("CarRolls"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls);
					break;
				case "btnCarRolls1_2":
					noImageUpdate = true;
					gridCarRolls1_2_Active.Visibility = gridCarRolls1_2_Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls12);
					break;
				case "btnCarRolls2":
					noImageUpdate = true;
					gridCarRolls2_Active.Visibility = gridCarRolls2_Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls2);
					break;
				case "btnLiftChains":
					image = (origin.Content == (Image)FindResource("LiftChains") ? (Image)FindResource("A_LiftChains") : (Image)FindResource("LiftChains"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bLiftingChains);
					break;
				case "btnStorageChains":
					image = (origin.Content == (Image)FindResource("StorageChains") ? (Image)FindResource("A_StorageChains") : (Image)FindResource("StorageChains"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bStorageChains);
					break;
				case "btnStorageChains_Withdrawal":
					image = (origin.Content == (Image)FindResource("StorageChains-Withdrawal") ? (Image)FindResource("A_StorageChains-Withdrawal") : (Image)FindResource("StorageChains-Withdrawal"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bStorage_LiftingChains);
					break;
				case "btnDrain12":
					noImageUpdate = true;
					gridDrain12Active.Visibility = gridDrain12Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2);
					break;
				case "btnDrain123":
					noImageUpdate = true;
					gridDrain123Active.Visibility = gridDrain123Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2_3);
					break;
				case "btnDrain1234":
					noImageUpdate = true;
					gridDrain1234Active.Visibility = gridDrain1234Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2_3_4);
					break;
				default:
					UpdateStatusBar("Botão não reconhecido", 1);
					break;
			}
			if (noImageUpdate == false)
				origin.Content = image;
		}

		#endregion

		#region PLC_COM
		private void btnConnect_Click(object sender, RoutedEventArgs e)
		{
			string status = PLC.Connect(tbIPAddress.Text, 2);
			UpdateStatusBar(status);
			if (!status.Contains("Successful")) return;
			lblConnectionStatus.Background = Brushes.Green;
			lblConnectionStatus.Content = "Ligado";
		}
		private void btnDisconnect_Click(object sender, RoutedEventArgs e)
		{
			string status = PLC.Disconnect();
			UpdateStatusBar(status);
			if (!status.Contains("Disconnected") && !status.Contains("Not connected")) return;
			lblConnectionStatus.Background = Brushes.IndianRed;
			lblConnectionStatus.Content = "Desligado";
		}
		private void btnWriteData_Click(object sender, RoutedEventArgs e)
		{
			double.TryParse("220.34", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test);
			PLC.WriteInt(400, 52, test);
		}
		#endregion

	}
}