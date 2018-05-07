using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PacketDotNet;
using SharpPcap;


namespace MyPacketCapturer
{
    public partial class frmCapture : Form
    {
        CaptureDeviceList devices;  //List of devices for this computers
        public static ICaptureDevice device;  //the device we will be using
        public static string stringPackets = "";  //data that was captured
        static int numPackets = 0;
        frmSend fSend; //This will be our send form
        static Dictionary<String, int[]> connections = new Dictionary<String, int[]>();

        public frmCapture()
        {
            InitializeComponent();
            
            //get the list of devices
            devices = CaptureDeviceList.Instance;

            //make sure that there is at least one device
            if (devices.Count < 1)
            {
                MessageBox.Show("no Capture Devices Found!");
                Application.Exit();
            }

            //add devices to the combo box
            foreach (ICaptureDevice dev in devices)
            {
                cmbDevices.Items.Add(dev.Description);
            }

            //get the third device and display in combo box
            device = devices[0];
            cmbDevices.Text = device.Description;

            //register our handler function to the packet arrival event
            device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);

            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
        }

        private static void device_OnPacketArrival(object sender, CaptureEventArgs packet)
        {
            //increment number of packets captured
            numPackets++;

            //array to store our data
            byte[] data = packet.Packet.Data;

            //keep track of the number of bytes displayed per line
            int byteCounter = 0;

            if (data[23] == 6)
            { 
                stringPackets += Environment.NewLine + Environment.NewLine;
                stringPackets += "Packet Number: " + numPackets + Environment.NewLine;
                stringPackets += "Destination MAC Address: ";
                //parsing the packets 
                foreach (byte b in data)
                {
                    //add the byte to our string in hexadecimal
                    if (byteCounter <= 13) stringPackets += b.ToString("X2") + " ";
                    byteCounter++;

                    switch (byteCounter)
                    {
                        case 6:
                            stringPackets += Environment.NewLine;
                            stringPackets += "Source MAC Address: ";
                            break;
                        case 12:
                            stringPackets += Environment.NewLine;
                            stringPackets += "EtherType: ";
                            break;
                        case 14:
                            if (data[12] == 8)
                            {
                                if (data[13] == 0) stringPackets += "(IP)";
                                if (data[13] == 6) stringPackets += "(ARP)";
                            }
                            break;
                        case 24:
                            stringPackets += Environment.NewLine;
                            //Check IP header if protocol is TCP
                            if (data[23] == 6)
                            {
                                stringPackets += "Protocol: (TCP)";
                                stringPackets += Environment.NewLine;

                                //Storing source mac address in a string
                                String sourceMac = data[6].ToString("X2") + ":" + data[7].ToString("X2") + ":" + data[8].ToString("X2") +
                                                    ":" + data[9].ToString("X2") + ":" + data[10].ToString("X2") + ":" + data[11].ToString("X2");
                                
                                //Getting source port
                                String port = data[34].ToString("X2") + data[35].ToString("X2");
                                int portNum = Convert.ToInt32(port, 16);
                                stringPackets += "Source Port: " + portNum.ToString();
                                if (connections.ContainsKey(sourceMac))
                                {
                                    connections[sourceMac][1] += 1;
                                }
                                else
                                {
                                    int [] temp = {portNum, 1};
                                    connections.Add(sourceMac, temp);
                                }
                                //getting destination port
                                port = data[36].ToString("X2") + data[37].ToString("X2");
                                portNum = Convert.ToInt32(port, 16);
                                stringPackets += Environment.NewLine;
                                stringPackets += "Desination Port: " + portNum.ToString();
                            }
                            break;
                    }

                }
            }
            byteCounter = 0;
        }
        

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnStartStop.Text == "Start")
                {
                    device.StartCapture();
                    timer1.Enabled = true;
                    btnStartStop.Text = "Stop";
                }
                else
                {
                    device.StopCapture();
                    timer1.Enabled = false;
                    btnStartStop.Text = "Start";
                    txtCapturedData.AppendText("\n");
                    //message to display info on each source of tcp packets
                    String message = "";
                    foreach (KeyValuePair<String, int[]> key in connections)
                    {
                        message += "\n";
                        message += "MAC Address: " + key.Key.ToString() + " -- Port: " + key.Value[0] + " -- Packets Sent: " + key.Value[1];
                        message += "\n";
                        message += "Traffic Percent: " + Math.Round(((double)key.Value[1] / (double)numPackets) * 100.0, 2) + "%";
                        message += "\n";
                    }
                    if (!message.Equals(""))
                    {
                        MessageBox.Show(message);
                    }
                }
            }
            catch(Exception exp)
            {

            }
        }

        //dump the packet data from stringPackets to the text box
        private void timer1_Tick(object sender, EventArgs e)
        {
            txtCapturedData.AppendText(stringPackets);
            stringPackets = "";
            txtNumPackets.Text = Convert.ToString(numPackets);
           
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            device = devices[cmbDevices.SelectedIndex];
            cmbDevices.Text = device.Description;

            //register our handler function to the packet arrival event
            device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);

            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtCapturedData.Text);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            openFileDialog1.Title = "open the Captured Packets";
            openFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (openFileDialog1.FileName != "")
            {
                txtCapturedData.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
        }

        private void sendWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frmSend.instantiations == 0)
            {
                fSend = new frmSend(); // creates a new frmSend
                fSend.Show();
              
            }          
        }
    }
}
