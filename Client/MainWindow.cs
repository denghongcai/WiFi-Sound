using System;
using System.Globalization;
using System.Runtime.InteropServices;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using System.Windows.Forms;
using CSCore.SoundIn;
using CSCore.Streams;
using CSCore.Win32;
using System.Net.Sockets;

namespace Client
{
    public partial class MainWindow : Form
    {
        //Change this to CaptureMode.Capture to capture a microphone,...
        private CaptureMode CaptureMode = Client.CaptureMode.LoopbackCapture;

        private MMDevice _selectedDevice;
        private WasapiCapture _soundIn;
        private readonly GraphVisualization _graphVisualization = new GraphVisualization();
        private IWaveSource _finalSource;
        private NetworkStream _stream;
        private TcpClient _client;

        public MMDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                if (value != null)
                    btnStart.Enabled = true;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            cbCaptureMode.SelectedIndex = 0;
        }

        private void RefreshDevices()
        {
            deviceList.Items.Clear();

            using (var deviceEnumerator = new MMDeviceEnumerator())
            using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(
                CaptureMode == CaptureMode.Capture ? DataFlow.Capture : DataFlow.Render, DeviceState.Active))
            {
                foreach (var device in deviceCollection)
                {
                    var deviceFormat = WaveFormatFromBlob(device.PropertyStore[
                        new PropertyKey(new Guid(0xf19f064d, 0x82c, 0x4e27, 0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c), 0)].BlobValue);

                    var item = new ListViewItem(device.FriendlyName) {Tag = device};
                    item.SubItems.Add(deviceFormat.Channels.ToString(CultureInfo.InvariantCulture));

                    deviceList.Items.Add(item);
                }
            }
        }

        private void ConnectToSer(string server)
        {
            Int32 port = 14000;
            TcpClient client = new TcpClient(server, port);
            _stream = client.GetStream();
        }

        private void StartCapture()
        {
            if (SelectedDevice == null)
                return;

            if(CaptureMode == CaptureMode.Capture)
                _soundIn = new WasapiCapture();
            else
                _soundIn = new WasapiLoopbackCapture();

            _soundIn.Device = SelectedDevice;
            _soundIn.Initialize();

            var soundInSource = new SoundInSource(_soundIn);
            var singleBlockNotificationStream = new SingleBlockNotificationStream(soundInSource);
            _finalSource = singleBlockNotificationStream.ToWaveSource(24);

            byte[] buffer = new byte[_finalSource.WaveFormat.BytesPerSecond / 4];

            try
            {
                ConnectToSer("127.0.0.1");
                soundInSource.DataAvailable += (s, e) =>
                {
                    int read;
                    while ((read = _finalSource.Read(buffer, 0, buffer.Length)) > 0)
                        _stream.Write(buffer, 0, read);
                };

                singleBlockNotificationStream.SingleBlockRead += SingleBlockNotificationStreamOnSingleBlockRead;

                _soundIn.Start();
            }
            catch (SocketException e)
            {
                MessageBox.Show("SocketException: " + e.ToString());
            }
        }

        private void StopCapture()
        {
            _stream.Close();
            _client.Close();
           _soundIn.Stop();
        }

        private void SingleBlockNotificationStreamOnSingleBlockRead(object sender, SingleBlockReadEventArgs e)
        {
            _graphVisualization.AddSamples(e.Left, e.Right);
        }

        private static WaveFormat WaveFormatFromBlob(Blob blob)
        {
            if (blob.Length == 40)
                return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormatExtensible));
            return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormat));
        }

        private void btnRefreshDevices_Click(object sender, EventArgs e)
        {
            RefreshDevices();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartCapture();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_soundIn != null)
            {
                _soundIn.Stop();
                _soundIn.Dispose();
                _finalSource.Dispose();

                btnStop.Enabled = false;
                btnStart.Enabled = true;
            }
        }

        private void deviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (deviceList.SelectedItems.Count > 0)
            {
                SelectedDevice = (MMDevice) deviceList.SelectedItems[0].Tag;
            }
            else
            {
                SelectedDevice = null;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var image = pictureBox1.Image;
            pictureBox1.Image = _graphVisualization.Draw(pictureBox1.Width, pictureBox1.Height);
            if(image != null)
                image.Dispose();
        }

        private void cbCaptureMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(cbCaptureMode.SelectedItem.ToString())
            {
                case "Loopback":
                    CaptureMode = Client.CaptureMode.LoopbackCapture;
                    break;
                case "Normal":
                    CaptureMode = Client.CaptureMode.Capture;
                    break;
            }
        }
    }

    public enum CaptureMode
    {
        Capture,
        LoopbackCapture
    }
}