using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using NAudio.Wave;
using System.Diagnostics;

namespace usbcamera
{
    public partial class Form1 : Form
    {
        VideoCaptureDevice videoSource;
        VideoFileWriter FileWriter;
        FilterInfoCollection videoDevices;
        VideoCapabilities[] ca;
        WaveFileWriter writer = null;
        WaveInEvent waveIn = new WaveInEvent();
        int waveInDevices;
        string name;
        int screenshot = 0;
        int record = 0;
        int width ;
        int height;
        int framerate;
        //bool write_finish_flag = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FileWriter = new VideoFileWriter();
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo aa in videoDevices)
            {               
                comboBox2.Items.Add(aa.Name);
            }
            tableLayoutPanel3.CellBorderStyle = TableLayoutPanelCellBorderStyle.Inset;
            tableLayoutPanel4.CellBorderStyle = TableLayoutPanelCellBorderStyle.Inset;
            waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                comboBox3.Items.Add(deviceInfo.ProductName);
            }
            button1.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            foreach (FilterInfo aa in videoDevices)
            {
                if (aa.Name == comboBox2.SelectedItem)
                {
                    if (videoSource!=null)
                    {
                        videoSource.SignalToStop();
                        if (FileWriter.IsOpen)
                        {

                            FileWriter.Close();
                        }
                        button1.Text = "start";
                        button4.Text = "start record";
                        record = 0;
                        button4.Enabled = false;
                        button3.Enabled = false;
                    }
                    videoSource = new VideoCaptureDevice(aa.MonikerString);
                    ca = videoSource.VideoCapabilities;
                    comboBox1.Items.Clear();
                    foreach (VideoCapabilities a in ca)
                    {
                        comboBox1.Items.Add(a.FrameSize);
                    }
                    
                    videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                    break;
                }
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            Bitmap newBmp = new Bitmap(eventArgs.Frame);                                 
             pictureBox1.Image = (Bitmap)newBmp.Clone();
            if (record==1)
            {
                if (FileWriter.IsOpen)
                {
                    newBmp = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
                    // write_finish_flag = false;
                    try
                    {
                        FileWriter.WriteVideoFrame(newBmp);
                    }
                    catch (IOException)
                    {
                    }
                    catch (Exception)
                    {

                    }
                    
                   // write_finish_flag = true;

                }
            }
            
            if (screenshot==1)
            {
                newBmp.Save(System.Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg");
                screenshot = 0;
            }          
        }


        public void Delays(double delayTime)
        {
            Console.WriteLine(DateTime.Now + ":" + "delay:" + delayTime);
            DateTime now = DateTime.Now;
            double s;
            do
            {
                TimeSpan spand = DateTime.Now - now;
                s = spand.TotalMilliseconds;
                Application.DoEvents();
            }
            while (s < delayTime);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (button1.Text == "pause")
            {
                button1.Text = "start";
                button4.Enabled = false;
                button3.Enabled = false;
                videoSource.Stop();
                if (FileWriter != null)
                {
                    if (FileWriter.IsOpen)
                    {
                        FileWriter.Close();
                       
                    }
                }
            }
            else
            {
                button1.Text = "pause";
                button4.Enabled = true;
                button3.Enabled = true;
                if (videoSource.IsRunning)
                {
                    videoSource.Stop();
                }
                bool resolution_flag = false;
                foreach (VideoCapabilities a in ca)
                {
                    if (a.FrameSize == (Size)comboBox1.SelectedItem)
                    {
                        videoSource.VideoResolution = a;
                        resolution_flag = true;
                        tableLayoutPanel3.Size = a.FrameSize;
                        pictureBox1.Size = a.FrameSize;
                        this.Size = new Size(a.FrameSize.Width, a.FrameSize.Height + tableLayoutPanel4.Height + this.Height - this.ClientSize.Height);
                        break;
                    }
                }
                if (!resolution_flag)
                {
                    MessageBox.Show("resolution not find");
                }
                videoSource.Start();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (videoSource!=null)
            {
                videoSource.SignalToStop();
            }
            if (waveIn != null)
            {
                waveIn.StopRecording();
            }
            if (writer!=null)
            {
                writer.Close();
            }
           
            
                     
            System.Environment.Exit(0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (videoSource.IsRunning)
            {
                screenshot = 1;
            }
            else
            {
                MessageBox.Show("camera not running");
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
            if (videoSource.IsRunning)
            {
                if (record == 1)
                {
                    button4.Text = "start record";
                    button1.Enabled = true;
                    record = 0;
                    if (waveInDevices > 0)
                    {
                        writer.Close();
                        waveIn.StopRecording();
                    }
                    FileWriter.Close();
                    //merge(name+".mp4",name+".wav", name + "_merge.mp4");
                   // File.Delete(name + ".wav");
                   // File.Delete(name + ".mp4");

                }
                else
                {
                    name = System.Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    button4.Text = "stop record";
                    button1.Enabled = false;
                    record = 1;
                    if (waveInDevices>0)
                    {
                        writer = new WaveFileWriter(name + ".wav", waveIn.WaveFormat);
                        
                        waveIn.DataAvailable += WaveIn_DataAvailable;
                        waveIn.StartRecording();
                    }


                    FileWriter.Open(name + ".mp4", width, height, framerate, VideoCodec.MPEG4);
                   
                }
            }
            else
            {
 
                MessageBox.Show("camera not running");
            }
        }


        public void merge(string video, string audio, string outs)
        {
            Process my = new Process();
            my.StartInfo.FileName = System.Environment.CurrentDirectory + @"\dll\ffmpeg.exe";
            my.StartInfo.UseShellExecute = false;
             my.StartInfo.CreateNoWindow = true;
            my.StartInfo.Arguments = "-i " + video + " -i " + audio + "  -c:v copy -c:a aac -strict experimental " + outs;
           // MessageBox.Show(my.StartInfo.Arguments);
            my.StartInfo.RedirectStandardOutput = true;
            my.Start();
            string output = my.StandardOutput.ReadToEnd();
            //my.WaitForExit();
            
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception)
            {
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
            if (videoSource.IsRunning)
            {
                videoSource.Stop();
            }
            bool resolution_flag = false;
            foreach (VideoCapabilities a in ca)
            {
                if (a.FrameSize == (Size)comboBox1.SelectedItem)
                {
                    videoSource.VideoResolution = a;
                    resolution_flag = true;
                    tableLayoutPanel3.Size = a.FrameSize;
                    pictureBox1.Size = a.FrameSize;
                    width = a.FrameSize.Width;
                    height = a.FrameSize.Height;
                    framerate = a.AverageFrameRate;
                        
                    if (FileWriter.IsOpen)
                    {
                        if (record == 1)
                        {
                            FileWriter.Close();
                            
                           // FileWriter.Open(System.Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".mp4", width, height, 20, VideoCodec.MPEG4);
                            
                        }
                    }
                    tableLayoutPanel3.Location = new Point(0, 0);
                    this.Size = new Size(a.FrameSize.Width, a.FrameSize.Height + tableLayoutPanel4.Height + this.Height - this.ClientSize.Height);
                    break;
                }
            }
            if (!resolution_flag)
            {
                MessageBox.Show("resolution not find");
            }
            videoSource.Start();
            button1.Text = "pause";
            if (waveInDevices == 0)
            {
                //if (comboBox3.SelectedItem != null)
                //{
                button4.Enabled = true;
                // }
            }
            else
            {
                if (comboBox3.SelectedItem != null)
                {
                button4.Enabled = true;
                }
            }

            button3.Enabled = true;
        }


        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < waveInDevices; i++)
            {
                if (WaveIn.GetCapabilities(i).ProductName == comboBox3.SelectedItem.ToString())
                {
                    
                    waveIn.DeviceNumber = i;
                    if (comboBox1.SelectedItem!=null)
                    {
                        button4.Enabled = true;
                    }
                    
                    break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
