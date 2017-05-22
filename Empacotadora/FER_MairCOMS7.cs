using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empacotadora {
	/// <summary>
	/// Contains the addresses of the variables provided by the user.
	/// Use "ValueOf()" to get or set the desired value.
	/// </summary>
	class FER_MairCOMS7 {

		/*public static Dictionary<string, object> ValueOf(string variable)
		{

		}*/
		private Dictionary<string, object> EditOrder = new Dictionary<string, object>() {
			{"bModifiedData", 0.0 },					//COMANDO: dati modificati [da PC o tracking]
			{"bRequestPop-upForcingOrderChange", 0.1 }, //COMANDO: richiesta pop-up forzatura cambio ordine
			{"bForceOrderChange", 0.2 },				//COMANDO: forzatura cambio ordine
			{"Empty area, ready to receive data", 0.3 },//STATO: zona vuota, pronta a ricevere dati
			{"bEmptyingAreaInProgress", 0.4 },			//STATO: svuotamento zona in corso
			};

		Dictionary<string, object> Order = new Dictionary<string, object>() {
			{ "iOrderCode", 2.0 },		//VALORE: codice ordine
			// Tube
			{ "bRoundTube", 4.0 },		//STATO: tubo tondo
			{ "rTubeLength", 8.0 },		//VALORE: lunghezza tubo [mm]
			{ "rTubeWidth", 12.0 },		//VALORE: larghezza tubo [mm]
			{ "rTubeHeight", 16.0 },	//VALORE: altezza tubo [mm]
			{ "rTubeThickness", 20.0 },	//VALORE: spessore tubo [mm]
			{ "rtubeWeightCalculated", 24.0 },		//VALORE: peso tubo calcolato [Kg]
			{ "rtubeLenghtBeforeCutting", 28.0 },   //VALORE: lunghezza tubo prima del taglio [mm]
			{ "rTubeInternalVolume", 32.0 },		//VALORE: volume interno tubo [litri]
			{ "rPercentualLineSpeed", 36.0 },		//VALORE: (0-1) percentuale velocità linea rispetto alla massima
			// Package
			// ...
			};

		public Dictionary<string, object> Manual = new Dictionary<string, object>() {
			// Lifting chains
			// Drains 1+2
		};

	}
}
