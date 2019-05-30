using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;



namespace AUT_Home2013v1._0
{
    public partial class Form1 : Form
    {
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void btn_init_robot_Click(object sender, EventArgs e)
        {
            AUTRobot.Robot_init();
            AUTRobot.Kineckt_Init();
            sensor_show();
            Get_Farame();
        }
        void Get_Farame()
        {
            Thread getFrame = new Thread(new ParameterizedThreadStart(
                               new Action<object>((t) =>
                               {
                                   while (true)
                                   {
                                       System.Threading.Thread.Sleep(100);
                                       this.Invoke(new Action(() =>
                                       {
                                           if (AUTRobot.get_frame() != null)
                                               pictureBox1.Image = AUTRobot.get_frame().ToBitmap();
                                       }));
                                   }
                               })));
            
            getFrame.SetApartmentState(ApartmentState.STA);
            getFrame.Start();
        }

        #region Test functions
        public Form1()
        {
            InitializeComponent();
        }
        void sensor_show()
        {
            #region sensors show thr
            Thread Sensors_Update_Show = new Thread(new ParameterizedThreadStart(
                               new Action<object>((t) =>
                               {
                                   while (true)
                                   {
                                       System.Threading.Thread.Sleep(10);
                                       this.Invoke(new Action(() =>
                                       {
                                           label19.Text = AUTRobot.SN1.ToString();
                                           label18.Text = AUTRobot.SN2.ToString();
                                           label17.Text = AUTRobot.SN3.ToString();
                                           label16.Text = AUTRobot.SN4.ToString();
                                           label15.Text = AUTRobot.SN5.ToString();
                                           label14.Text = AUTRobot.SN6.ToString();
                                           label13.Text = AUTRobot.SN7.ToString();
                                           label12.Text = AUTRobot.SN8.ToString();

                                           label23.Text = (AUTRobot.BODY_C_Z).ToString();
                                           label22.Text = AUTRobot.Body_Current_Orientation.ToString();
                                       }));

                                   }
                               })));
            Sensors_Update_Show.SetApartmentState(ApartmentState.STA);
            Sensors_Update_Show.Start();
            #endregion  sensors show thr
        }
        protected override CreateParams CreateParams
        {
            get
            {
                const int CP_NOCLOSE = 0x200;
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE;
                return myCp;
            }
        }
        private void EXIT_Click(object sender, EventArgs e)
        {
            AUTRobot.Kill();
            System.Threading.Thread.Sleep(10);
            Process.GetCurrentProcess().Kill();
            this.Close();
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked==true)
            {
                AUTRobot.Motors_Torque(true);
            }
            else
            {
                AUTRobot.Motors_Torque(false);
            }
        }
        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            AUTRobot.BODY_Z((int)numericUpDown4.Value);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            AUTRobot.head((double)numericUppan.Value, (double)numericUptilt.Value, (double)numericUpspeed.Value);
        }
        private void button9_Click(object sender, EventArgs e)
        {
            AUTRobot.Speech(textBox1.Text);
        }
        private void button10_Click(object sender, EventArgs e)
        {
         
            AUTRobot.SoundPlay(textBox2.Text);
        
        }
        private void button11_Click(object sender, EventArgs e)
        {
            AUTRobot.SoundStop();
        }
        private void button12_Click(object sender, EventArgs e)
        {
            AUTRobot.both_ARM_ready(1);
        }
        private void button13_Click(object sender, EventArgs e)
        {
            AUTRobot.right_ARM_ready((int)numericUpmr.Value);
        }
        private void button14_Click(object sender, EventArgs e)
        {
            AUTRobot.left_ARM_ready((int)numericUpml.Value);
        }
        private void button15_Click(object sender, EventArgs e)
        {
            AUTRobot.both_ARM_stop();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(15, 0, 0);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(-10, 0, 0);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(0, -10, 0);
        }
        private void button5_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(0, 10, 0);
        }
        private void button7_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(0, 0, -10);
        }
        private void button8_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(0, 0, 10);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive(0, 0, 0);
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive((double)numericUpDown1.Value, (double)numericUpDown2.Value, (double)numericUpDown3.Value);
        }
        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive((double)numericUpDown1.Value, (double)numericUpDown2.Value, (double)numericUpDown3.Value);
        }
        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            AUTRobot.Omni_Drive((double)numericUpDown1.Value, (double)numericUpDown2.Value, (double)numericUpDown3.Value);
        }
        private void button16_Click(object sender, EventArgs e)
        {
            AUTRobot.Left_Gripper(20);
            //AUTRobot.Right_Gripper(4);
        }
        private void button17_Click(object sender, EventArgs e)
        {
            AUTRobot.Right_Gripper(0);
            AUTRobot.Left_Gripper(0);
        }
        private void button18_Click(object sender, EventArgs e)
        {
            Action act = new Action(() => 
            {
                while (true)
                {
                    if (AUTRobot.SN1 > 30) AUTRobot.Omni_Drive(15, 0, 0);
                    if (AUTRobot.SN1 <= 30) AUTRobot.Omni_Drive(0, 0, 15);

                    if (AUTRobot.SN2 <= 30) AUTRobot.Omni_Drive(0, 0, 15);
                    if (AUTRobot.SN3 <= 30) AUTRobot.Omni_Drive(0, 0, 15);

                    if (AUTRobot.SN8 <= 30) AUTRobot.Omni_Drive(0, 0, -15);
                    if (AUTRobot.SN7 <= 30) AUTRobot.Omni_Drive(0, 0, -15);
                    System.Threading.Thread.Sleep(5);
                }
            });
            act.BeginInvoke(null, null);
        }
        #endregion Test functions

        private void MAIN_THR_Click(object sender, EventArgs e)
        {
            Thread MainLoop = new Thread(new ParameterizedThreadStart(
                    new Action<object>((t) =>
                    {

                        System.Threading.Thread.Sleep(5);
                        //enter code here! :)                       
                        //Use AUTRobot class for get the function and enjoy it! :)

                        //example:

                        //AUTRobot.Omni_Drive(0, 0, 0);
                    })));

            MainLoop.SetApartmentState(ApartmentState.STA);
            MainLoop.Start();
        }

        private void ShFace_Click(object sender, EventArgs e)
        {
            this.Hide();
            FaceForm faceform = new FaceForm(this);
            faceform.Show();
        }
    }
}
