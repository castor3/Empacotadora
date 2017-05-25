using System;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

public class IBHLinkComm {
	enum Task {
		TDT_UINT8 = 5,
		TDT_UINT16 = 6,
		TFC_READ = 1,
		TFC_WRITE = 2
	}
	enum CON {
		OK = 0,         // service could be executed without an error
		UE = 1,         // timeout from remote station remote station remote station has not responded within 1 sec.timeout
		RR = 2,         // resource unavailable remote station remote station has no left buffer space for the requested service
		RS = 3,         // requested function of master is not activated within the remote station. remote station the connection seems to be closed in the remote station.try to send command again.
		NA = 17,        // no response of the remote station remote station check network wiring, check remote address, check baud rate
		DS = 18,        // master not into the logical token ring network in general check master DP-Address or highest-station-Addres s of other masters. Examine bus wiring to bus short circuits.
		LR = 20,        // Resource of the local FDL controller not available or not sifficient. HOST too many messages. no more segments in DEVICE free
		IV = 21,        // the specified msg.data_cnt parameter invalid HOST check the limit of 222 bytes (read) respectively 216 bytes (write) in msg.data_cnt
		TO = 48,        // timeout, the request message was accepted but no indication is sent back by the remote station remote station MPI protocol error, or station not presentor
		SE = 57,        // Sequence fault, internal state machine error. Remote station does not react like awaited or a reconnection was retried while connection is already open or device has no SAPs left to open connection channel
		RIV = 0x85,     // specified offset address out of limits or not known in the remote station HOST please check msg.data_adr if present or offset parameter in request message
		RPDU = 0x86,    // wrong PDU coding in the MPI response of the remote station DEVICE contact hotline
		ROP = 0x87,     // specified length to write or to read results in an access outside the limits HOST please check msg.data_cnt length in request message
		RHW = 0x88,
		RMODE = 0x89
	}

	[Serializable]
	public struct IBHLinkMSG {

		public byte rx;                 // Receiver Code
		public byte tx;                 // Transmitter Code
		public byte ln;                 // Data length of the Message
		public byte nr;                 // Identification Code
		public byte a;                  // Response Code
		public byte f;                  // Error Code
		public byte b;                  // Command Code
		public byte e;                  // Extention Code
		public byte device_adr;         // Remote partner address
		public byte data_area;          // Data area
		public UInt16 data_adr;         // Data address
		public byte data_idx;           // Data index
		public byte data_cnt;           // Data quantity
		public byte data_type;          // Data type
		public byte function;           // Function code
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 240)]
		public byte[] d;                // User specific data

	}

	//Definitions
	private IBHLinkMSG msg;
	private IBHLinkMSG ans;
	private byte MPIAddress;

	const int HOST = 255;
	const int MPI_TASK = 3;

	const int WRITE_MAX = 212;
	const int READ_MAX = 222;
	const int MAX_DATA_ADR = 65535;

	const int MPI_READ_WRITE_M = 0x33;
	const int MPI_READ_WRITE_DB = 0x31;
	const int MPI_READ_WRITE_IO = 0x34;
	const int MPI_READ_WRITE_CNT = 0x35;
	const int MPI_READ_WRITE_TIM = 0x36;
	const int MPI_DISCONNECT = 0x3F;

	const int INPUT_AREA = 0;
	const int OUTPUT_AREA = 1;

	const int IBHLINK_PORT = 1099;

	private bool dataReady;
	private byte MessageNr = 0;
	private Socket sock;

	public IBHLinkComm(byte MPIAdr) {
		msg = new IBHLinkMSG() {
			d = new byte[240]
		};
		ans = new IBHLinkMSG();

		dataReady = false;

		if (MPIAdr > 126)
			throw new ArgumentException("MPI address is too big!");

		MPIAddress = MPIAdr;
	}

	//Create new socket / Connect to IP adress or host name
	public void Connect(string host, int port) {
		if (!IPAddress.TryParse(host, out IPAddress addr)) {
			IPHostEntry hostEntry = Dns.GetHostEntry(host);
			addr = hostEntry.AddressList[0];
		}

		sock = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		sock.Connect(addr, port);
	}

	//Disconnect socket
	public void Disconnect() {
		GenerateDisconnectRequest(MPIAddress);
		Communicate();
		sock.Disconnect(false);
	}

	//Send and recive the data
	public void Communicate() {
		try {
			sock.Send(GetBytes());

			byte[] data = new byte[1000];
			sock.Receive(data);
			PutBytes(data);
		}
		catch (Exception e) {
			throw e;
		}
	}

	//Check if the request is correct
	private void CheckWriteRequest(int Address, byte[] data) {
		if (data.GetLength(0) > WRITE_MAX)
			throw new ArgumentException("Data is too big!");

		if (Address < 0 || Address > MAX_DATA_ADR)
			throw new ArgumentException("Data address is erroneous!");
	}

	private void CheckReadRequest(int Address, int len) {
		if (len > READ_MAX)
			throw new ArgumentException("Length is too big!");

		if (Address < 0 || Address > MAX_DATA_ADR)
			throw new ArgumentException("Data address is erroneous!");
	}

	private void CheckTClen(int len) {
		if (len % 2 != 0)
			throw new ArgumentException("Length has to be divisible by two!");
	}

	//Increase and reset the message number
	private byte GetMessageNr() {
		if (MessageNr != 255)
			MessageNr++;
		else
			MessageNr = 0;

		return MessageNr;
	}

	//Create write messages
	public void GenerateWriteDBRequest(UInt16 DBNumber, int Address, byte[] data) {
		CheckWriteRequest(Address, data);

		msg.rx = MPI_TASK;
		msg.tx = HOST;
		msg.ln = (byte)(8 + data.GetLength(0));
		msg.nr = GetMessageNr();
		msg.a = 0;
		msg.f = 0;
		msg.b = MPI_READ_WRITE_DB;
		msg.e = 0;
		msg.device_adr = MPIAddress;
		msg.data_area = (byte)((Address & 0xFF00) >> 8);
		msg.data_adr = (ushort)DBNumber;
		msg.data_idx = (byte)(Address & 0x00FF);
		msg.data_cnt = (byte)data.GetLength(0);
		msg.data_type = (byte)Task.TDT_UINT8;
		msg.function = (byte)Task.TFC_WRITE;
		data.CopyTo(msg.d, 0);
	}

	//Create read messages
	public void GenerateReadDBRequest(UInt16 DBNumber, int Address, int len) {
		CheckReadRequest(Address, len);

		msg.rx = MPI_TASK;
		msg.tx = HOST;
		msg.ln = 8;
		msg.nr = GetMessageNr();
		msg.a = 0;
		msg.f = 0;
		msg.b = MPI_READ_WRITE_DB;
		msg.e = 0;
		msg.device_adr = MPIAddress;
		msg.data_area = (byte)((Address & 0xFF00) >> 8);
		msg.data_adr = DBNumber;
		msg.data_idx = (byte)(Address & 0x00FF);
		msg.data_cnt = (byte)len;
		msg.data_type = (byte)Task.TDT_UINT8;
		msg.function = (byte)Task.TFC_READ;
	}

	//Create disconnect message
	public void GenerateDisconnectRequest(int Address) {
		if (Address < 0 || Address > MAX_DATA_ADR)
			throw new ArgumentException("Data address is erroneous!");

		msg.rx = MPI_TASK;
		msg.tx = HOST;
		msg.ln = 8;
		msg.nr = GetMessageNr();
		msg.a = 0;
		msg.f = 0;
		msg.b = MPI_DISCONNECT;
		msg.e = 0;
		msg.device_adr = MPIAddress;
		msg.data_area = 0;
		msg.data_adr = 0;
		msg.data_idx = 0;
		msg.data_cnt = 0;
		msg.data_type = 0;
		msg.function = 0;
	}

	//put the informations to the right position
	private byte[] GetBytes() {
		dataReady = false;

		int size = Marshal.SizeOf(msg);
		int len = 8 + msg.ln;
		IntPtr ip = Marshal.AllocHGlobal(size); //WARNING: ALLOCATING UNMANAGED MEMORY!
		Marshal.StructureToPtr(msg, ip, false);
		byte[] buffer = new byte[len];
		Marshal.Copy(ip, buffer, 0, len);
		Marshal.FreeHGlobal(ip);                //UNALLOCATE - DO NOT REMOVE THIS!
		return buffer;
	}

	//puts the incoming information to the right position and check
	//if message is correct
	private void PutBytes(byte[] data) {
		int size = Marshal.SizeOf(ans);
		IntPtr ptr = Marshal.AllocHGlobal(size);
		Marshal.Copy(data, 0, ptr, size);
		ans = (IBHLinkMSG)Marshal.PtrToStructure(ptr, ans.GetType());
		Marshal.FreeHGlobal(ptr);

		if (ans.a != msg.b)
			throw new ArgumentException("Answer doesn´t fit to Request!");

		if (ans.f != 0)
			throw new ArgumentException(getErrorText(ans.f));

		if (msg.data_type != ans.data_type)
			throw new ArgumentException("Answer and request got different data_types!");

		if (msg.rx != ans.tx)
			throw new ArgumentException("Reciever and transmitter are different!");

		if (msg.nr != ans.nr)
			throw new ArgumentException("Answer and request got different message-numbers!");

		if (msg.e != ans.e)
			throw new ArgumentException("Answer and request got different extensions!");

		if (msg.device_adr != ans.device_adr)
			throw new ArgumentException("Remote station addresses from request and answer are different!");

		if (msg.data_area != ans.data_area)
			throw new ArgumentException("Data areas from request and answer are different!");

		if (msg.data_idx != ans.data_idx)
			throw new ArgumentException("Data index from request and answer are different!");

		if (msg.data_cnt != ans.data_cnt)
			throw new ArgumentException("Data Count from request and answer are different!");

		if (msg.function != ans.function)
			throw new ArgumentException("Function from request and answer are different!");

		if (ans.d == null)
			throw new ArgumentException("No answer-data available!");

		dataReady = true;
	}

	//returns the information-data from message
	public byte[] GetRequestData() {
		if (dataReady != true)
			throw new ArgumentException("No answer-data available!");
		return ans.d;
	}

	//Check error and print error-message
	public static string getErrorText(byte ErrorCode) {
		CON err = (CON)ErrorCode;
		switch (err) {
			case CON.OK:
				return "No Error";
			case CON.UE:
				return "Timeout from remote station. (remote station) Remote station has not responded within 1 sec. timeout.";
			case CON.RR:
				return "Resource unavailable. (remote station) Remote station has no left buffer space for the requested service.";
			case CON.RS:
				return "Requested function of master is not activated within the remote station. (remote station) The connection seems to be closed in the remote station. Try to send command again.";
			case CON.NA:
				return "No response of the remote station. (remote station) Check network wiring, check remote address, check baud rate.";
			case CON.DS:
				return "Master not into the logical token ring. (network in general) Check master DP-Address or highest-station-Addres s of other masters. Examine bus wiring to bus short circuits.";
			case CON.LR:
				return "Resource of the local FDL controller not available or not sifficient. (HOST) Too many messages. No more segments in DEVICE free.";
			case CON.IV:
				return "The specified msg.data_cnt parameter invalid. (HOST) Check the limit of 222 bytes. (read) respectively 216 bytes (write) in msg.data_cnt.";
			case CON.TO:
				return "Timeout, the request message was accepted but no indication is sent back by the remote station. (remote station) MPI protocol error, or station not presentor.";
			case CON.SE:
				return "Sequence fault, internal state machine error. Remote station does not react like awaited or a reconnection was retried while connection is already open or device has no SAPs left to open connection channel. (remote station) In case of sequencs fault consult support center, else retry request message.";
			case CON.RIV:
				return "Specified offset address out of limits or not known in the remote station. (HOST) Please check msg.data_adr if present or offset parameter in request message.";
			case CON.RPDU:
				return "Wrong PDU coding in the MPI response of the remote station. (DEVICE) Contact hotline.";
			case CON.ROP:
				return "Specified length to write or to read results in an access outside the limits. (HOST) Please check msg.data_cnt length in request message.";
			case CON.RHW:
				return "Specified address not defined in the remote station. (HOST) Please check msg.data_adr in the request message.";
			case CON.RMODE:
				return "MPI remote station not in the right operational mode.(remote station) Bring S7 into RUN-P Mode.";
			default:
				return "There was an error!(unknown error-code)";
		}
	}
}
