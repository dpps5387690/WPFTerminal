using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

namespace WPFTerminal
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool _continue;
        Thread readThread;
        private SerialPort _serialPort;
        delegate void Display(string buffer);
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitSerialPortNum()
        {
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();
            // Display each port name to the console.
            comboBox_PortNum.Items.Clear();
            foreach (string port in ports)
            {
                Console.WriteLine(port);
                comboBox_PortNum.Items.Add(port);
            }
            if (ports.Count() == 0)
            {
                comboBox_PortNum.IsEnabled = false;
            }
            else
            {
                comboBox_PortNum.SelectedIndex = 0;
                comboBox_PortNum.IsEnabled = true;
            }
        }
        #region HotKey       
        private void callButtonEvent(Button btn, RoutedEvent EventName)
        {
            btn.RaiseEvent(new RoutedEventArgs(EventName));
            return;
        }

        List<HotKeyClass> HotName = new List<HotKeyClass>()
        {
            new HotKeyClass("Start Serial Port",100,ModifierKeys.None,Key.F10,"button_StartStop"),
            new HotKeyClass("Refresh Serial Port",101,ModifierKeys.None,Key.F5,"button_ReFresh"),
            new HotKeyClass("Save Log for View",102,ModifierKeys.Control,Key.S,"button_Save"),
            new HotKeyClass("Save Log Start",103,ModifierKeys.Alt,Key.D,"button_SaveLog"),
            new HotKeyClass("Search",104,ModifierKeys.None,Key.F3,"button_Find"),
            new HotKeyClass("Clear View",105,ModifierKeys.Control,Key.D,"button_Clear"),
            //bunifuMaterialTextbox
            new HotKeyClass("Search Text",106,ModifierKeys.Control,Key.F,"TextBox_Find"),
            new HotKeyClass("Search Previous",107,ModifierKeys.Shift,Key.F3,"Previous"),
        };
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            HotKeyClass hotKeyClass = HotName.Find(x => (x.keyModifiers == e.KeyboardDevice.Modifiers) && (x.keys == e.Key || x.keys == e.SystemKey));
            if (hotKeyClass == null)
                return;
            if (hotKeyClass.ControlName.Contains("button"))
            {
                object nowtb = FindName(hotKeyClass.ControlName);
                Button tileButton = nowtb as Button;
                callButtonEvent(tileButton, Button.ClickEvent);
            }
            else if (hotKeyClass.ControlName.Contains("TextBox"))
            {
                object nowtb;
                nowtb = FindName(hotKeyClass.ControlName);
                TextBox textbox = nowtb as TextBox;
                textbox.Focus();
            }
            else if (hotKeyClass.ControlName == "Previous")
                FindPrevious();
        }
        private void HotKey_Init()
        {
            foreach (HotKeyClass hotKeyClass in HotName)
            {
                string keycomb = "";
                Key keynum = hotKeyClass.keys;
                ModifierKeys keyModnum = hotKeyClass.keyModifiers;

                //HotKey.RegisterHotKey(Handle, hotKeyClass.ID, keyModnum, keynum);

                if (hotKeyClass.keyModifiers != ModifierKeys.None)
                    keycomb = "(" + keyModnum + "-" + keynum + ")";
                else
                    keycomb = "(" + keynum.ToString() + ")";

               if (hotKeyClass.ControlName.Contains("button"))
               {
                   object nowtb;
                   nowtb = FindName(hotKeyClass.ControlName);
                   if (nowtb != null)
                   {
                        Button tileButton = nowtb as Button;
                        tileButton.ToolTip = hotKeyClass.IniName + keycomb;
                   }
               }

            }
        }
        #endregion
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSerialPortNum();
            comboBox_Speed.SelectedIndex = 6;
            HotKey_Init();

            //string str1 = "tCL\ttRCD/tRP\ttRAS\ttWR\ttCWL\ttRRD_S\ttRRD_L\ttWTR_S\ttWTR_L\ttRFC\ttRFC2\ttRFC4\ttRTP\ttFAW\tCMD_stretch\t\n";
            //string str2 = "19\t26\t46\t24\t18\t9\t14\t3\t8\t842\t278\t171\t12\t53\t1\n";
            //string str3 = "tRDRD_sg\ttRDRD_dg\ttRDRD_dr\ttRDRD_dd\t-tRDWR_sg\ttRDWR_dg\ttRDWR_dr\ttRDWR_dd\t-tWRWR_sg\ttWRWR_dg\ttWRWR_dr\ttWRWR_dd\t-tWRRD_sg\ttWRRD_dg\ttWRRD_dr\ttWRRD_dd\n";
            //string str4 = "16\t16\t16\t16\t-16\t16\t16\t16\t-16\t16\t16\t16\t-16\t16\t16\t16\n\n";

            //richTextBox_View.AppendText(str1);
            //richTextBox_View.AppendText(str2);
            //richTextBox_View.AppendText(str3);
            //richTextBox_View.AppendText(str4);
        }
        private void bunifuImageButton_ReFresh_Click(object sender, EventArgs e)
        {
            InitSerialPortNum();
        }

        public void serial_OPEN()
        {

            _serialPort = new SerialPort();

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = comboBox_PortNum.Text;
            _serialPort.BaudRate = Convert.ToInt32(comboBox_Speed.Text);
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            //_serialPort.ReadTimeout = 10;
            //_serialPort.DataReceived += new SerialDataReceivedEventHandler(comport_DataReceived);
            //_serialPort.WriteTimeout = 500;
        }
        delegate void bunifuImageButton_StartStop_Click_Delegate(object sender, EventArgs e);
        private void comport_DataReceived()
        {
            while (_continue)
            {
                try
                {
                    if (_serialPort.BytesToRead > 0)
                    {

                        string buffer = _serialPort.ReadExisting();

                        Display d = new Display(DisplayText);
                        this.Dispatcher.Invoke(d, new Object[] { buffer });//使用委託的方式操作control
                        //this.BeginInvoke(d, new Object[] { buffer });//使用委託的方式操作control
                        //Thread.Sleep(1);
                    }
                    else
                        Thread.Sleep(1);
                }
                catch (TimeoutException timeoutEx)
                {
                    //以下這邊請自行撰寫你想要的例外處理
                    this.Dispatcher.Invoke(new bunifuImageButton_StartStop_Click_Delegate(this.bunifuImageButton_StartStop_Click), button_StartStop, null);
                }
                catch (Exception ex)
                {
                    //以下這邊請自行撰寫你想要的例外處理
                    this.Dispatcher.Invoke(new bunifuImageButton_StartStop_Click_Delegate(this.bunifuImageButton_StartStop_Click), button_StartStop, null);
                }
            }
        }
        private void DisplayText(string buffer)
        {
            if (WriteLog != null)
                WriteLog.WriteLine(buffer.Replace("\r", ""));

            richTextBox_View.AppendText(buffer.Replace("\r\n", "\n"));
            if (checkBox_ENdLine.IsChecked == true)
            {
                richTextBox_View.ScrollToEnd();
            }
        }
        private void bunifuImageButton_StartStop_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (comboBox_PortNum.Items.Count < 1)
            {
                const string message =
                    "Please connent Debug Card and enter SerialPort to Reflash.";
                const string caption = "error";
                var result = MessageBox.Show(message, caption,
                                             MessageBoxButton.OK,
                                             MessageBoxImage.Error);

                return;
            }


            if (button.Tag.ToString() == "Start")
            {
                readThread = new Thread(comport_DataReceived);
                serial_OPEN();
                try
                {
                    _serialPort.Open();//開啟serial
                    button.Tag = "Stop";
                    Image image1 = new Image();
                    image1.Source = new BitmapImage(new Uri(@"Resources/pause_96px.png", UriKind.Relative));
                    button.Content = image1;

                    //button_StartStop.Background = new SolidColorBrush(Colors.Black);
                    _continue = true;
                    readThread.Start();
                    readThread.IsBackground = true;//thread 背景執行  
                }
                catch (UnauthorizedAccessException er)
                {
                    Console.WriteLine(er.Message);
                    MessageBox.Show(er.Message, "Error");
                }

            }
            else if (button.Tag.ToString() == "Stop")
            {
                _continue = false;//停止thread
                _serialPort.Close();//關閉serial port

                if (WriteLog != null)
                    WriteLog.Close();

                button.Tag = "Start";
                Image image1 = new Image();
                image1.Source = new BitmapImage(new Uri(@"Resources/play_96px.png", UriKind.Relative));
                button.Content = image1;

                if (readThread != null)
                    readThread.Abort();

            }
        }

        private void bunifuImageButton_Clear_Click(object sender, EventArgs e)
        {
            richTextBox_View.Document.Blocks.Clear();
        }
        SaveFileDialog saveFile = null;
        private bool SaveFile()
        {
            saveFile = new SaveFileDialog();
            saveFile.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFile.FilterIndex = 1;
            saveFile.Title = "Save As Log";
            return (bool)saveFile.ShowDialog();
        }
        private void bunifuImageButton_Save_Click(object sender, EventArgs e)
        {
            if (SaveFile() == true)
            {
                //debug richTextBox_View.SaveFile(saveFile.FileName, RichTextBoxStreamType.PlainText);
                TextRange range;
                FileStream fStream;
                range = new TextRange(richTextBox_View.Document.ContentStart, richTextBox_View.Document.ContentEnd);
                fStream = new FileStream(saveFile.FileName, FileMode.Create);
                range.Save(fStream, DataFormats.Text);
                fStream.Close();
            }
        }
        StreamWriter WriteLog;
        private void bunifuImageButton_SaveLog_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button.Tag.ToString() == "SaveStart")
            {
                if (SaveFile() == true)
                {
                    button.Tag = "SaveStop";

                    Image image1 = new Image();
                    image1.Source = new BitmapImage(new Uri(@"Resources/close_window_96px.png", UriKind.Relative));
                    button.Content = image1;

                    WriteLog = new StreamWriter(File.Open(saveFile.FileName, FileMode.Create));
                }
            }
            else if (button.Tag.ToString() == "SaveStop")
            {
                button.Tag = "SaveStart";
                Image image1 = new Image();
                image1.Source = new BitmapImage(new Uri(@"Resources/save_as_96px.png", UriKind.Relative));
                button.Content = image1;
                WriteLog.Close();
                WriteLog = null;
            }

        }
        private void Window_Closed(object sender, EventArgs e)
        {
            _continue = false;
            if (_serialPort != null)
                _serialPort.Close();

            if (WriteLog != null)
                WriteLog.Close();

            if (readThread != null)
                readThread.Abort();
        }
        #region search
        /// <summary>
        /// Takes a string input and searches richtextbox in the direction specified.  
        /// </summary>
        private void SelectWord(string input, LogicalDirection direction)
        {
            RichTextBox rtb = richTextBox_View; //the name of your richtextbox control

            TextPointer currentStartposition = rtb.Selection.Start;
            TextPointer currentEndposition = rtb.Selection.End;
            TextPointer position;
            TextPointer previousPosition;
            string textLine = null;
            if (direction == LogicalDirection.Forward)
            {
                position = currentStartposition.GetLineStartPosition(1);
                previousPosition = currentEndposition;
                if (position != null)
                    textLine = new TextRange(previousPosition, position).Text;
            }
            else
            {
                position = currentStartposition.GetLineStartPosition(0);
                previousPosition = currentStartposition;
                if (position != null)
                    textLine = new TextRange(position, previousPosition).Text;
            }

            while (position != null)
            {
                int indexInRun;
                if (direction == LogicalDirection.Forward)
                    indexInRun = textLine.IndexOf(input, StringComparison.CurrentCultureIgnoreCase);
                else
                    indexInRun = textLine.LastIndexOf(input, StringComparison.CurrentCultureIgnoreCase);

                if (indexInRun >= 0)
                {
                    TextPointer nextPointer = null;
                    if (direction == LogicalDirection.Forward)
                        position = previousPosition;

                    int inputLength = input.Length;
                    while (nextPointer == null)
                    {
                        if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text && nextPointer == null) //checks to see if textpointer is actually text
                        {
                            string textRun = position.GetTextInRun(LogicalDirection.Forward);
                            if (textRun.Length - 1 < indexInRun)
                                indexInRun -= textRun.Length;
                            else //found the start position of text pointer
                            {
                                position = position.GetPositionAtOffset(indexInRun);
                                nextPointer = position;
                                while (inputLength > 0)
                                {
                                    //textRun = nextPointer.GetTextInRun(LogicalDirection.Forward);
                                    //if (Math.Abs(textRun.Length - indexInRun) < inputLength)
                                    //{
                                    //    inputLength -= textRun.Length;
                                    //    indexInRun = 0; //after the first pass, index in run is no longer relevant
                                    //}
                                    //else
                                    {
                                        nextPointer = nextPointer.GetPositionAtOffset(inputLength);
                                        rtb.Selection.Select(position, nextPointer);
                                        rtb.Focus();

                                        //moves the scrollbar to the selected text
                                        Rect r = position.GetCharacterRect(LogicalDirection.Forward);
                                        double totaloffset = r.Top + rtb.VerticalOffset;
                                        rtb.ScrollToVerticalOffset(totaloffset - rtb.ActualHeight / 2);
                                        return; //word is selected and scrolled to. Exit method
                                    }
                                    //nextPointer = nextPointer.GetNextContextPosition(LogicalDirection.Forward);
                                }


                            }
                        }
                        position = position.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }

                previousPosition = position;
                if (direction == LogicalDirection.Forward)
                {
                    position = position.GetLineStartPosition(1);
                    if (position != null)
                        textLine = new TextRange(previousPosition, position).Text;
                }
                else
                {
                    position = position.GetLineStartPosition(-1);
                    if (position != null)
                        textLine = new TextRange(position, previousPosition).Text;
                }

            }

            //if next/previous word is not found, leave the current selected word selected
            rtb.Selection.Select(currentStartposition, currentEndposition);
            rtb.Focus();
            MessageBox.Show("Not find.");
        }

        private void FindNext()
        {
            //richTextBox_View.SelectionBrush = Brushes.White;
            SelectWord(TextBox_Find.Text, LogicalDirection.Forward);
        }

        private void FindPrevious()
        {
            //richTextBox_View.SelectionBrush = Brushes.White;
            SelectWord(TextBox_Find.Text, LogicalDirection.Backward);
        }
        #endregion

        private void bunifuImageButton_Find_Click(object sender, EventArgs e)
        {
            FindNext();
        }

        private void bunifuMaterialTextbox_Find_KeyDown(object sender, KeyEventArgs e)
        {
           if (e.Key == Key.Enter)
               FindNext();
           if (checkBox_ENdLine.IsChecked == true)
               checkBox_ENdLine.IsChecked = false;
        }


    }
}
