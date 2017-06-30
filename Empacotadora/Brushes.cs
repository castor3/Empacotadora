using System.Windows.Media;

namespace Empacotadora {
	class Brushes
    {
        // Brushes for controls
        public static SolidColorBrush Green = new SolidColorBrush(Colors.ForestGreen);                                      // Green
		public static SolidColorBrush IndianRed = new SolidColorBrush(Colors.IndianRed);										// red
		// Brushes for buttons
		public static SolidColorBrush ActiveBorder = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF444444"));		// deep gray
        public static SolidColorBrush NonActiveBack = new SolidColorBrush(Colors.RosyBrown);								// soft dark red
        public static SolidColorBrush NonActiveBorder = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF999999"));	// light gray
        // Brushes for shapes
        public static SolidColorBrush GrayBrush = new SolidColorBrush(Colors.LightSlateGray);
        public static SolidColorBrush BlackBrush = new SolidColorBrush(Colors.Black);
		// yellow brush for manual button background
		public static SolidColorBrush YellowBrush = new SolidColorBrush(Colors.LightYellow);
	}
}
