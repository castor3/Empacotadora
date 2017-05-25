using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Empacotadora {
	/// <summary>
	/// Contains PLC address (as double) of variables
	/// </summary>
	class Addresses {
		// b->bool	(1 bit)
		// i->int	(16 bits)
		// r->real	(32 bits, double)
		/// <summary>
		/// Variables used while in the program's "Storage" page
		/// </summary>
		public class Storage {
			public const int DBNumber = 495;
			// Edit order        
			public static Tuple<double, string> bModifiedData = Tuple.Create(0.0, "bool");//COMANDO: dati modificati [da PC o tracking]
			public static Tuple<double, string> bRequestOrderChange = Tuple.Create(0.1, "bool");      //COMANDO: richiesta pop-up forzatura cambio ordine
			public static Tuple<double, string> bForceOrderChange = Tuple.Create(0.2, "bool");        //COMANDO: forzatura cambio ordine
			public static Tuple<double, string> bEmptyArea = Tuple.Create(0.3, "bool");               //STATO: zona vuota, pronta a ricevere dati
			public static Tuple<double, string> bEmptyingAreaInProgress = Tuple.Create(0.4, "bool");  //STATO: svuotamento zona in corso
			// Order
			public static Tuple<double, string> iOrderCode = Tuple.Create(2.0, "integer");               //VALORE: codice ordine
			public class Order {
				// Order -> Tube data
				public static Tuple<double, string> bRoundTube = Tuple.Create(4.0, "bool");               //STATO: tubo tondo
				public static Tuple<double, string> rTubeLength = Tuple.Create(8.0, "real");              //VALORE: lunghezza tubo [mm]
				public static Tuple<double, string> rTubeWidth = Tuple.Create(12.0, "real");              //VALORE: larghezza tubo [mm]
				public static Tuple<double, string> rTubeHeight = Tuple.Create(16.0, "real");             //VALORE: altezza tubo [mm]
				public static Tuple<double, string> rTubeThickness = Tuple.Create(20.0, "real");          //VALORE: spessore tubo [mm]
				public static Tuple<double, string> rtubeWeightCalculated = Tuple.Create(24.0, "real");   //VALORE: peso tubo calcolato [Kg]
				public static Tuple<double, string> rtubeLenghtBeforeCutting = Tuple.Create(28.0, "real");//VALORE: lunghezza tubo prima del taglio [mm]
				public static Tuple<double, string> rTubeInternalVolume = Tuple.Create(32.0, "real");     //VALORE: volume interno tubo [litri]
				public static Tuple<double, string> rPercentualLineSpeed = Tuple.Create(36.0, "real");    //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				// Order -> Package data
				public static Tuple<double, string> bHexagonal = Tuple.Create(40.0, "bool");          //STATO: pacco esagono
				public static Tuple<double, string> iTubeNumber = Tuple.Create(42.0, "integer");         //VALORE: numero tubi per pacco
				public static Tuple<double, string> iProfileOutput = Tuple.Create(44.0, "integer");      //VALORE: fila uscita controsagoma
				public static Tuple<double, string> rTheoreticalWeight = Tuple.Create(46.0, "real");  //VALORE: peso teorico [Kg]
				public static Tuple<double, string> rPackageBaseWidth = Tuple.Create(50.0, "real");   //VALORE: larghezza base pacco [mm]
				public static Tuple<double, string> rBigRowWidth = Tuple.Create(54.0, "real");        //VALORE: larghezza fila massima pacco [mm]
				public static Tuple<double, string> rPackageSideWidth = Tuple.Create(58.0, "real");   //VALORE: larghezza lato pacco [mm]
				public static Tuple<double, string> rPackageHeight = Tuple.Create(62.0, "real");      //VALORE: altezza pacco [mm]
			}
			// Manual movement
			public static Tuple<double, string> bLiftingChains = Tuple.Create(86.0, "bool");          //Catene sollevabili
			public static Tuple<double, string> bDrains1_2 = Tuple.Create(86.1, "bool");              //Scoli_1_2
			public static Tuple<double, string> bDrains1_2_3 = Tuple.Create(86.2, "bool");            //Scoli_1_2_3
			public static Tuple<double, string> bDrains1_2_3_4 = Tuple.Create(86.3, "bool");          //Scoli_1_2_3_4
			public static Tuple<double, string> bStorageChains = Tuple.Create(86.4, "bool");          //Catene stoccaggio
			public static Tuple<double, string> bStorage_LiftingChains = Tuple.Create(86.5, "bool");  //Catene prelievo + catene stoccaggio marcia
			// Production data
			public static Tuple<double, string> bTubePresence = Tuple.Create(90.0, "bool");           //STATO: presenza tubi
			public static Tuple<double, string> bEndRow = Tuple.Create(90.1, "bool");                 //STATO: fine fila
			public static Tuple<double, string> bEndPackage = Tuple.Create(90.2, "bool");             //STATO: fine pacco
			public static Tuple<double, string> bLateralOutputProfile = Tuple.Create(90.3, "bool");   //STATO: uscita controsagoma laterale
			public static Tuple<double, string> bEvenRow = Tuple.Create(90.4, "bool");                //STATO: fila pari
			public static Tuple<double, string> bTubesInFirstRow = Tuple.Create(90.5, "bool");        //STATO: prima fila del pacco
			public static Tuple<double, string> iPackageNumber = Tuple.Create(92.0, "integer");          //VALORE: numero progressivo pacco
			public static Tuple<double, string> iTubesInPackage = Tuple.Create(94.0, "integer");         //VALORE: tubi su pacco
			public static Tuple<double, string> iTubesInLastRow = Tuple.Create(96.0, "integer");         //VALORE: tubi ultima fila
			public static Tuple<double, string> iFilesInPackage = Tuple.Create(98.0, "integer");         //VALORE: file su pacco
			public static Tuple<double, string> iNumberOfRailsToLift = Tuple.Create(100.0, "integer");   //VALORE: numero scoli da sollevare
			// SETUP
			public static Tuple<double, string> bEnableDrain = Tuple.Create(110.0, "bool");               //STATO: Abilitazione scoli
			public static Tuple<double, string> bEvacuateLastPackage = Tuple.Create(110.1, "bool");       //COMANDO: evacuazione ultimo pacco su scoli
			public static Tuple<double, string> iNumberOfPackagesPerGroup = Tuple.Create(112.0, "integer");  //VALORE: Numero pacchi per gruppo
			public static Tuple<double, string> rDelayToStartWeighing = Tuple.Create(116.0, "real");      //VALORE: Ritardo start pesatura
			public static Tuple<double, string> rTimeSpanBetweenPackages = Tuple.Create(120.0, "real");   //VALORE: Tempo separazione tra pacchi [s]
			public static Tuple<double, string> rTimeSpanBetweenGroups = Tuple.Create(124.0, "real");     //VALORE: Tempo separazione tra gruppi [s]
		}
	}
}
