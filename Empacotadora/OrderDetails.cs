using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Empacotadora {
	public class OrderDetails {
		// Properties
		public string ID { get; set; }
		public string Active = "1";
		public string Name { get; set; }
		public string Diameter { get; set; }
		public string Width { get; set; }
		public string Height { get; set; }
		public string Thickness { get; set; }
		public string Length { get; set; }
		public string Density { get; set; }
		public string TubeAm { get; set; }
		public string TubeType { get; set; }
		public string PackageType { get; set; }
		public string Weight { get; set; }
		public string Created { get; set; }
		public string Straps { get; set; }
		public string StrapsPosition { get; set; }
		// Methods
		public double CalculateWeight(OrderDetails order) {
			// Returns kg/m
			bool boolDiam = double.TryParse(order.Diameter, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double diameter);
			bool boolWidth = double.TryParse(order.Width, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double width);
			bool boolHeight = double.TryParse(order.Height, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double height);
			double.TryParse(order.Thickness, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double thickness);
			double.TryParse(order.Length, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double length);
			double.TryParse(order.Density, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double density);
			double area;
			if (boolDiam && !(boolWidth && boolHeight))
				area = GetAreaOfRoundTube(diameter, thickness);
			else
				area = GetAreaOfSquareTube(width, height, thickness);
			double weight = (density / 10) * area;
			return weight;
		}
		private static double GetAreaOfSquareTube(double width, double height, double thickness) {
			double pi = Math.PI;
			double area;
			double outerRadius = 0;
			if (thickness <= 6)
				outerRadius = 2 * thickness;
			else if (thickness > 6 && thickness <= 10)
				outerRadius = 2.5 * thickness;
			else if (thickness > 10)
				outerRadius = 3 * thickness;

			double innerRadius = 0;
			if (thickness <= 6)
				innerRadius = 1 * thickness;
			else if (thickness > 6 && thickness <= 10)
				innerRadius = 1.5 * thickness;
			else if (thickness > 10)
				innerRadius = 2 * thickness;
			area = (
						(2 * thickness * (width + height - (2 * thickness))) -
						((4 - pi) * (Math.Pow(outerRadius, 2) - Math.Pow(innerRadius, 2)))
					) /
						Math.Pow(10, 2);
			return area;
		}
		private static double GetAreaOfRoundTube(double diameter, double thickness) {
			double pi = Math.PI;
			double diameterOut = diameter;
			double diameterIn = diameter - (2 * thickness);
			double area = (
								pi
								*
								(Math.Pow(diameterOut, 2) - Math.Pow(diameterIn, 2))
							) /
								(4 * Math.Pow(10, 2));
			return area;
		}
		public static ICollection<OrderDetails> ReadOrdersFromFile(string path) {
			ICollection<OrderDetails> orders = new Collection<OrderDetails>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return orders;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[1] == "1") {
						byte.TryParse(array[14], out byte strapsNmbr);
						string strapsPosition = "";
						for (int i = 15; i < 15 + strapsNmbr; i++) {
							strapsPosition += array[i] + ",";
						}
						orders.Add(new OrderDetails() {
							ID = array[0],
							Name = array[2],
							Diameter = array[3],
							Width = array[4],
							Height = array[5],
							Thickness = array[6],
							Length = array[7],
							Density = array[8],
							TubeAm = array[9],
							TubeType = array[10],
							PackageType = array[11],
							Weight = array[12],
							Created = array[13],
							Straps = array[14],
							StrapsPosition = strapsPosition
						});
					}
				}
				catch (IndexOutOfRangeException exc) {
					MessageBox.Show(exc.Message);
					return orders;
				}
			}
			return orders;
		}
		public static bool DeactivateOrder(string path, string orderID) {
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return false;
			ICollection<string> newFileContent = new Collection<string>();
			foreach (string line in linesFromFile) {
				string newline = "";
				string[] array = line.Split(',');
				if (array[0] == orderID) {
					array[1] = "0";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? line : newline);
			}
			return Document.WriteToFile(path, newFileContent.ToArray());
		}
		public static bool EditOrder(string path, string orderID, string[] valuesToWrite) {
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return false;
			ICollection<string> newFileContent = new Collection<string>();
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				string newLine = "";
				if (array[0] == orderID) {
					for (int i = 0; i <= valuesToWrite.Length - 1; i++)
						newLine += valuesToWrite[i] + ",";
					newLine = newLine.Remove(newLine.Length - 1);
				}
				newFileContent.Add(newLine == "" ? line : newLine);
			}
			if (newFileContent.ToArray() == linesFromFile.ToArray()) return false;
			return Document.WriteToFile(path, newFileContent.ToArray());
		}
		public static string[] CreateArrayFromOrder(OrderDetails order) {
			IEnumerable<string> orderList = new Collection<string> {
				order.ID, order.Active, order.Name, order.Diameter, order.Width, order.Height,
				order.Thickness, order.Length, order.Density, order.TubeAm, order.TubeType,
				order.PackageType, order.Weight, order.Created, order.Straps.ToString(), order.StrapsPosition };
			return orderList.ToArray();
		}
	}
}
