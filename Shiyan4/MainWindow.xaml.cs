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
using System.IO.Ports;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace Shiyan4
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort;
        Slider[] sliders = new Slider[6];
        int[] pins = { 0, 3, 5, 6, 9, 10 };
        Color[] colors = { Colors.White, Colors.White, Colors.Blue, Colors.Yellow, Colors.Green, Colors.Red };
        BindingList<MidiMessage> recvList = new BindingList<MidiMessage>(), sendList = new BindingList<MidiMessage>();
        delegate void MessageHandler(MidiMessage message);
        MessageHandler messageHandler;
        delegate void UpdateTemperatureHandler(int val);
        UpdateTemperatureHandler updateTemperature;
        delegate void UpdateBrightnessHandler(int val);
        UpdateBrightnessHandler updateBrightness;
        bool logStarted;
        PortLog portLog;
        string fileName;
        Point[] tPoints, bPoints;
        int tCount, bCount;

        public MainWindow()
        {
            InitializeComponent();
            cbBaudRate.Items.Add(9600);
            cbBaudRate.Items.Add(19200);
            cbBaudRate.Items.Add(38400);
            cbBaudRate.Items.Add(57600);
            cbBaudRate.SelectedIndex = 0;
            sliders[1] = slider1;
            sliders[2] = slider2;
            sliders[3] = slider3;
            sliders[4] = slider4;
            sliders[5] = slider5;
            lvReceive.ItemsSource = recvList;
            lvSend.ItemsSource = sendList;
            messageHandler = new MessageHandler(HandleMessage);
            updateTemperature = new UpdateTemperatureHandler(UpdateTemperature);
            updateBrightness = new UpdateBrightnessHandler(UpdateBrightness);
            logStarted = false;
        }

        private void CbPortName_DropDownOpened(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            cbPortName.Items.Clear();
            foreach (string port in ports)
            {
                cbPortName.Items.Add(port);
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (cbPortName.SelectedIndex == -1)
            {
                MessageBox.Show("请选择端口");
                return;
            }
            string selectedPort = (string)cbPortName.SelectedItem;
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                if (port == selectedPort)
                {
                    serialPort = new SerialPort(port, (int)cbBaudRate.SelectedItem);
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(DataHandler);
                    serialPort.Open();
                    MessageBox.Show("连接成功");
                    tPoints = new Point[200];
                    bPoints = new Point[200];
                    new Thread(new ThreadStart(SerialQuery)).Start();
                    return;
                }
            }
            MessageBox.Show("该端口已失效");
        }

        private void SerialQuery()
        {
            while (serialPort.IsOpen)
            {
                Dispatcher.Invoke(messageHandler, new MidiMessage(new byte[] { 0xE0, 0x11, 0x11 }, false));
                Dispatcher.Invoke(messageHandler, new MidiMessage(new byte[] { 0xE1, 0x11, 0x11 }, false));
                Thread.Sleep(500);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("当前未建立连接");
                return;
            }
            serialPort.Close();
            MessageBox.Show("连接已断开");
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("端口未打开");
                return;
            }
            for (int i = 1; i <= 5; ++i)
            {
                int val = 255 - (int)Math.Round(sliders[i].Value);
                byte[] message = { (byte)(0xD0 | pins[i]), (byte)(val & 0x7F), (byte)(val >> 7 & 1)};
                HandleMessage(new MidiMessage(message, false));
            }
        }

        double GetTemperature(int val)
        {
            double ret = 3435 * (25 + 273.15) / (3435 + (25 + 273.15) * (Math.Log((1024.0 / val - 1) * 10000) - Math.Log(10000)));
            return ret;
        }

        public void DataHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            byte[] message = new byte[3];
            port.Read(message, 0, 3);
            Dispatcher.Invoke(messageHandler, new MidiMessage(message, true));
            byte cmd = (byte)(message[0] >> 4 & 0xF);
            switch (cmd)
            {
                case 0xE:
                    int val = (message[2] << 7 | message[1]) & 0x3FF;
                    int chan = message[2] >> 3 & 0xF;
                    switch (chan)
                    {
                        case 0:
                            Dispatcher.Invoke(updateTemperature, val);
                            break;
                        case 1:
                            Dispatcher.Invoke(updateBrightness, val);
                            break;
                    }
                    break;
            }
        }

        void HandleMessage(MidiMessage msg)
        {
            if (msg.IsRecv)
            {
                recvList.Insert(0, msg);
            }
            else
            {
                try
                {
                    serialPort.Write(msg.DataBytes, 0, 3);
                    sendList.Insert(0, msg);
                }
                catch (Exception ex)
                {
                }
            }
            if (logStarted) portLog.Items.Add(new PortLogItem( msg));
        }

        private void UpdateTemperature(int val)
        {
            double temperature = GetTemperature(val) - 273.15;
            if (temperature < 0 || temperature > 50) return;
            txtTemperature.Text = string.Format("{0:F2}℃", temperature);
            double y = 150 - temperature * 3;
            if (tCount == 200)
            {
                for (int i = 1; i < 200; ++i)
                {
                    tPoints[i - 1] = new Point(i - 1, tPoints[i].Y);
                }
                tPoints[199] = new Point(199, y);
            }
            else
            {
                tPoints[tCount] = new Point(tCount, y);
                ++tCount;
            }
            List<Point> tList = new List<Point>();
            for (int i = 0; i < tCount; ++i) tList.Add(tPoints[i]);
            plTemperature.Points = new PointCollection(tList);
        }

        private void UpdateBrightness(int val)
        {
            int brightness = val;
            if (brightness < 0 || brightness > 1023) return;
            txtBrightness.Text = "" + brightness;
            double y = 150 - brightness / 1024.0 * 150;
            if (bCount == 200)
            {
                for (int i = 1; i < 200; ++i)
                {
                    bPoints[i - 1] = new Point(i - 1, bPoints[i].Y);
                }
                bPoints[199] = new Point(199, y);
            }
            else
            {
                bPoints[bCount] = new Point(bCount, y);
                ++bCount;
            }
            List<Point> bList = new List<Point>();
            for (int i = 0; i < bCount; ++i) bList.Add(bPoints[i]);
            plBrightness.Points = new PointCollection(bList);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int index = 0;
            Slider slider = (Slider)sender;
            for (index = 1; index <= 5; ++index)
            {
                if (slider == sliders[index])
                {
                    break;
                }
            }
            SetColor(colors[index], (int)Math.Round(slider.Value));
        }

        private string GetDefaultFileName()
        {
            return string.Format("log-{0:yyyy-MM-dd-HH-mm-ss}", DateTime.Now);
        }

        private void BtnLogStart_Click(object sender, RoutedEventArgs e)
        {
            logStarted = true;
            portLog = new PortLog();
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = GetDefaultFileName();
            dialog.Filter = "JSON文件|*.json";
            if (!(bool)dialog.ShowDialog())
            {
                logStarted = false;
                return;
            }
            fileName = dialog.FileName;
            MessageBox.Show("记录已开始");
        }

        private void BtnLogEnd_Click(object sender, RoutedEventArgs e)
        {
            if (!logStarted) return;
            logStarted = false;
            File.WriteAllText(fileName, JsonConvert.SerializeObject(portLog));
            MessageBox.Show("文件已保存");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        private void SetColor(Color color, int alpha)
        {
            color.A = (byte)alpha;
            cvsColor.Background = new SolidColorBrush(color);
        }
    }
}
