using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyPacketCapturer
{
    public partial class frmSend : Form
    {
        public static int instantiations = 0;
        

        public frmSend()
        {
            InitializeComponent();
            instantiations++;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

            string stringBytes = "";
            //get the hex values from the file
            foreach (string s in txtPacket.Lines){
                //Taking out the comments
                string[] noComments = s.Split('#','\r');
                string s1 = noComments[0];
                stringBytes += s1 + Environment.NewLine;
            }
            //Extract the hexvalues into a string array
            string[] sBytes = stringBytes.Split(new string [] {"\n","\r\r"," ","\r"},StringSplitOptions.RemoveEmptyEntries);

            //change the strings into bytes
            byte[] packet = new byte[sBytes.Length];
            int i = 0;
               foreach (string s in sBytes)
                {
                    try {
                        packet[i] = Convert.ToByte(s, 16); i++;
                    }
                    catch (Exception exp) { }
                }

                //send out the packets
            try { 
                frmCapture.device.SendPacket(packet);
            }
            catch (Exception exp)       { } 
        }   //end btnSend_Click 

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            openFileDialog1.Title = "open the Captured Packets";
            openFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (openFileDialog1.FileName != "")
            {
                txtPacket.Text = System.IO.File.ReadAllText(openFileDialog1.FileName);
            }
           
            
        
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text Files|*.txt|All Files|*.*";
            saveFileDialog1.Title = "Save the Captured Packets";
            saveFileDialog1.ShowDialog();

            //Check to see if filename was given
            if (saveFileDialog1.FileName != "")
            {
                System.IO.File.WriteAllText(saveFileDialog1.FileName, txtPacket.Text);
            }

        }

        private void frmSend_FormClosed(object sender, FormClosedEventArgs e)
        {
            instantiations--;
        }
    }
}
