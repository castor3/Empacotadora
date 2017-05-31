using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Empacotadora {
	public class History {
		//Properties
		public string Name { get; set; }
		public string PackNmbr { get; set; }
		public string TubeAm { get; set; }
		public string Weight { get; set; }
		public string Created { get; set; }
		//Methods
		public static List<History> ReadHistoryFromFile(ref string path) {
			List<History> history = new List<History>();
			var linesFromFile = File.ReadLines(path);
			foreach (var line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					history.Add(new History() {
						Name = array[0],
						PackNmbr = array[1],
						TubeAm = array[2],
						Weight = array[3],
						Created = array[4],
					});
				}
				catch (IndexOutOfRangeException) {
					continue;
				}
			}
			return history;
		}
	}
}
