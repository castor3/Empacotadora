using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Empacotadora
{
    class Brushes
    {
        // Brushes for controls
        public static SolidColorBrush active_back = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFCD5353"));
        public static SolidColorBrush active_border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF444444"));
        public static SolidColorBrush non_active_back = new SolidColorBrush(Colors.RosyBrown);
        public static SolidColorBrush non_active_border = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF999999"));
        // Brushes for shapes
        public static SolidColorBrush tomatoBrush = new SolidColorBrush(Colors.Tomato);
        public static SolidColorBrush grayBrush = new SolidColorBrush(Colors.SlateGray);
        public static SolidColorBrush blackBrush = new SolidColorBrush(Colors.Black);
		// Brush edit straps
		public static SolidColorBrush modifyStrapsBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFF0F05B"));
		// "drain" text brushes
		public static SolidColorBrush drainON = (SolidColorBrush)(new BrushConverter().ConvertFrom("#00FFFFFF"));
		public static SolidColorBrush drainOFF = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFCD5353"));
		// yellow brush for manual button background
		public static SolidColorBrush yellowBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFF80"));
		
	}
}
