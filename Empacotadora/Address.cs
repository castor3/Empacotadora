using System;

namespace Empacotadora {
	/// <summary>
	/// Contains PLC address (as double) of variables
	/// </summary>
	namespace Address {
		// b->bool	(1 bit)
		// i->int	(16 bits)
		// r->real	(32 bits, double)
		// d->dint	(32 bits, double int)
		// a->array	(32bits * 22values) -> array of 22 "real" values
		/// <summary>
		/// Variables used to communicate PC -- PLC
		/// </summary>
		public class PCPLC {
			public const int DBNumber = 402;
			public class Archive {
				// Archive
				public static Tuple<int, int, string> bArchivingRequest = Tuple.Create(0, 0, "bool");   //COMANDO: richiesta rinfresco e archiviazione dati (x PC)
				public static Tuple<int, int, string> bArchived = Tuple.Create(0, 1, "bool");           //STATO: dati archiviati (da PC)
				public static Tuple<int, int, string> bPrintRequest = Tuple.Create(0, 2, "bool");       //COMANDO: richiesta stampa cartellino (x PC)
				public static Tuple<int, int, string> bPrintExecuted = Tuple.Create(0, 3, "bool");      //STATO: stampa eseguita (da PC)
				public static Tuple<int, string> iArchivingCode = Tuple.Create(2, "integer");           //VALORE: codice in zona archiviazione (x PC)

				public class Tube {
					// Archive -> Tube
					public static Tuple<int, int, string> bRound = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rCalculatedWeight = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo entrante [mm]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");   //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					// Archive -> Package
					public static Tuple<int, string> iProgressiveNumber = Tuple.Create(40, "integer");  //VALORE: numero progressivo pacco in pesa (x PC)
					public static Tuple<int, string> iTubesPresent = Tuple.Create(42, "integer");       //VALORE: numero tubi presenti nel pacco (x PC)
				}
			}
			public class Weight {
				// Pesa
				public static Tuple<int, int, string> bInsertedWeight = Tuple.Create(52, 0, "bool");        //STATO: pesa inserita (da PC)
				public static Tuple<int, int, string> bMC302_ZeroCentre = Tuple.Create(52, 1, "bool");      //PESA MC302/E: valore del peso uguale a zero
				public static Tuple<int, int, string> bMC302_StableWeight = Tuple.Create(52, 2, "bool");    //PESA MC302/E: valore del peso stabile
				public static Tuple<int, int, string> bMC302_MinimunWeight = Tuple.Create(52, 3, "bool");   //PESA MC302/E: valore del peso minore o uguale a 19Kg
				public static Tuple<int, int, string> bMC302_TareEntered = Tuple.Create(52, 4, "bool");     //PESA MC302/E: pulsante di tara premuto [ >< ]
				public static Tuple<int, string> rZeroWeightBand = Tuple.Create(54, "real");                //VALORE: finestra di peso a zero [kg] (da PC)
				public static Tuple<int, string> rPackageWeight = Tuple.Create(58, "real");                 //VALORE: peso pacco (da PC)
			}
			public class PCCommands {
				// PC commands
				public static Tuple<int, int, string> bPCShutdownCommand = Tuple.Create(66, 0, "bool");     //PULSANTE: comando spegnimento PC
				public static Tuple<int, int, string> bResetAlarms = Tuple.Create(66, 1, "bool");           //PULSANTE: reset allarmi
				public static Tuple<int, int, string> bTestLamps = Tuple.Create(66, 2, "bool");             //PULSANTE: test lampade
				public static Tuple<int, int, string> bDecrementsFIFO = Tuple.Create(66, 3, "bool");        //PULSANTE: decrementa fifo
				public static Tuple<int, int, string> bIncrementsFIFO = Tuple.Create(66, 4, "bool");        //PULSANTE: incrementa fifo
			}
		}
		public class PackPipe {
			public const int DBNumber = 440;
			public class Mode {
				// MODO
				public static Tuple<int, int, string> bDelayedEmergency = Tuple.Create(0, 0, "bool");   //MODO: emergenza ritardata
				public static Tuple<int, int, string> bAdjustment = Tuple.Create(0, 1, "bool");         //MODO: regolazione
				public static Tuple<int, int, string> bManual = Tuple.Create(0, 2, "bool");             //MODO: manuale
				public static Tuple<int, int, string> bSemiAutomatic = Tuple.Create(0, 3, "bool");      //MODO: semiautomatico
				public static Tuple<int, int, string> bAutomatic = Tuple.Create(0, 4, "bool");          //MODO: automatico
				public static Tuple<int, int, string> bSemiAut_Aut = Tuple.Create(0, 5, "bool");        //MODO: semiautomatico o automatico
				public static Tuple<int, int, string> bMan_Auto = Tuple.Create(0, 6, "bool");           //MODO: manuale, semiautomatico o automatico
				public static Tuple<int, int, string> bManAutReg = Tuple.Create(0, 7, "bool");          //MODO: manuale, semiautomatico, automatico o regolazione
				public static Tuple<int, int, string> bAlarms = Tuple.Create(1, 0, "bool");             //MODO: presenza allarmi
				public static Tuple<int, int, string> bMessages = Tuple.Create(1, 1, "bool");           //MODO: presenza messaggi
				public static Tuple<int, int, string> bAlarmReset = Tuple.Create(1, 2, "bool");         //MODO: reset allarmi
				public static Tuple<int, int, string> bTestSpies = Tuple.Create(1, 3, "bool");          //MODO: test spie
				public static Tuple<int, int, string> bRequestOpenGate = Tuple.Create(1, 4, "bool");    //MODO: Richiesta apertura gate
			}
			public class PC {
				// DatiPC
				public static Tuple<int, string> iTubesOnPackage = Tuple.Create(30, "integer");         //numero di tubi confezionati [pacco in corso]
				public static Tuple<int, string> iPackageNumber = Tuple.Create(32, "integer");              //numero pacco in formazione su PP (Numero pacchi fatti + 1)
				public static Tuple<int, string> iTubesInCurrentRow = Tuple.Create(34, "integer");      //tubi contati per fila in corso
				public static Tuple<int, string> iNumberOfCurrentRow = Tuple.Create(36, "integer");     //numero di fila in corso
				public static Tuple<int, string> rBlowingCartPosition = Tuple.Create(38, "real");       //Posizione Carrello Soffio
			}
			public class PLCData {
				// DatiPLC
				public static Tuple<int, string> rFinalWidthInFormation = Tuple.Create(54, "real");     //VALORE: Larghezza finale fila in formazione
			}
		}
		public class LateralConveyor {
			public const int DBNumber = 442;
			public class Mode {
				//MODO
				public static Tuple<int, int, string> bDelayedEmergency = Tuple.Create(0, 0, "bool");  //MODO: emergenza ritardata
				public static Tuple<int, int, string> bAdjustment = Tuple.Create(0, 1, "bool");        //MODO: regolazione
				public static Tuple<int, int, string> bManual = Tuple.Create(0, 2, "bool");            //MODO: manuale
				public static Tuple<int, int, string> bSemiAutomatic = Tuple.Create(0, 3, "bool");     //MODO: semiautomatico
				public static Tuple<int, int, string> bAutomatic = Tuple.Create(0, 4, "bool");         //MODO: automatico
				public static Tuple<int, int, string> bSemiAut_Aut = Tuple.Create(0, 5, "bool");       //MODO: semiautomatico o automatico
				public static Tuple<int, int, string> bMan_Auto = Tuple.Create(0, 6, "bool");          //MODO: manuale, semiautomatico o automatico
				public static Tuple<int, int, string> bManAutReg = Tuple.Create(0, 7, "bool");         //MODO: manuale, semiautomatico, automatico o regolazione
				public static Tuple<int, int, string> bAlarms = Tuple.Create(1, 0, "bool");            //MODO: presenza allarmi
				public static Tuple<int, int, string> bMessages = Tuple.Create(1, 1, "bool");          //MODO: presenza messaggi
				public static Tuple<int, int, string> bAlarmReset = Tuple.Create(1, 2, "bool");        //MODO: reset allarmi
				public static Tuple<int, int, string> bTestSpies = Tuple.Create(1, 3, "bool");         //MODO: test spie
				public static Tuple<int, int, string> bRequestOpenGate = Tuple.Create(1, 4, "bool");   //MODO: Richiesta apertura gate
			}
			public class PCData {
				//DatiPC
				public static Tuple<int, string> iNumberOfRegimentsExecuted = Tuple.Create(36, "integer");  //numero di regge eseguite
				public static Tuple<int, string> rPackagePositionInStrapper = Tuple.Create(38, "real");     //posizione pacco in reggiatura
			}
			public class PLC_TrLaterali {
				// PLC_TrLaterali
				public class Strapper {
					//Reggiatura
					public static Tuple<int, int, string> bConsContaAvanti = Tuple.Create(80, 0, "bool");
					public static Tuple<int, int, string> bConsContaIndietro = Tuple.Create(80, 1, "bool");
					public static Tuple<int, string> dConteggio = Tuple.Create(82, "dinteger");
					public static Tuple<int, string> dConteggio2 = Tuple.Create(86, "dinteger");
				}
				public class PackageCentering {
					//CentraggioPacco
					public static Tuple<int, int, string> bConsContaAvanti = Tuple.Create(90, 0, "bool");
					public static Tuple<int, int, string> bConsContaIndietro = Tuple.Create(90, 1, "bool");
					public static Tuple<int, string> dConteggio = Tuple.Create(92, "dinteger");
					public static Tuple<int, string> dConteggio2 = Tuple.Create(96, "dinteger");
				}
			}
		}
		public class Accumulator_1 {
			public const int DBNumber = 480;
			public class OrderChange {
				//CAMBIO_ORDINE
				public static Tuple<int, int, string> bModifiedData = Tuple.Create(0, 0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<int, int, string> bRequestOrderChange = Tuple.Create(0, 1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<int, int, string> bForceOrderChange = Tuple.Create(0, 2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<int, int, string> bEmptyArea = Tuple.Create(0, 3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<int, int, string> bEmptyingAreaInProgress = Tuple.Create(0, 4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<int, string> iOrderCode = Tuple.Create(2, "integer");               //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<int, int, string> bRound = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rCalculatedWeight = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<int, string> rInternalVolume = Tuple.Create(32, "real");        //VALORE: volume interno tubo [litri]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");   //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<int, int, string> bHexagonal = Tuple.Create(40, 0, "bool");     //STATO: pacco esagono
					public static Tuple<int, string> bTubeNumber = Tuple.Create(42, "integer");         //VALORE: numero tubi per pacco
					public static Tuple<int, string> iProfileOutput = Tuple.Create(44, "integer");      //VALORE: fila uscita controsagoma
					public static Tuple<int, string> rTheoreticalWeight = Tuple.Create(46, "real");     //VALORE: peso teorico [Kg]
					public static Tuple<int, string> rPackageBaseWidth = Tuple.Create(50, "real");      //VALORE: larghezza base pacco [mm]
					public static Tuple<int, string> rBigRowWidth = Tuple.Create(54, "real");           //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<int, string> rPackageSideWidth = Tuple.Create(58, "real");      //VALORE: larghezza lato pacco [mm]
					public static Tuple<int, string> rPackageHeight = Tuple.Create(62, "real");         //VALORE: altezza pacco [mm]
				}
			}
			public class ManualMovement {
				public static Tuple<int, int, string> bTransportChain = Tuple.Create(86, 0, "bool");                    //Trasportatore salita
				public static Tuple<int, int, string> bTubeFitting = Tuple.Create(86, 1, "bool");                       //Fermo tubo
				public static Tuple<int, int, string> bTubeOrientation = Tuple.Create(86, 2, "bool");                   //Orientatore tubi
				public static Tuple<int, int, string> bAlignmentRolls = Tuple.Create(86, 3, "bool");                    //Rulli allineamento
				public static Tuple<int, int, string> bTrasportQueue = Tuple.Create(86, 5, "bool");                     //Trasportatore file
				public static Tuple<int, int, string> bAlignQueue = Tuple.Create(86, 6, "bool");                        //Allineamento file
				public static Tuple<int, int, string> bLoader = Tuple.Create(86, 7, "bool");                            //Pale
				public static Tuple<int, int, string> bMechanicalCounterblocks = Tuple.Create(87, 0, "bool");           //Controsagome meccaniche
				public static Tuple<int, int, string> bLowerPneumaticCounterblocks = Tuple.Create(87, 1, "bool");       //Controsagome pneumatica inferiore
				public static Tuple<int, int, string> bSuperiorPneumaticCounterblocks = Tuple.Create(87, 2, "bool");    //Controsagome pneumatica superiore
				public static Tuple<int, int, string> bLateralPneumaticCounterblocks = Tuple.Create(87, 3, "bool");     //Controsagome pneumatica laterale
				public static Tuple<int, int, string> bShelves = Tuple.Create(87, 4, "bool");                           //Mensole
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<int, int, string> bTubePresence = Tuple.Create(90, 0, "bool");             //STATO: presenza tubi
				public static Tuple<int, int, string> bEndRow = Tuple.Create(90, 1, "bool");                   //STATO: fine fila
				public static Tuple<int, int, string> bEndPackage = Tuple.Create(90, 2, "bool");               //STATO: fine pacco
				public static Tuple<int, int, string> bLateralOutputProfile = Tuple.Create(90, 3, "bool");     //STATO: uscita controsagoma laterale
				public static Tuple<int, int, string> bEvenRow = Tuple.Create(90, 4, "bool");                  //STATO: fila pari
				public static Tuple<int, int, string> bTubesInFirstRow = Tuple.Create(90, 5, "bool");          //STATO: prima fila del pacco
				public static Tuple<int, string> iPackageNumber = Tuple.Create(92, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<int, string> iTubesInPackage = Tuple.Create(94, "integer");        //VALORE: tubi su pacco
				public static Tuple<int, string> iTubesInLastRow = Tuple.Create(96, "integer");        //VALORE: tubi ultima fila
				public static Tuple<int, string> iFilesInPackage = Tuple.Create(98, "integer");        //VALORE: file su pacco
			}
			public static Tuple<int, int, string> Rows = Tuple.Create(110, 308, "Array[int]");  //ARRAY: numero tubi per fila [1..100]
			public class Setup {
				public static Tuple<int, string> timeTubeAlignment = Tuple.Create(310, "real"); //VALORE: tempo allineamento tubo
				public static Tuple<int, string> shelvesAditionalDescentTimeCalculatingCoefficient = Tuple.Create(314, "real"); //VALORE: coefficiente calcolo tempo discesa supplementare mensole (shelves) [s/mm]
			}
		}
		public class Accumulator_2 {
			public const int DBNumber = 481;
			public class OrderChange {
				//CAMBIO_ORDINE
				public static Tuple<int, int, string> bModifiedData = Tuple.Create(0, 0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<int, int, string> bRequestOrderChange = Tuple.Create(0, 1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<int, int, string> bForceOrderChange = Tuple.Create(0, 2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<int, int, string> bEmptyArea = Tuple.Create(0, 3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<int, int, string> bEmptyingAreaInProgress = Tuple.Create(0, 4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<int, string> iOrderCode = Tuple.Create(2, "integer");               //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<int, int, string> bRound = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rCalculatedWeight = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<int, string> rInternalVolume = Tuple.Create(32, "real");        //VALORE: volume interno tubo [litri]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");   //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<int, int, string> bHexagonal = Tuple.Create(40, 0, "bool");     //STATO: pacco esagono
					public static Tuple<int, string> bTubeNumber = Tuple.Create(42, "integer");         //VALORE: numero tubi per pacco
					public static Tuple<int, string> iProfileOutput = Tuple.Create(44, "integer");      //VALORE: fila uscita controsagoma
					public static Tuple<int, string> rTheoreticalWeight = Tuple.Create(46, "real");     //VALORE: peso teorico [Kg]
					public static Tuple<int, string> rPackageBaseWidth = Tuple.Create(50, "real");      //VALORE: larghezza base pacco [mm]
					public static Tuple<int, string> rBigRowWidth = Tuple.Create(54, "real");           //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<int, string> rPackageSideWidth = Tuple.Create(58, "real");      //VALORE: larghezza lato pacco [mm]
					public static Tuple<int, string> rPackageHeight = Tuple.Create(62, "real");         //VALORE: altezza pacco [mm]
				}
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<int, int, string> bTubePresence = Tuple.Create(90, 0, "bool");             //STATO: presenza tubi
				public static Tuple<int, int, string> bEndRow = Tuple.Create(90, 1, "bool");                   //STATO: fine fila
				public static Tuple<int, int, string> bEndPackage = Tuple.Create(90, 2, "bool");               //STATO: fine pacco
				public static Tuple<int, int, string> bLateralOutputProfile = Tuple.Create(90, 3, "bool");     //STATO: uscita controsagoma laterale
				public static Tuple<int, int, string> bEvenRow = Tuple.Create(90, 4, "bool");                  //STATO: fila pari
				public static Tuple<int, int, string> bTubesInFirstRow = Tuple.Create(90, 5, "bool");          //STATO: prima fila del pacco
				public static Tuple<int, string> iPackageNumber = Tuple.Create(92, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<int, string> iTubesInPackage = Tuple.Create(94, "integer");        //VALORE: tubi su pacco
				public static Tuple<int, string> iTubesInLastRow = Tuple.Create(96, "integer");        //VALORE: tubi ultima fila
				public static Tuple<int, string> iFilesInPackage = Tuple.Create(98, "integer");        //VALORE: file su pacco
			}
			public class Setup { }
		}
		public class Trolley {
			public const int DBNumber = 485;
			public class OrderChange {
				//CAMBIO_ORDINE
				public static Tuple<int, int, string> bModifiedData = Tuple.Create(0, 0, "bool");              //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<int, int, string> bRequestOrderChange = Tuple.Create(0, 1, "bool");        //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<int, int, string> bForceOrderChange = Tuple.Create(0, 2, "bool");          //COMANDO: forzatura cambio ordine
				public static Tuple<int, int, string> bEmptyArea = Tuple.Create(0, 3, "bool");                 //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<int, int, string> bEmptyingAreaInProgress = Tuple.Create(0, 4, "bool");    //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<int, string> iOrderCode = Tuple.Create(2, "integer");               //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<int, int, string> bRound = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rCalculatedWeight = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<int, string> rInternalVolume = Tuple.Create(32, "real");        //VALORE: volume interno tubo [litri]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");   //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<int, int, string> bHexagonal = Tuple.Create(40, 0, "bool");     //STATO: pacco esagono
					public static Tuple<int, string> bTubeNumber = Tuple.Create(42, "integer");         //VALORE: numero tubi per pacco
					public static Tuple<int, string> iProfileOutput = Tuple.Create(44, "integer");      //VALORE: fila uscita controsagoma
					public static Tuple<int, string> rTheoreticalWeight = Tuple.Create(46, "real");     //VALORE: peso teorico [Kg]
					public static Tuple<int, string> rPackageBaseWidth = Tuple.Create(50, "real");      //VALORE: larghezza base pacco [mm]
					public static Tuple<int, string> rBigRowWidth = Tuple.Create(54, "real");           //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<int, string> rPackageSideWidth = Tuple.Create(58, "real");      //VALORE: larghezza lato pacco [mm]
					public static Tuple<int, string> rPackageHeight = Tuple.Create(62, "real");         //VALORE: altezza pacco [mm]
				}
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<int, int, string> bTubePresence = Tuple.Create(90, 0, "bool");             //STATO: presenza tubi
				public static Tuple<int, int, string> bEndRow = Tuple.Create(90, 1, "bool");                   //STATO: fine fila
				public static Tuple<int, int, string> bEndPackage = Tuple.Create(90, 2, "bool");               //STATO: fine pacco
				public static Tuple<int, int, string> bLateralOutputProfile = Tuple.Create(90, 3, "bool");     //STATO: uscita controsagoma laterale
				public static Tuple<int, int, string> bEvenRow = Tuple.Create(90, 4, "bool");                  //STATO: fila pari
				public static Tuple<int, int, string> bTubesInFirstRow = Tuple.Create(90, 5, "bool");          //STATO: prima fila del pacco
				public static Tuple<int, string> iPackageNumber = Tuple.Create(92, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<int, string> iTubesInPackage = Tuple.Create(94, "integer");        //VALORE: tubi su pacco
				public static Tuple<int, string> iTubesInLastRow = Tuple.Create(96, "integer");        //VALORE: tubi ultima fila
				public static Tuple<int, string> iFilesInPackage = Tuple.Create(98, "integer");        //VALORE: file su pacco
			}
		}
		/// <summary>
		/// Variables used while in the program's "Strapper" page
		/// </summary>
		public class Strapper {
			public const int DBNumber = 488;
			public class OrderChange {
				// CAMBIO_ORDINE
				public static Tuple<int, int, string> bModifiedData = Tuple.Create(0, 0, "bool");               //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<int, int, string> bRequestOrderChange = Tuple.Create(0, 1, "bool");         //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<int, int, string> bForceOrderChange = Tuple.Create(0, 2, "bool");           //COMANDO: forzatura cambio ordine
				public static Tuple<int, int, string> bEmptyArea = Tuple.Create(0, 3, "bool");                  //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<int, int, string> bEmptyingAreaInProgress = Tuple.Create(0, 4, "bool");     //STATO: svuotamento zona in corso
			}
			public class Order {
				//ORDINE
				public static Tuple<int, string> iOrderCode = Tuple.Create(2, "integer");                   //VALORE: codice ordine
				public class Tube {
					//DATI DEL TUBO
					public static Tuple<int, int, string> bRoundTube = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rTubeLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rTubeWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rTubeHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rTubeThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rtubeWeightCalculated = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rtubeLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<int, string> rTubeInternalVolume = Tuple.Create(32, "real");        //VALORE: volume interno tubo [litri]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");       //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//DATI DEL PACCO
					public static Tuple<int, string> iTubeNumber = Tuple.Create(42, "integer");        //VALORE: numero tubi per pacco
					public static Tuple<int, string> iProfileOutput = Tuple.Create(44, "integer");     //VALORE: fila uscita controsagoma
					public static Tuple<int, string> rTheoreticalWeight = Tuple.Create(46, "real");    //VALORE: peso teorico [Kg]
					public static Tuple<int, string> rPackageBaseWidth = Tuple.Create(50, "real");     //VALORE: larghezza base pacco [mm]
					public static Tuple<int, string> rBigRowWidth = Tuple.Create(54, "real");          //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<int, string> rPackageSideWidth = Tuple.Create(58, "real");     //VALORE: larghezza lato pacco [mm]
					public static Tuple<int, string> rPackageHeight = Tuple.Create(62, "real");        //VALORE: altezza pacco [mm]
				}
			}
			public class ManualMovement {
				public static Tuple<int, int, string> bCar = Tuple.Create(86, 0, "bool");           //Carrello evacuazione entrata-uscita
				public static Tuple<int, int, string> bCarRolls = Tuple.Create(86, 1, "bool");      //Rulli contenimento 1
				public static Tuple<int, int, string> bCarRolls2 = Tuple.Create(86, 2, "bool");     //Rulli contenimento 2
				public static Tuple<int, int, string> bCarRolls12 = Tuple.Create(86, 3, "bool");    //Rulli contenimento 1/2
				public static Tuple<int, int, string> bLateralTransp = Tuple.Create(86, 4, "bool"); //Trasporti laterali
				public static Tuple<int, int, string> bUpperRolls = Tuple.Create(86, 5, "bool");    //Rulli pneumati superiori
			}
			public class ProductionData {
				//DATI DI PRODUZIONE
				public static Tuple<int, int, string> bTubePresence = Tuple.Create(90, 0, "bool");          //STATO: presenza tubi
				public static Tuple<int, int, string> bEndRow = Tuple.Create(90, 1, "bool");                //STATO: fine fila
				public static Tuple<int, int, string> bEndPackage = Tuple.Create(90, 2, "bool");            //STATO: fine pacco
				public static Tuple<int, int, string> bLateralOutputProfile = Tuple.Create(90, 3, "bool");  //STATO: uscita controsagoma laterale
				public static Tuple<int, int, string> bEvenRow = Tuple.Create(90, 4, "bool");               //STATO: fila pari
				public static Tuple<int, int, string> bTubesInFirstRow = Tuple.Create(90, 5, "bool");       //STATO: prima fila del pacco
				public static Tuple<int, string> iPackageNumber = Tuple.Create(92, "integer");              //VALORE: numero progressivo pacco
				public static Tuple<int, string> iTubesInPackage = Tuple.Create(94, "integer");             //VALORE: tubi su pacco
				public static Tuple<int, string> iTubesInLastRow = Tuple.Create(96, "integer");             //VALORE: tubi ultima fila
				public static Tuple<int, string> iFilesInPackage = Tuple.Create(98, "integer");             //VALORE: file su pacco
			}
			public class Strap {
				// REGGIATURA
				public static Tuple<int, string> iNumberOfStraps = Tuple.Create(110, "integer");                //VALORE: Numero reggiature 
				public static Tuple<int, int, string> aStrapsPosition = Tuple.Create(112, 196, "Array[real]");  //ARRAY: quote di reggiatura [mm] (ARRAY[1..22] OF REAL	)
			}
			public class Setup {
				// SETUP
				public static Tuple<int, int, string> bEnableTablets = Tuple.Create(200, 0, "bool");                //STATO: Reggiatura - Abilitazione tavolette (x PP frontale)
				public static Tuple<int, string> rStrainCountCoefficient1 = Tuple.Create(210, "real");              //VALORE: coefficente conteggio reggiatura 1 [mm/imp]
				public static Tuple<int, string> rStrainCountCoefficient2 = Tuple.Create(214, "real");              //VALORE: coefficente conteggio reggiatura 2 (solo previsto) [mm/imp]
				public static Tuple<int, string> rCenterPickupPacketCoefficientCount = Tuple.Create(218, "real");   //VALORE: coefficente conteggio centratura prelievo pacco [mm/imp]
				public static Tuple<int, string> rSlowdown = Tuple.Create(222, "real");                             //VALORE: Corsa per rallentamento [mm]
				public static Tuple<int, string> rPositioningTabletsOffset = Tuple.Create(226, "real");             //Valore: offset posizione tavolette
				public static Tuple<int, string> rStrapPositionOffset = Tuple.Create(230, "real");                  //VALORE: Offset quota di reggiatura [mm]
			}
		}
		/// <summary>
		/// Variables used while in the program's "Storage" page
		/// </summary>
		public class Storage {
			public const int DBNumber = 495;
			public class OrderChange {
				// Edit order
				public static Tuple<int, int, string> bModifiedData = Tuple.Create(0, 0, "bool");           //COMANDO: dati modificati [da PC o tracking]
				public static Tuple<int, int, string> bRequestOrderChange = Tuple.Create(0, 1, "bool");     //COMANDO: richiesta pop-up forzatura cambio ordine
				public static Tuple<int, int, string> bForceOrderChange = Tuple.Create(0, 2, "bool");       //COMANDO: forzatura cambio ordine
				public static Tuple<int, int, string> bEmptyArea = Tuple.Create(0, 3, "bool");              //STATO: zona vuota, pronta a ricevere dati
				public static Tuple<int, int, string> bEmptyingAreaInProgress = Tuple.Create(0, 4, "bool"); //STATO: svuotamento zona in corso
			}
			public class Order {
				// Order
				public static Tuple<int, string> iOrderCode = Tuple.Create(2, "integer");                   //VALORE: codice ordine
				public class Tube {
					// Tubo
					public static Tuple<int, int, string> bRoundTube = Tuple.Create(4, 0, "bool");          //STATO: tubo tondo
					public static Tuple<int, string> rTubeLength = Tuple.Create(8, "real");                 //VALORE: lunghezza tubo [mm]
					public static Tuple<int, string> rTubeWidth = Tuple.Create(12, "real");                 //VALORE: larghezza tubo [mm]
					public static Tuple<int, string> rTubeHeight = Tuple.Create(16, "real");                //VALORE: altezza tubo [mm]
					public static Tuple<int, string> rTubeThickness = Tuple.Create(20, "real");             //VALORE: spessore tubo [mm]
					public static Tuple<int, string> rtubeWeightCalculated = Tuple.Create(24, "real");      //VALORE: peso tubo calcolato [Kg]
					public static Tuple<int, string> rtubeLenghtBeforeCutting = Tuple.Create(28, "real");   //VALORE: lunghezza tubo prima del taglio [mm]
					public static Tuple<int, string> rTubeInternalVolume = Tuple.Create(32, "real");        //VALORE: volume interno tubo [litri]
					public static Tuple<int, string> rPercentualLineSpeed = Tuple.Create(36, "real");       //VALORE: (0-1) percentuale velocità linea rispetto alla massima
				}
				public class Package {
					//Pacco
					public static Tuple<int, int, string> bHexagonal = Tuple.Create(40, 0, "bool");     //STATO: pacco esagono
					public static Tuple<int, string> iTubeNumber = Tuple.Create(42, "integer");         //VALORE: numero tubi per pacco
					public static Tuple<int, string> iProfileOutput = Tuple.Create(44, "integer");      //VALORE: fila uscita controsagoma
					public static Tuple<int, string> rTheoreticalWeight = Tuple.Create(46, "real");     //VALORE: peso teorico [Kg]
					public static Tuple<int, string> rPackageBaseWidth = Tuple.Create(50, "real");      //VALORE: larghezza base pacco [mm]
					public static Tuple<int, string> rBigRowWidth = Tuple.Create(54, "real");           //VALORE: larghezza fila massima pacco [mm]
					public static Tuple<int, string> rPackageSideWidth = Tuple.Create(58, "real");      //VALORE: larghezza lato pacco [mm]
					public static Tuple<int, string> rPackageHeight = Tuple.Create(62, "real");         //VALORE: altezza pacco [mm]
				}
			}
			public class ManualMovement {
				// Manual movement
				public static Tuple<int, int, string> bLiftingChains = Tuple.Create(86, 0, "bool");          //Catene sollevabili
				public static Tuple<int, int, string> bDrains1_2 = Tuple.Create(86, 1, "bool");              //Scoli_1_2
				public static Tuple<int, int, string> bDrains1_2_3 = Tuple.Create(86, 2, "bool");            //Scoli_1_2_3
				public static Tuple<int, int, string> bDrains1_2_3_4 = Tuple.Create(86, 3, "bool");          //Scoli_1_2_3_4
				public static Tuple<int, int, string> bStorageChains = Tuple.Create(86, 4, "bool");          //Catene stoccaggio
				public static Tuple<int, int, string> bStorage_LiftingChains = Tuple.Create(86, 5, "bool");  //Catene prelievo + catene stoccaggio marcia
			}
			public class ProductionData {
				// Production data
				public static Tuple<int, int, string> bTubePresence = Tuple.Create(90, 0, "bool");           //STATO: presenza tubi
				public static Tuple<int, int, string> bEndRow = Tuple.Create(90, 1, "bool");                 //STATO: fine fila
				public static Tuple<int, int, string> bEndPackage = Tuple.Create(90, 2, "bool");             //STATO: fine pacco
				public static Tuple<int, int, string> bLateralOutputProfile = Tuple.Create(90, 3, "bool");   //STATO: uscita controsagoma laterale
				public static Tuple<int, int, string> bEvenRow = Tuple.Create(90, 4, "bool");                //STATO: fila pari
				public static Tuple<int, int, string> bTubesInFirstRow = Tuple.Create(90, 5, "bool");        //STATO: prima fila del pacco
				public static Tuple<int, string> iPackageNumber = Tuple.Create(92, "integer");         //VALORE: numero progressivo pacco
				public static Tuple<int, string> iTubesInPackage = Tuple.Create(94, "integer");        //VALORE: tubi su pacco
				public static Tuple<int, string> iTubesInLastRow = Tuple.Create(96, "integer");        //VALORE: tubi ultima fila
				public static Tuple<int, string> iFilesInPackage = Tuple.Create(98, "integer");        //VALORE: file su pacco
				public static Tuple<int, string> iNumberOfRailsToLift = Tuple.Create(100, "integer");  //VALORE: numero scoli da sollevare
			}
			public class Setup {
				// Setup
				public static Tuple<int, int, string> bEnableDrain = Tuple.Create(110, 0, "bool");          //STATO: Abilitazione scoli
				public static Tuple<int, int, string> bEvacuateLastPackage = Tuple.Create(110, 1, "bool");  //COMANDO: evacuazione ultimo pacco su scoli
				public static Tuple<int, string> iNumberOfPackagesPerGroup = Tuple.Create(112, "integer");  //VALORE: Numero pacchi per gruppo
				public static Tuple<int, string> rDelayToStartWeighing = Tuple.Create(116, "real");         //VALORE: Ritardo start pesatura
				public static Tuple<int, string> rTimeSpanBetweenPackages = Tuple.Create(120, "real");      //VALORE: Tempo separazione tra pacchi [s]
				public static Tuple<int, string> rTimeSpanBetweenGroups = Tuple.Create(124, "real");        //VALORE: Tempo separazione tra gruppi [s]
			}
		}
	}
}