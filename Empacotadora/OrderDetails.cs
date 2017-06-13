using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Empacotadora {
	public class OrderDetails {
		// Properties
		public string active;
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
		public string PackageAm { get; set; }
		public string PackageType { get; set; }
		public string Weight { get; set; }
		public string Created { get; set; }
		// Methods
		public double CalculateWeight(string hei, string wid, string thick, string leng, string dens) {
			Double.TryParse(wid, out double width);
			Double.TryParse(hei, out double height);
			Double.TryParse(thick, out double thickness);
			Double.TryParse(leng, out double length);
			Double.TryParse(dens, out double density);
			double weight = (((height * width * length) - (((height - (2 * thickness)) *
							(width - (2 * thickness))) * length)) * (density * 1000) * 0.000000001);
			return weight;
		}
		public double CalculateWeight(string diam, string thick, string leng, string dens) {
			Double.TryParse(diam, out double diameter);
			Double.TryParse(thick, out double thickness);
			Double.TryParse(leng, out double length);
			Double.TryParse(dens, out double density);
			double diameterOut = diameter;
			double diameterIn = diameter - thickness;
			double weight = ((Math.PI * ((Math.Pow((0.5 * diameterOut), 2)) -
							(Math.Pow((0.5 * diameterIn), 2)))) * length * (density * 0.000001));
			return weight;
		}
		public static List<OrderDetails> ReadOrdersFromFile(string path) {
			List<OrderDetails> orders = new List<OrderDetails>();
			try {
				IEnumerable<string> linesFromFile = File.ReadLines(path);
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
								//Hardness = array[9],
								TubeAm = array[10],
								TubeType = array[11],
								PackageAm = array[12],
								PackageType = array[13],
								Weight = array[14],
								Created = array[15],
							});
						}
					}
					catch (IndexOutOfRangeException) {
						MessageBox.Show("Erro de index ao ler do ficheiro");
						continue;
					}
				}
			}
			catch (Exception exc) {
				if (exc is DirectoryNotFoundException || exc is FileNotFoundException)
					MessageBox.Show("File/Directory not found exception");
			}
			return orders;
		}
		public static void DeactivateOrder(string orderID, string path) {
			IEnumerable<string> linesFromFile = File.ReadAllLines(path);
			List<string> newFileContent = new List<string>();
			foreach (string line in linesFromFile) {
				string newline = "";
				// splits the order read from the line
				string[] array = line.Split(',');
				if (array[0] == orderID) {
					array[1] = "0";
					foreach (string value in array)
						newline += value + ",";
					// removes "," in the end of the line
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? line : newline);
			}
			try {
				File.WriteAllLines(path, newFileContent);
			}
			catch (IOException) {
				MessageBox.Show("Não foi possível escrever no ficheiro");
			}
		}
	}
}
