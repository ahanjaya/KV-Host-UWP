using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Sockets;
using System.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Networking;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KV_Host_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        TcpClient client = new TcpClient();

        private void updateLog(string info)
        {
            tbLog.Text += info + Environment.NewLine; // + "\r\n";
            Debug.WriteLine(info);
        }

        private string cmdSend(string message)
        {
            if (client.Connected)
            {
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");

                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer.
                stream.Write(data, 0, data.Length);

                // Receive the TcpServer.response.
                // Buffer to store the response bytes.
                data = new Byte[8192];

                // String to store the response ASCII representation.
                String responseData = String.Empty;
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                responseData = responseData.Replace("\r\n", "");

                updateLog("Command: " + message);
                updateLog("Response Data: " + responseData);
                return responseData;
            }
            else
            {
                updateLog("Disconnected...");
                DisplayNoConnectionDialog();
                return null;
            }
        }

        private async void DisplayNoConnectionDialog()
        {
            ContentDialog noConnectionDialog = new ContentDialog()
            {
                Title = "No PLC connection",
                Content = "Check connection and try again.",
                CloseButtonText = "Ok"
            };

            await noConnectionDialog.ShowAsync();
        }

        private async void DisplayWrongFormatDialog(string msg)
        {
            ContentDialog wrongFormatDialog = new ContentDialog()
            {
                Title = "Wrong Format",
                Content = string.Format("Plase select {0}", msg),
                CloseButtonText = "Ok"
            };

            await wrongFormatDialog.ShowAsync();
        }

        private string queryModel()
        {
            string plc_code, model;
            plc_code = cmdSend("?K");

            switch (plc_code)
            {
                case "57": model = "KV-8000"; break;
                case "54": model = "KV-7300"; break;
                case "55": model = "KV-7500"; break;
                case "51": model = "KV-3000"; break;
                case "52": model = "KV-5000"; break;
                case "53": model = "KV-5500"; break;
                case "128": model = "KV-NC32T"; break;
                case "132": model = "KV-N60**"; break;
                case "133": model = "KV-N40**"; break;
                case "134": model = "KV-N24**"; break;
                default: model = null; break;
            }
            return model;
        }

        private string HexString2Ascii(string hexString)
        {
            try
            {
                hexString = hexString.Replace(" ", "");
                string ascii = string.Empty;
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;
                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }
                ascii = ascii.Replace("\0", "");
                ascii += ".kpr";
                return ascii;
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }

            return string.Empty;
        }

        private void operatingMode()
        {
            string resp;
            resp = cmdSend("?M");

            switch (resp)
            {
                case "0": rbProgram.IsChecked = true; break;
                case "1": rbRun.IsChecked = true; break;
                default: break;
            }
        }

        private void setReset(string mode)
        {
            if (cbMemorySR.SelectedItem != null)
            {
                if (!double.IsNaN(nbDeviceNoSR.Value))
                {
                    string command = string.Empty, device_type, device_no;
                    device_type = cbMemorySR.SelectedItem.ToString();
                    device_no = nbDeviceNoSR.Value.ToString();

                    if (rbSingleSR.IsChecked == true)
                    {
                        command = string.Format("{0} {1}{2}", mode, device_type, device_no);
                    }
                    else if (rbMultipleSR.IsChecked == true)
                    {
                        if (!double.IsNaN(nbNumberSR.Value))
                        {
                            string numbers = nbNumberSR.Value.ToString();
                            command = string.Format("{0}S {1}{2} {3}", mode, device_type, device_no, numbers);
                        }
                        else
                        {
                            DisplayWrongFormatDialog("Total Numbers");
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                    cmdSend(command);
                }
                else
                {
                    DisplayWrongFormatDialog("Device No");
                }
            }
            else
            {
                DisplayWrongFormatDialog("Device Type");
            }
        }

        private bool readWrite()
        {
            if (cbMemoryRW.SelectedItem != null)
            {
                if (!double.IsNaN(nbDeviceNoRW.Value))
                {
                    if (cbDataFormatRW.SelectedItem != null)
                    {
                        if (rbSingleRW.IsChecked == true)
                        {
                            return true;
                        }
                        else if (rbMultipleRW.IsChecked == true)
                        {
                            if (!double.IsNaN(nbNumberRW.Value))
                            {
                                return true;
                            }
                            else
                            {
                                DisplayWrongFormatDialog("Total Numbers");
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        DisplayWrongFormatDialog("Data Type");
                        return false;
                    }
                }
                else
                {
                    DisplayWrongFormatDialog("Device No");
                    return false;
                }
            }
            else
            {
                DisplayWrongFormatDialog("Device Type");
                return false;
            }
        }

        private void clearAll()
        {
            tbPLCModel.Text = "";
            tbPLCProject.Text = "";
            rbProgram.IsChecked = false;
            rbRun.IsChecked = false;

            // Set / Reset
            cbMemorySR.SelectedIndex = -1;
            nbDeviceNoSR.Value = double.NaN;
            nbNumberSR.Value = double.NaN;
            rbSingleSR.IsChecked = false;
            rbMultipleSR.IsChecked = false;

            // Read / Write
            cbMemoryRW.SelectedIndex = -1;
            nbDeviceNoRW.Value = double.NaN;
            nbNumberRW.Value = double.NaN;
            cbDataFormatRW.SelectedIndex = -1;
            cbValuesRW.Items.Clear();
            rbSingleRW.IsChecked = false;
            rbMultipleRW.IsChecked = false;
        }

        private async void btnPing_Click(object sender, RoutedEventArgs e)
        {
            var tcpClient = new TcpClient();
            var connectionTask = tcpClient.ConnectAsync(tbIP.Text, Convert.ToInt16(nbPort.Value)).ContinueWith(task => { return task.IsFaulted ? null : tcpClient; }, TaskContinuationOptions.ExecuteSynchronously);
            var timeoutTask = Task.Delay(1000).ContinueWith<TcpClient>(task => null, TaskContinuationOptions.ExecuteSynchronously);
            var resultTask = Task.WhenAny(connectionTask, timeoutTask).Unwrap();
            var resultTcpClient = await resultTask;
            tcpClient.Dispose();

            if (resultTcpClient != null)
            {
                updateLog("Ping to " + tbIP.Text + ": Successful");
            }
            else
            {
                updateLog("Ping to " + tbIP.Text + ": Failed");
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string server = tbIP.Text;
            int port = Convert.ToInt16(nbPort.Text);
            
            if (btnConnect.Content.Equals("Connect"))
            {
                client = new TcpClient();
                var c = client.BeginConnect(server, port, null, null);
                var success = c.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));
                if (!success)
                {
                    DisplayNoConnectionDialog();
                    return;
                }
                else
                {
                    client.EndConnect(c);
                    updateLog("Connected to " + tbIP.Text + ":" + nbPort.Text);
                    btnConnect.Content = "Disconnect";
                    tbPLCModel.Text = queryModel(); //PLC Model

                    string plc_hex = cmdSend("RDS CM1740.H 32");
                    tbPLCProject.Text = HexString2Ascii(plc_hex); //PLC Project

                    //PLC Status Mode
                    operatingMode();
                    rbSingleSR.IsChecked = true;
                    rbSingleRW.IsChecked = true;
                }
            }
            else if (btnConnect.Content.ToString() == "Disconnect")
            {
                client.Close();
                if (!client.Connected)
                {
                    updateLog("Connection closed");
                    btnConnect.Content = "Connect";
                    clearAll();
                }
            }
        }

        private void rbProgram_Click(object sender, RoutedEventArgs e)
        {
            cmdSend("M0");
        }

        private void rbRun_Click(object sender, RoutedEventArgs e)
        {
            cmdSend("M1");
        }

        private void tbLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            var grid = (Grid)VisualTreeHelper.GetChild(tbLog, 0);
            for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
            {
                object obj = VisualTreeHelper.GetChild(grid, i);
                if (!(obj is ScrollViewer)) continue;
                ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f);
                break;
            }
        }

        private void rbSingleSR_Checked(object sender, RoutedEventArgs e)
        {
            nbNumberSR.Value = 1;
        }

        private void nbNumberRW_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (!double.IsNaN(nbNumberRW.Value))
            {
                var count = cbValuesRW.Items.Count;
                int diff = Math.Abs(Convert.ToInt16(nbNumberRW.Value) - count);

                if (count < nbNumberRW.Value)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        cbValuesRW.Items.Add("");
                    }
                }
                else
                {
                    for (int i = count; i > (count - diff); i--)
                    {
                        cbValuesRW.Items.RemoveAt(i - 1);
                    }
                }
            }
        }

        private void cbValuesRW_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            int index = cbValuesRW.SelectedIndex;

            if (index != -1)
            {
                cbValuesRW.Items[index] = args.Text;
            }
            else
            {
                DisplayWrongFormatDialog("Values Index");
            }
        }

        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            bool check = readWrite();
            if (check)
            {
                string command = string.Empty, device_type, device_no, data_format, numbers, results;
                device_type = cbMemoryRW.SelectedItem.ToString();
                device_no = nbDeviceNoRW.Value.ToString();
                data_format = cbDataFormatRW.SelectedItem.ToString();
                numbers = nbNumberRW.Value.ToString();
                cbValuesRW.Items.Clear();

                if (rbSingleRW.IsChecked == true)
                {
                    command = string.Format("RD {0}{1}{2}", device_type, device_no, data_format);
                }
                else if (rbMultipleRW.IsChecked == true)
                {
                    command = string.Format("RDS {0}{1}{2} {3}", device_type, device_no, data_format, numbers);
                }

                results = cmdSend(command);

                if (results != "E1")
                {
                    string[] values = results.Split(' ');
                    foreach (var val in values)
                    {
                        cbValuesRW.Items.Add(val);
                    }
                    cbValuesRW.SelectedIndex = 0;
                }
            }
        }

        private void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            bool check = readWrite();
            if (check)
            {
                string command = string.Empty, device_type, device_no, data_format, numbers, data = string.Empty;
                device_type = cbMemoryRW.SelectedItem.ToString();
                device_no = nbDeviceNoRW.Value.ToString();
                data_format = cbDataFormatRW.SelectedItem.ToString();
                numbers = nbNumberRW.Value.ToString();

                if (rbSingleRW.IsChecked == true)
                {
                    data = cbValuesRW.SelectedItem.ToString();
                    command = string.Format("WR {0}{1}{2} {3}", device_type, device_no, data_format, data);
                }
                else if (rbMultipleRW.IsChecked == true)
                {
                    foreach (var item in cbValuesRW.Items)
                    {
                        data += (item + " ");
                    }
                    data = data.Remove(data.Length - 1, 1);
                    command = string.Format("WRS {0}{1}{2} {3} {4}", device_type, device_no, data_format, numbers, data);
                }
                cmdSend(command);
            }
        }

        private void rbSingleRW_Checked(object sender, RoutedEventArgs e)
        {
            nbNumberRW.Value = 1;
        }

        private void btnSet_Click(object sender, RoutedEventArgs e)
        {
            setReset("ST");
        }

        private void btnRst_Click(object sender, RoutedEventArgs e)
        {
            setReset("ST");
        }
    }
}
