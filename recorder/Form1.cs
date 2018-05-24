using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Lame;
using NAudio.Wave;
using System.Net;
using System.Collections.Specialized;

namespace recorder
{
    public partial class Form1 : Form
    {
        private LameMP3FileWriter wri;
        private bool stopped = true;
        private IWaveIn waveIn = new WaveInEvent();
        private System.Timers.Timer timer = new System.Timers.Timer(1000);
        private int count = 0;

        public Form1()
        {
            InitializeComponent();
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.RecordingStopped += waveIn_RecordingStopped;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timerHandler);
            timer.AutoReset = true;
        }

        private void timerHandler(object source, System.Timers.ElapsedEventArgs e)
        {
            ++count;
            var s = Convert.ToString(count);
            Console.WriteLine("aaaa " + s);
            using (var client = new WebClient())
            {
                var values = "bp0,host=abc level=0.7,seq="+s;
//                client.UploadStringCompleted += new UploadStringCompletedEventHandler(UploadStringCallback2);
                client.UploadStringAsync(new Uri("http://192.168.100.51:8086/write?db=bp"), values);
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            // Setup MP3 writer to output at 32kbit/sec (~2 minutes per MB)
            wri = new LameMP3FileWriter(@"d:\temp\test_output.mp3", waveIn.WaveFormat, 32);
            waveIn.StartRecording();
            stopped = false;
            timer.Enabled = true;
        }
        void waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // flush output to finish MP3 file correctly
            wri.Flush();
            // Dispose of objects
            waveIn.Dispose();
            wri.Dispose();
            // signal that recording has finished
            stopped = true;
        }

        public delegate void MyDelegate(Form1 f, float v);

        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // write recorded data to MP3 writer
            if (wri != null)
                wri.Write(e.Buffer, 0, e.BytesRecorded);
            float max = 0;
            var buffer = new WaveBuffer(e.Buffer);
            // interpret as 32 bit floating point audio
            for (int index = 0; index < e.BytesRecorded / 2; index++)
            {
                var sample = buffer.ShortBuffer[index];
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                // is this the max value?
                if (sample32 > max) max = sample32;
            }
            MyDelegate md = new MyDelegate(showResult);
            this.BeginInvoke(md, this, max);
        }

        public static void showResult(Form1 f, float v)
        {
            f.progressBar1.Value = Convert.ToInt32(100 * v);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            timer.Enabled = false;
            if (!stopped)
                waveIn.StopRecording();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer.Enabled = false;
            waveIn.StopRecording();
            while (!stopped)
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        private void butnUpload_Click(object sender, EventArgs e)
        {
            WebClient webClient = new WebClient();
            webClient.UploadProgressChanged += WebClientUploadProgressChanged;
            webClient.UploadFileCompleted += WebClientUploadCompleted;
            webClient.UploadFileAsync(new Uri("http://localhost:9680/upload"), @"C:\Users\wg\Desktop\id\z1.txt");
        }
        void WebClientUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            Console.WriteLine("Upload {0}% complete. ", e.ProgressPercentage);
        }
        void WebClientUploadCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            Console.WriteLine("completed. ");
        }
    }
}
