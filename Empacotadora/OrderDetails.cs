using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Empacotadora {
	public class OrderDetails {
		// Properties
		public string Active;
		public string ID { get; set; }
		public string Name { get; set; }
		public string Diameter { get; set; }
		public string Width { get; set; }
		public string Height { get; set; }
		public string Thick { get; set; }
		public string Length { get; set; }
		public string Density { get; set; }
		//public string Hardness { get; set; }
		public string TubeAm { get; set; }
		public string TubeType { get; set; }
		public string PackageType { get; set; }
		public string Weight { get; set; }
		public string Created { get; set; }
		public byte Straps { get; set; }
		public int[] StrapsPosition { get; set; }
		// Methods
		public double CalculateWeight(string hei, string wid, string thick, string leng, string dens) {
			double.TryParse(wid, out double width);
			double.TryParse(hei, out double height);
			double.TryParse(thick, out double thickness);
			double.TryParse(leng, out double length);
			double.TryParse(dens, out double density);
			double weight = (((height * width * length) - (((height - (2 * thickness)) *
							(width - (2 * thickness))) * length)) * (density * 1000) * 0.000000001);
			return weight;
		}
		public double CalculateWeight(string diam, string thick, string leng, string dens) {
			double.TryParse(diam, out double diameter);
			double.TryParse(thick, out double thickness);
			double.TryParse(leng, out double length);
			double.TryParse(dens, out double density);
			double diameterOut = diameter;
			double diameterIn = diameter - thickness;
			double weight = ((Math.PI * ((Math.Pow((0.5 * diameterOut), 2)) -
							(Math.Pow((0.5 * diameterIn), 2)))) * length * (density * 0.000001));
			return weight;
		}
		public static ICollection<OrderDetails> ReadOrdersFromFile(string path) {
			ICollection<OrderDetails> orders = new ICollection<OrderDetails>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return orders;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[1] == "1") {
						orders.Add(new OrderDetails() {
							ID = array[0],
							Name = array[2],
							Diameter = array[3],
							Width = array[4],
							Height = array[5],
							Thick = array[6],
							Length = array[7],
							Density = array[8],
							TubeAm = array[10],
							TubeType = array[11],
							PackageType = array[12],
							Weight = array[13],
							Created = array[14],
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
		public static void DeactivateOrder(string orderID, string path) {
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return;
			ICollection<string> newFileContent = new ICollection<string>();
			foreach (string line in linesFromFile) {
				string newline = "";
				// splits the order read from the line
				string[] array = line.Split(',');
				if (array[0] == orderID) {
					array[1] = "0";
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					// removes "," in the end of the line
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? line : newline);
			}
			Document.WriteToFile(path, newFileContent.ToArray());
		}
	}
}
