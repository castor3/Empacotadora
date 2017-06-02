using System.Windows.Media;

namespace Empacotadora {
	class Brushes
    {
        // Brushes for controls
        public static SolidColorBrush green = new SolidColorBrush(Colors.ForestGreen);										// Green
		public static SolidColorBrush lightRed = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFCD5353"));          // red
		// Brushes for buttons
		public static SolidColorBrush active_border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF444444"));		// deep gray
        public static SolidColorBrush non_active_back = new SolidColorBrush(Colors.RosyBrown);								// light gray
        public static SolidColorBrush non_active_border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF999999"));	// light dark red
        // Brushes for shapes
        public static SolidColorBrush tomatoBrush = new SolidColorBrush(Colors.Tomato);
        public static SolidColorBrush grayBrush = new SolidColorBrush(Colors.SlateGray);
        public static SolidColorBrush blackBrush = new SolidColorBrush(Colors.Black);
		// Brush edit straps
		public static SolidColorBrush modifyStrapsBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFF0F05B"));	// Yellow
		// yellow brush for manual button background
		public static SolidColorBrush yellowBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF80"));
	}
}
