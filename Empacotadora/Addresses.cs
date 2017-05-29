using System;

namespace Empacotadora {
	/// <summary>
	/// Contains PLC address (as double) of variables
	/// </summary>
	class Addresses {
		// b->bool	(1 bit)
		// i->int	(16 bits)
		// r->real	(32 bits, double)
		// d->dint	(32 bits, double int)
		// a->array	(32bits * 22values) -> array of 22 "real" values
		/// <summary>
		/// Variables used to communicate between PC <valid attribute="<"/>=<valid attribute=">"/> PLC
		/// </summary>
		public class PCPLC {
			public const int DBNumber = 402;
			public class Archive {
				// Archive
				public static Tuple<double, string> bArchivingRequest = Tuple.Create(0.0, "bool");  //COMANDO: richiesta rinfresco e archiviazione dati (x PC)
				public static Tuple<double, string> bArchived = Tuple.Create(0.1, "bool");          //STATO: dati archiviati (da PC)
				public static Tuple<double, string> bPrintRequest = Tuple.Create(0.2, "bool");      //COMANDO: richiesta stampa cartellino (x PC)
				public static Tuple<double, string> bPrintExecuted = Tuple.Create(0.3, "bool");     //STATO: stampa eseguita (da PC)
				public static Tuple<double, string> iArchivingCode = Tuple.Create(2.0, "integer");  //VALORE: codice in zona archiviazione (x PC)

				public class Tube {
					// Archive -> Tube
					public static Tuple<double, string> bRound = Tuple.Create(4.0, "bool");                 //STATO: tubo tondo
					public static Tuple<double, string> rLength = Tuple.Create(8.0, "real");                //VALORE: lunghezza tubo [mm]
					public static Tuple<double, string> rWidth = Tuple.Create(12.0, "real");                //VALORE: larghezza tubo [mm]
					public static Tuple<double, string> rHeight = Tuple.Create(16.0, "real");               //VALORE: altezza tubo [mm]
					public static Tuple<double, string> rThickness = Tuple.Create(20.0, "real");            //VALORE: spessore tubo [mm]
					public static Tuple<double, string> rCalculatedWeight = Tuple.Create(24.0, "real");     //VALORE: peso tubo calcolato [Kg]
					public static Tuple<double, string> rLenghtBeforeCutting = Tuple.Create(28.0, "real");  //VALORE: lunghezza tubo entrante [mm]
					public static Tuple<double, string> rPercentualLineSpeed = Tuple.Create(36.0, "real");  //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					// Archive -> Package
					public static Tuple<double, string> iProgressiveNumber = Tuple.Create(40.0, "integer"); //VALORE: numero progressivo pacco in pesa (x PC)
					public static Tuple<double, string> iTubesPresent = Tuple.Create(42.0, "integer");      //VALORE: numero tubi presenti nel pacco (x PC)
				}
			}
			public class Weight {
				// Pesa
				public static Tuple<double, string> bInsertedWeight = Tuple.Create(52.0, "bool");       //STATO: pesa inserita (da PC)
				public static Tuple<double, string> bMC302_ZeroCentre = Tuple.Create(52.1, "bool");     //PESA MC302/E: valore del peso uguale a zero
				public static Tuple<double, string> bMC302_StableWeight = Tuple.Create(52.2, "bool");   //PESA MC302/E: valore del peso stabile
				public static Tuple<double, string> bMC302_MinimunWeight = Tuple.Create(52.3, "bool");  //PESA MC302/E: valore del peso minore o uguale a 19Kg
				public static Tuple<double, string> bMC302_TareEntered = Tuple.Create(52.4, "bool");    //PESA MC302/E: pulsante di tara premuto [ >< ]
				public static Tuple<double, string> rZeroWeightBand = Tuple.Create(54.0, "real");       //VALORE: finestra di peso a zero [kg] (da PC)
				public static Tuple<double, string> rPackageWeight = Tuple.Create(58.0, "real");        //VALORE: peso pacco (da PC)
			}
			public class PCCommands {
				// PC commands
				public static Tuple<double, string> bPCShutdownCommand = Tuple.Create(66.0, "bool");    //PULSANTE: comando spegnimento PC
				public static Tuple<double, string> bResetAlarms = Tuple.Create(66.1, "bool");          //PULSANTE: reset allarmi
				public static Tuple<double, string> bTestLamps = Tuple.Create(66.2, "bool");            //PULSANTE: test lampade
				public static Tuple<double, string> bDecrementsFIFO = Tuple.Create(66.3, "bool");       //PULSANTE: decrementa fifo
				public static Tuple<double, string> bIncrementsFIFO = Tuple.Create(66.4, "bool");       //PULSANTE: incrementa fifo
			}
		}
		/// <summary>
		/// Variables used while in the program's "..." page
		/// </summary>
		public class PackPipe {
			public const int DBNumber = 440;
			public class Mode {
				// MODO
				public static Tuple<double, string> bDelayedEmergency = Tuple.Create(0.0, "bool");  //MODO: emergenza ritardata
				public static Tuple<double, string> bAdjustment = Tuple.Create(0.1, "bool");        //MODO: regolazione
				public static Tuple<double, string> bManual = Tuple.Create(0.2, "bool");            //MODO: manuale
				public static Tuple<double, string> bSemiAutomatic = Tuple.Create(0.3, "bool");     //MODO: semiautomatico
				public static Tuple<double, string> bAutomatic = Tuple.Create(0.4, "bool");         //MODO: automatico
				public static Tuple<double, string> bSemiAut_Aut = Tuple.Create(0.5, "bool");       //MODO: semiautomatico o automatico
				public static Tuple<double, string> bMan_Auto = Tuple.Create(0.6, "bool");          //MODO: manuale, semiautomatico o automatico
				public static Tuple<double, string> bManAutReg = Tuple.Create(0.7, "bool");         //MODO: manuale, semiautomatico, automatico o regolazione
				public static Tuple<double, string> bAlarms = Tuple.Create(1.0, "bool");            //MODO: presenza allarmi
				public static Tuple<double, string> bMessages = Tuple.Create(1.1, "bool");          //MODO: presenza messaggi
				public static Tuple<double, string> bAlarmReset = Tuple.Create(1.2, "bool");        //MODO: reset allarmi
				public static Tuple<double, string> bTestSpies = Tuple.Create(1.3, "bool");         //MODO: test spie
				public static Tuple<double, string> bRequestOpenGate = Tuple.Create(1.4, "bool");   //MODO: Richiesta apertura gate
			}
			public class PCData {
				// DatiPC
				public static Tuple<double, string> rBlowingCartPosition = Tuple.Create(38.0, "real");      //Position of the blowing cart
			}
			public class PLCData {
				// DatiPLC
				public static Tuple<double, string> rFinalWidthInFormation = Tuple.Create(54.0, "real");    //VALORE: Larghezza finale fila in formazione
			}
		}
		/// <summary>
		/// Variables used while in the program's "..." page
		/// </summary>
		public class LateralConveyor {
			public const int DBNumber = 442;
			public class Mode {
				//MODO
				public static Tuple<double, string> bDelayedEmergency = Tuple.Create(0.0, "bool");  //MODO: emergenza ritardata
				public static Tuple<double, string> bAdjustment = Tuple.Create(0.1, "bool");        //MODO: regolazione
				public static Tuple<double, string> bManual = Tuple.Create(0.2, "bool");            //MODO: manuale
				public static Tuple<double, string> bSemiAutomatic = Tuple.Create(0.3, "bool");     //MODO: semiautomatico
				public static Tuple<double, string> bAutomatic = Tuple.Create(0.4, "bool");         //MODO: automatico
				public static Tuple<double, string> bSemiAut_Aut = Tuple.Create(0.5, "bool");       //MODO: semiautomatico o automatico
				public static Tuple<double, string> bMan_Auto = Tuple.Create(0.6, "bool");          //MODO: manuale, semiautomatico o automatico
				public static Tuple<double, string> bManAutReg = Tuple.Create(0.7, "bool");         //MODO: manuale, semiautomatico, automatico o regolazione
				public static Tuple<double, string> bAlarms = Tuple.Create(1.0, "bool");            //MODO: presenza allarmi
				public static Tuple<double, string> bMessages = Tuple.Create(1.1, "bool");          //MODO: presenza messaggi
				public static Tuple<double, string> bAlarmReset = Tuple.Create(1.2, "bool");        //MODO: reset allarmi
				public static Tuple<double, string> bTestSpies = Tuple.Create(1.3, "bool");         //MODO: test spie
				public static Tuple<double, string> bRequestOpenGate = Tuple.Create(1.4, "bool");   //MODO: Richiesta apertura gate
			}
			public class PCData {
				//DatiPC
				public static Tuple<double, string> iNumberOfRegimentsExecuted = Tuple.Create(36.0, "integer"); //numero di regge eseguite
				public static Tuple<double, string> rPackagePositionStrapper = Tuple.Create(38.0, "real");      //posizione pacco in reggiatura
			}
			public class PLC_TrLaterali {
				// PLC_TrLaterali
				public class Strapper {
					//Reggiatura
					public static Tuple<double, string> bConsContaAvanti = Tuple.Create(80.0, "bool");
					public static Tuple<double, string> bConsContaIndietro = Tuple.Create(80.1, "bool");
					public static Tuple<double, string> dConteggio = Tuple.Create(82.0, "dinteger");
					public static Tuple<double, string> dConteggio2 = Tuple.Create(86.0, "dinteger");
				}
				public class PackageCentering {
					//CentraggioPacco
					public static Tuple<double, string> bConsContaAvanti = Tuple.Create(90.0, "bool");
					public static Tuple<double, string> bConsContaIndietro = Tuple.Create(90.1, "bool");
					public static Tuple<double, string> dConteggio = Tuple.Create(92.0, "dinteger");
					public static Tuple<double, string> dConteggio2 = Tuple.Create(96.0, "dinteger");
				}
			}
		}
		/// <summary>
		/// Variables used while in the program's "..." page
		/// </summary>
		public class Trolley {
			public const int DBNumber = 485;
			public class OrderChange {
				//CAMBIO_ORDINE
				public static Tuple<double, string> bModifiedData = Tuple.Create(0.0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<double, string> bRequestOrderChange = Tuple.Create(0.1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<double, string> bForceOrderChange = Tuple.Create(0.2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<double, string> bEmptyArea = Tuple.Create(0.3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<double, string> bEmptyingAreaInProgress = Tuple.Create(0.4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<double, string> iOrderCode = Tuple.Create(2.0, "integer");          //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<double, string> bRound = Tuple.Create(4.0, "bool");                 //STATO: tubo tondo
					public static Tuple<double, string> rLength = Tuple.Create(8.0, "real");                //VALORE: lunghezza tubo [mm]
					public static Tuple<double, string> rWidth = Tuple.Create(12.0, "real");                //VALORE: larghezza tubo [mm]
					public static Tuple<double, string> rHeight = Tuple.Create(16.0, "real");               //VALORE: altezza tubo [mm]
					public static Tuple<double, string> rThickness = Tuple.Create(20.0, "real");            //VALORE: spessore tubo [mm]
					public static Tuple<double, string> rCalculatedWeight = Tuple.Create(24.0, "real");     //VALORE: peso tubo calcolato [Kg]
					public static Tuple<double, string> rLenghtBeforeCutting = Tuple.Create(28.0, "real");  //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<double, string> rInternalVolume = Tuple.Create(32.0, "real");       //VALORE: volume interno tubo [litri]
					public static Tuple<double, string> rPercentualLineSpeed = Tuple.Create(36.0, "real");  //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<double, string> bHexagonal = Tuple.Create(40.0, "bool");            //STATO: pacco esagono
					public static Tuple<double, string> bTubeNumber = Tuple.Create(42.0, "integer");        //VALORE: numero tubi per pacco
					public static Tuple<double, string> iProfileOutput = Tuple.Create(44.0, "integer");     //VALORE: fila uscita controsagoma
					public static Tuple<double, string> rTheoreticalWeight = Tuple.Create(46.0, "real");    //VALORE: peso teorico [Kg]
					public static Tuple<double, string> rPackageBaseWidth = Tuple.Create(50.0, "real");     //VALORE: larghezza base pacco [mm]
					public static Tuple<double, string> rBigRowWidth = Tuple.Create(54.0, "real");          //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<double, string> rPackageSideWidth = Tuple.Create(58.0, "real");     //VALORE: larghezza lato pacco [mm]
					public static Tuple<double, string> rPackageHeight = Tuple.Create(62.0, "real");        //VALORE: altezza pacco [mm]
				}
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<double, string> bTubePresence = Tuple.Create(90.0, "bool");             //STATO: presenza tubi
				public static Tuple<double, string> bEndRow = Tuple.Create(90.1, "bool");                   //STATO: fine fila
				public static Tuple<double, string> bEndPackage = Tuple.Create(90.2, "bool");               //STATO: fine pacco
				public static Tuple<double, string> bLateralOutputProfile = Tuple.Create(90.3, "bool");     //STATO: uscita controsagoma laterale
				public static Tuple<double, string> bEvenRow = Tuple.Create(90.4, "bool");                  //STATO: fila pari
				public static Tuple<double, string> bTubesInFirstRow = Tuple.Create(90.5, "bool");          //STATO: prima fila del pacco
				public static Tuple<double, string> iPackageNumber = Tuple.Create(92.0, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<double, string> iTubesInPackage = Tuple.Create(94.0, "integer");        //VALORE: tubi su pacco
				public static Tuple<double, string> iTubesInLastRow = Tuple.Create(96.0, "integer");        //VALORE: tubi ultima fila
				public static Tuple<double, string> iFilesInPackage = Tuple.Create(98.0, "integer");        //VALORE: file su pacco
			}
		}
		/// <summary>
		/// Variables used while in the program's "Strapper" page
		/// </summary>
		public class Strapper {
			public const int DBNumber = 488;
			public class OrderChange {
				// CAMBIO_ORDINE
				public static Tuple<double, string> bModifiedData = Tuple.Create(0.0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<double, string> bRequestOrderChange = Tuple.Create(0.1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<double, string> bForceOrderChange = Tuple.Create(0.2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<double, string> bEmptyArea = Tuple.Create(0.3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<double, string> bEmptyingAreaInProgress = Tuple.Create(0.4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<double, string> iOrderCode = Tuple.Create(2.0, "integer");              //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<double, string> bRoundTube = Tuple.Create(4.0, "bool");                 //STATO: tubo tondo
					public static Tuple<double, string> rTubeLength = Tuple.Create(8.0, "real");                //VALORE: lunghezza tubo [mm]
					public static Tuple<double, string> rTubeWidth = Tuple.Create(12.0, "real");                //VALORE: larghezza tubo [mm]
					public static Tuple<double, string> rTubeHeight = Tuple.Create(16.0, "real");               //VALORE: altezza tubo [mm]
					public static Tuple<double, string> rTubeThickness = Tuple.Create(20.0, "real");            //VALORE: spessore tubo [mm]
					public static Tuple<double, string> rtubeWeightCalculated = Tuple.Create(24.0, "real");     //VALORE: peso tubo calcolato [Kg]
					public static Tuple<double, string> rtubeLenghtBeforeCutting = Tuple.Create(28.0, "real");  //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<double, string> rTubeInternalVolume = Tuple.Create(32.0, "real");       //VALORE: volume interno tubo [litri]
					public static Tuple<double, string> rPercentualLineSpeed = Tuple.Create(36.0, "real");      //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<double, string> iTubeNumber = Tuple.Create(42.0, "integer");        //VALORE: numero tubi per pacco
					public static Tuple<double, string> iProfileOutput = Tuple.Create(44.0, "integer");     //VALORE: fila uscita controsagoma
					public static Tuple<double, string> rTheoreticalWeight = Tuple.Create(46.0, "real");    //VALORE: peso teorico [Kg]
					public static Tuple<double, string> rPackageBaseWidth = Tuple.Create(50.0, "real");     //VALORE: larghezza base pacco [mm]
					public static Tuple<double, string> rBigRowWidth = Tuple.Create(54.0, "real");          //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<double, string> rPackageSideWidth = Tuple.Create(58.0, "real");     //VALORE: larghezza lato pacco [mm]
					public static Tuple<double, string> rPackageHeight = Tuple.Create(62.0, "real");        //VALORE: altezza pacco [mm]
				}
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<double, string> bTubePresence = Tuple.Create(90.0, "bool");         //STATO: presenza tubi
				public static Tuple<double, string> bEndRow = Tuple.Create(90.1, "bool");               //STATO: fine fila
				public static Tuple<double, string> bEndPackage = Tuple.Create(90.2, "bool");           //STATO: fine pacco
				public static Tuple<double, string> bLateralOutputProfile = Tuple.Create(90.3, "bool"); //STATO: uscita controsagoma laterale
				public static Tuple<double, string> bEvenRow = Tuple.Create(90.4, "bool");              //STATO: fila pari
				public static Tuple<double, string> bTubesInFirstRow = Tuple.Create(90.5, "bool");      //STATO: prima fila del pacco
				public static Tuple<double, string> iPackageNumber = Tuple.Create(92.0, "integer");     //VALORE: numero progressivo pacco
				public static Tuple<double, string> iTubesInPackage = Tuple.Create(94.0, "integer");    //VALORE: tubi su pacco
				public static Tuple<double, string> iTubesInLastRow = Tuple.Create(96.0, "integer");    //VALORE: tubi ultima fila
				public static Tuple<double, string> iFilesInPackage = Tuple.Create(98.0, "integer");    //VALORE: file su pacco
			}
			public class Strap {
				// REGGIATURA
				public static Tuple<double, string> iNumberOfStraps = Tuple.Create(110.0, "integer");       //VALORE: Numero reggiature 
				public static Tuple<double, string> aStrapsPosition = Tuple.Create(112.0, "Array[real]");   //ARRAY: quote di reggiatura [mm] (ARRAY[1..22] OF REAL	)
			}
			public class Setup {
				// SETUP
				public static Tuple<double, string> bEnableTablets = Tuple.Create(200.0, "bool");                       //STATO: Reggiatura - Abilitazione tavolette (x PP frontale)
				public static Tuple<double, string> rStrainCountCoefficient1 = Tuple.Create(210.0, "real");             //VALORE: coefficente conteggio reggiatura 1 [mm/imp]
				public static Tuple<double, string> rStrainCountCoefficient2 = Tuple.Create(214.0, "real");             //VALORE: coefficente conteggio reggiatura 2 (solo previsto) [mm/imp]
				public static Tuple<double, string> rCenterPickupPacketCoefficientCount = Tuple.Create(218.0, "real");  //VALORE: coefficente conteggio centratura prelievo pacco [mm/imp]
				public static Tuple<double, string> rSlowdown = Tuple.Create(222.0, "real");                            //VALORE: Corsa per rallentamento [mm]
				public static Tuple<double, string> rPositioningTabletsOffset = Tuple.Create(226.0, "real");            //Valore: offset posizione tavolette
				public static Tuple<double, string> rStrapPositionOffset = Tuple.Create(230.0, "real");                 //VALORE: Offset quota di reggiatura [mm]
			}
		}
		/// <summary>
		/// Variables used while in the program's "Storage" page
		/// </summary>
		public class Storage {
			public const int DBNumber = 495;
			public class OrderChange {
				// Edit order        
				public static Tuple<double, string> bModifiedData = Tuple.Create(0.0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<double, string> bRequestOrderChange = Tuple.Create(0.1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<double, string> bForceOrderChange = Tuple.Create(0.2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<double, string> bEmptyArea = Tuple.Create(0.3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<double, string> bEmptyingAreaInProgress = Tuple.Create(0.4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				// Order
				public static Tuple<double, string> iOrderCode = Tuple.Create(2.0, "integer");              //VALORE: codice ordine
				public class Tube {
					// Tubo
					public static Tuple<double, string> bRoundTube = Tuple.Create(4.0, "bool");               //STATO: tubo tondo
					public static Tuple<double, string> rTubeLength = Tuple.Create(8.0, "real");              //VALORE: lunghezza tubo [mm]
					public static Tuple<double, string> rTubeWidth = Tuple.Create(12.0, "real");              //VALORE: larghezza tubo [mm]
					public static Tuple<double, string> rTubeHeight = Tuple.Create(16.0, "real");             //VALORE: altezza tubo [mm]
					public static Tuple<double, string> rTubeThickness = Tuple.Create(20.0, "real");          //VALORE: spessore tubo [mm]
					public static Tuple<double, string> rtubeWeightCalculated = Tuple.Create(24.0, "real");   //VALORE: peso tubo calcolato [Kg]
					public static Tuple<double, string> rtubeLenghtBeforeCutting = Tuple.Create(28.0, "real");//VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<double, string> rTubeInternalVolume = Tuple.Create(32.0, "real");     //VALORE: volume interno tubo [litri]
					public static Tuple<double, string> rPercentualLineSpeed = Tuple.Create(36.0, "real");    //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//Pacco
					public static Tuple<double, string> bHexagonal = Tuple.Create(40.0, "bool");            //STATO: pacco esagono
					public static Tuple<double, string> iTubeNumber = Tuple.Create(42.0, "integer");        //VALORE: numero tubi per pacco
					public static Tuple<double, string> iProfileOutput = Tuple.Create(44.0, "integer");     //VALORE: fila uscita controsagoma
					public static Tuple<double, string> rTheoreticalWeight = Tuple.Create(46.0, "real");    //VALORE: peso teorico [Kg]
					public static Tuple<double, string> rPackageBaseWidth = Tuple.Create(50.0, "real");     //VALORE: larghezza base pacco [mm]
					public static Tuple<double, string> rBigRowWidth = Tuple.Create(54.0, "real");          //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<double, string> rPackageSideWidth = Tuple.Create(58.0, "real");     //VALORE: larghezza lato pacco [mm]
					public static Tuple<double, string> rPackageHeight = Tuple.Create(62.0, "real");        //VALORE: altezza pacco [mm]
				}
			}
			//public class ManualMovement {
			//	// Manual movement
			//	public static Tuple<double, string> bLiftingChains = Tuple.Create(86.0, "bool");          //Catene sollevabili
			//	public static Tuple<double, string> bDrains1_2 = Tuple.Create(86.1, "bool");              //Scoli_1_2
			//	public static Tuple<double, string> bDrains1_2_3 = Tuple.Create(86.2, "bool");            //Scoli_1_2_3
			//	public static Tuple<double, string> bDrains1_2_3_4 = Tuple.Create(86.3, "bool");          //Scoli_1_2_3_4
			//	public static Tuple<double, string> bStorageChains = Tuple.Create(86.4, "bool");          //Catene stoccaggio
			//	public static Tuple<double, string> bStorage_LiftingChains = Tuple.Create(86.5, "bool");  //Catene prelievo + catene stoccaggio marcia
			//}
			public class ProductionData {
				// Production data
				public static Tuple<double, string> bTubePresence = Tuple.Create(90.0, "bool");           //STATO: presenza tubi
				public static Tuple<double, string> bEndRow = Tuple.Create(90.1, "bool");                 //STATO: fine fila
				public static Tuple<double, string> bEndPackage = Tuple.Create(90.2, "bool");             //STATO: fine pacco
				public static Tuple<double, string> bLateralOutputProfile = Tuple.Create(90.3, "bool");   //STATO: uscita controsagoma laterale
				public static Tuple<double, string> bEvenRow = Tuple.Create(90.4, "bool");                //STATO: fila pari
				public static Tuple<double, string> bTubesInFirstRow = Tuple.Create(90.5, "bool");        //STATO: prima fila del pacco
				public static Tuple<double, string> iPackageNumber = Tuple.Create(92.0, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<double, string> iTubesInPackage = Tuple.Create(94.0, "integer");        //VALORE: tubi su pacco
				public static Tuple<double, string> iTubesInLastRow = Tuple.Create(96.0, "integer");        //VALORE: tubi ultima fila
				public static Tuple<double, string> iFilesInPackage = Tuple.Create(98.0, "integer");        //VALORE: file su pacco
				public static Tuple<double, string> iNumberOfRailsToLift = Tuple.Create(100.0, "integer");  //VALORE: numero scoli da sollevare
			}
			public class Setup {
				// Setup
				public static Tuple<double, string> bEnableDrain = Tuple.Create(110.0, "bool");                 //STATO: Abilitazione scoli
				public static Tuple<double, string> bEvacuateLastPackage = Tuple.Create(110.1, "bool");         //COMANDO: evacuazione ultimo pacco su scoli
				public static Tuple<double, string> iNumberOfPackagesPerGroup = Tuple.Create(112.0, "integer"); //VALORE: Numero pacchi per gruppo
				public static Tuple<double, string> rDelayToStartWeighing = Tuple.Create(116.0, "real");        //VALORE: Ritardo start pesatura
				public static Tuple<double, string> rTimeSpanBetweenPackages = Tuple.Create(120.0, "real");     //VALORE: Tempo separazione tra pacchi [s]
				public static Tuple<double, string> rTimeSpanBetweenGroups = Tuple.Create(124.0, "real");       //VALORE: Tempo separazione tra gruppi [s]
			}
		}
	}
}