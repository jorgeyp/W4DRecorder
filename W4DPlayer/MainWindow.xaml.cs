using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace W4DPlayer
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveFileWriter waveWriter;
        AiffFileWriter aiffWriter;
        DispatcherTimer timer; //It is used to calculate time intervals
        WasapiCapture capture;
        WasapiLoopbackCapture loopbackCapture;
        int length;
        MMDevice device;
        String filename = "record";

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += OnTimerTick;
            InsertInputDevicesInCombo();
            InsertOutputDevicesInCombo();
            InsertShareModeInCombo();
            InsertFormatInCombo();
            InsertBitDepthInCombo();
            textboxSave.Text = filename;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (device != null)
            {
                progressBarAmplitude.Value = device.AudioMeterInformation.MasterPeakValue;
            }
        }

        private int BitDepthToBps(string bitDepth)
        {
            if (bitDepth.Equals("PCM"))
                return 16;
            else return 32;
        }

        private void Finish()
        {
            // TODO
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            Finish();
        }

        private void ButtonClickStart(object sender, RoutedEventArgs e)
        {
                timer.Start();
                device = (MMDevice)comboDevices.SelectedItem;
                var shareMode = (AudioClientShareMode)comboShare.SelectedItem;
                var bitDepth = (string)comboBitDepth.SelectedItem;
                var bps = BitDepthToBps(bitDepth);

                Console.WriteLine("device name: " + device.DeviceFriendlyName);

                Console.WriteLine(device.DataFlow);
                if (device.DataFlow.ToString().Equals("Capture"))
                {
                    Console.WriteLine("Grabando capture");
                    RecordCaptureDevice(device, shareMode, bps);
                }
                else if (device.DataFlow.ToString().Equals("Render"))
                {
                    Console.WriteLine("Grabando render");
                    RecordRenderDevice(device);
                }
                else Console.WriteLine("wrong device");


                //cada 100 ms
                //device.AudioMeterInformation.MasterPeakValue;
                //device.AudioEndpointVolume.MasterVolumeLevelScalar;
        }

        private void RecordRenderDevice(MMDevice device)
        {
            device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)sliderVolume.Value;
            loopbackCapture = new WasapiLoopbackCapture(device);
            createFile(loopbackCapture.WaveFormat);
            loopbackCapture.RecordingStopped += OnRecordingStopped;
            loopbackCapture.DataAvailable += OnDataAvailable;
            Console.WriteLine("grabando");
            loopbackCapture.StartRecording();
        }

        private void createFile(WaveFormat waveFormat)
        {
            if (comboFormat.SelectedItem.Equals("AIFF"))
            {
                aiffWriter = new AiffFileWriter(@"" + filename + ".aiff", waveFormat);
            }
            else if (comboFormat.SelectedItem.Equals("WAV"))
            {
                waveWriter = new WaveFileWriter(@"" + filename + ".wav", waveFormat);
            }
        }

        private void RecordCaptureDevice(MMDevice device, AudioClientShareMode shareMode, int bps)
        {
            device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)sliderVolume.Value;
            capture = new WasapiCapture(device);
            capture.ShareMode = shareMode;
            capture.WaveFormat = new WaveFormat(44100, bps, 2);
            createFile(capture.WaveFormat);
            capture.RecordingStopped += OnRecordingStopped;
            capture.DataAvailable += OnDataAvailable;
            Console.WriteLine("grabando");
            capture.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveWriter != null)
            {
                waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                /*int seconds = (int)(waveWriter.Length / waveWriter.WaveFormat.AverageBytesPerSecond);
                length = seconds;
                if (seconds > 15)
                {
                    capture.StopRecording();
                }*/
            }
            if (aiffWriter != null)
            {
                aiffWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
        }

        private void ButtonClickStop(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            progressBarAmplitude.Value = 0;
            if (capture != null)
            {
                capture.StopRecording();
                capture.Dispose();
                Console.WriteLine("cerrando fichero");
            }
            if (loopbackCapture != null)
            {
                loopbackCapture.StopRecording();
                loopbackCapture.Dispose();
                Console.WriteLine("cerrando fichero");
            }
            if (waveWriter != null)
            {
                waveWriter.Close();
                waveWriter.Dispose();
            }
            if (aiffWriter != null)
            {
                aiffWriter.Close();
                aiffWriter.Dispose();
            }
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// It is necessary to fill in the different devices we have to play audio
        /// </summary>
        private void InsertInputDevicesInCombo()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active); //to render sounds and just the active devices. For example, if we don't have headphones connected to the computer we don't need that element in the list
            // poner capture en vez de render pa grabar
            foreach (var device in devices) //we iterate through all the available devices
            {
                comboDevices.Items.Add(device); //we insert device information in the combo box 
            }
            comboDevices.SelectedIndex = 0; //we mark as the default one the first one
        }

        /// <summary>
        /// It is necessary to fill in the different devices we have to play audio
        /// </summary>
        private void InsertOutputDevicesInCombo()
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active); //to render sounds and just the active devices. For example, if we don't have headphones connected to the computer we don't need that element in the list
            // poner capture en vez de render pa grabar
            foreach (var device in devices) //we iterate through all the available devices
            {
                comboDevices.Items.Add(device); //we insert device information in the combo box 
            }
            comboDevices.SelectedIndex = 0; //we mark as the default one the first one
        }

        private void InsertFormatInCombo()
        {
            comboFormat.Items.Add("WAV");
            comboFormat.Items.Add("AIFF");
            comboFormat.SelectedIndex = 0;
        }

        private void InsertShareModeInCombo()
        {
            comboShare.Items.Add(AudioClientShareMode.Shared);
            comboShare.Items.Add(AudioClientShareMode.Exclusive);
            comboShare.SelectedIndex = 0;
        }

        private void InsertBitDepthInCombo()
        {
            comboBitDepth.Items.Add("PCM");
            comboBitDepth.Items.Add("IEEE Float");
            comboBitDepth.SelectedIndex = 0;
        }



        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (device != null)
            {
                device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)sliderVolume.Value;
            }
        }

        private void ButtonClickSave(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = filename;
            dlg.DefaultExt = "." + comboFormat.SelectedItem.ToString().ToLower();
            dlg.Filter = "Audio files (*.wav;*.aiff)|*.wav;*.aiff|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == true)
            {
                // Save document 
                filename = dlg.FileName;
            }
            textboxSave.Text = filename;
        }

    }
}
