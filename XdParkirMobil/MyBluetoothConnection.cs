
using Android.Bluetooth;
using Android.Runtime;
using System.Collections.Generic;
using Java.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace XdParkirMobil
{
    public class MyBluetoothConnection
    {
        private BluetoothAdapter btAdapter;
        private BluetoothDevice btDevice;
        private BluetoothSocket btSocket;
        private string uuidString;
        private string errMsg;
        private readonly string errHeader = "E>";

        public MyBluetoothConnection(BluetoothAdapter adapter,
            string deviceName, string uuidString)
        {
            MakeConnection(adapter, deviceName, uuidString);
        }

        public string MakeConnection(BluetoothAdapter adapter,
            string deviceName, string uuidString)
        {
            this.uuidString = uuidString;

            if (adapter == null)
            {
                errMsg = "No Bluetooth adapter found.";
                return errMsg;
            }

            btAdapter = adapter;
            if (!btAdapter.IsEnabled)
            {
                errMsg = "Bluetooth adapter is not enabled.";
                return errMsg;
            }

            var device1 = btAdapter.BondedDevices.FirstOrDefault(r => r.Name == deviceName);
            if (device1 == null)
            {
                errMsg = $"Device with name {deviceName} not found";
                return errMsg;
            }
            btDevice = device1;

            if (btDevice.BondState != Bond.Bonded)
            {
                btDevice.CreateBond();
                if (btDevice.BondState == Bond.Bonded)
                {
                    // Toast.MakeText(this, "bt1 Bounded --> By else", ToastLength.Long);
                }
            }

            var socket1 = btDevice.CreateRfcommSocketToServiceRecord(
                Java.Util.UUID.FromString(uuidString));
            if (socket1 == null)
            {
                errMsg = $"Socket with UUID:{uuidString} from device {deviceName} not found";
                return errMsg;
            }
            btSocket = socket1;
            try
            {
                if (!btSocket.IsConnected)
                    btSocket.Connect();
            }
            catch (System.Exception ex)
            {
                errMsg = $"Cannot CONNECTED: {ex.Message}";
                return errMsg;
            }
            finally
            {
                // btSocket.Close();
            }

            errMsg = string.Empty; // Must define for no error
            return "1"; // For Status "OK"
        }

        public string ErrorMessage { get { return errMsg; } }

        public string SendMessageA(Mutex mx, int timeOut = 100)
        {
            return SimpleTalk((byte)'A', mx, timeOut);//.GetAwaiter().GetResult();
        }

        public string SendMessageX(Mutex mx, int timeOut = 100)
        {
            return SimpleTalk((byte)'X', mx, timeOut);//.GetAwaiter().GetResult();
        }

        // public async Task<string> SimpleTalk(byte cmd, Mutex mx, int timeOut = 100)
        public string SimpleTalk(byte cmd, Mutex mx, int timeOut = 100)
        {
            if (mx is null)
            {
                throw new System.ArgumentNullException(nameof(mx));
            }

            string rStr = "";  // result string
            if (!string.IsNullOrEmpty(errMsg)) return errHeader + errMsg;
            mx.WaitOne();
            try
            {
                if (!btSocket.IsConnected) btSocket.Connect();

                btSocket.OutputStream.WriteByte(cmd);

                System.Threading.Thread.Sleep(timeOut);

                var mReader = new InputStreamReader(btSocket.InputStream);
                var buffer = new BufferedReader(mReader);
                if (buffer.Ready())
                {
                    // rStr = buffer.ReadLine();
                    char[] chr = new char[100];
                    buffer.Read(chr);
                    rStr = "";

                    foreach (char c in chr)
                    {
                        if (c == '\0')
                            break;
                        rStr += c;
                    }
                }
            }
            catch (System.Exception ex)
            {
                errMsg = errHeader + "SimpleTalk: " + ex.Message;
                rStr = errMsg;
            }
            finally
            {
                mx.ReleaseMutex();     // must be called here !!!
                // btSocket.Close();
            }

            return rStr;
        }

        public void CloseSocket()
        {
            btSocket.Close();
        }

    }
}