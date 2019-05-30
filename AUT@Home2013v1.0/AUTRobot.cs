using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Media;
using DxlPort.Enums;
using DxlPort;

using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Kinect;
using System.Net;
using System.Net.Sockets;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.GPU;


namespace AUT_Home2013v1._0
{
    class AUTRobot
    {
        static KinectSensor sensor;
        static Image<Bgr, byte> K_IMG;

        static int Enable_Motors_Update = 0;

        public static SoundPlayer sound_player;
        public static SpeechSynthesizer reader;
        #region Ports configuration
        static DynamixelPort DXL = new DynamixelPort("COM4", 1000000);
        static DynamixelPort SN_Device1 = new DynamixelPort("COM3", 1000000);
        static DynamixelPort SN_Device2 = new DynamixelPort("COM5", 1000000);
        #endregion Ports configuration

        #region Sensors Varible deffinition
        public static int MSPD1 = 0;
        public static int MSPD2 = 0;
        public static int MSPD3 = 0;

        public static int u_pan = 0;
        public static int u_tilt = 0;
        public static int u_headspeed = 0;

        public static double SN1 = 0;
        public static double SN2 = 0;
        public static double SN3 = 0;
        public static double SN4 = 0;
        public static double SN5 = 0;
        public static double SN6 = 0;
        public static double SN7 = 0;
        public static double SN8 = 0;
        #endregion Sensors Varible deffinition

        /// <summary>
        /// output of compass which about -180 and 180 (degree)
        /// </summary>
        public static int Body_Current_Orientation
        {
            get;
            private set;
        }

        /// <summary>
        /// ARM motors initialize speed
        /// </summary>
        public static int Arm_InitSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// the status of robot moving (when robot move function is runing) 1=moving
        /// </summary>
        public static int Robot_Is_Moving
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Absolute position of body according to zero point (zero point is about 80cm from ground
        /// </summary>
        static int BODY_Current_Z = 0;
        /// <summary>
        /// public current body z position
        /// </summary>
        public static int BODY_C_Z = 0;
        /// <summary>
        /// set body z axis position
        /// </summary>
        static int BODY_Z_SET = 100;

        /// <summary>
        /// set body z point
        /// </summary>
        /// <param name="value">min=0 and max=300</param>
        public static void BODY_Z(int value)
        {
            if (value < 0) value = 0;
            if (value > 300) value = 300;

            BODY_Z_SET = value + 100;
        }        

        /// <summary>
        /// omni directional movement of robot
        /// </summary>
        /// <param name="Vx">moving speed in X direction</param>
        /// <param name="Vy">moving speed in Y direction</param>
        /// <param name="Rw">turning speed in W direction</param>
        public static void Omni_Drive(double Vx, double Vy, double Rw)
        {
            if (Vx < -100) Vx = -100;
            if (Vx > 100) Vx = 100;
            Vx = Vx * 10;

            if (Vy < -100) Vy = -100;
            if (Vy > 100) Vy = 100;
            Vy = Vy * 10;

            if (Rw < -100) Rw = -100;
            if (Rw > 100) Rw = 100;
            Rw = Rw * 10;

            double s1 = -((-Sin(60) * Vx) + (Cos(60) * Vy) + Rw);
            double s2 = -((-Sin(60 + 120) * Vx) + (Cos(60 + 120) * Vy) + Rw);
            double s3 = -((-Sin(60 + 120 + 120) * Vx) + (Cos(60 + 120 + 120) * Vy) + Rw);


            MSPD1 = (int)-s1;
            MSPD2 = (int)-s2;
            MSPD3 = (int)-s3;
        }

        /// <summary>
        /// initialize kineckt
        /// </summary>
        public static void Kineckt_Init()
        {
            Thread init = new Thread(new ParameterizedThreadStart(
                    new Action<object>((t) =>
                    {
                        try
                        {
                            sensor = KinectSensor.KinectSensors[0];
                            sensor.ColorStream.Enable();
                            sensor.SkeletonStream.Enable();
                            sensor.ColorFrameReady += runtime_VideoFrameReady;
                            sensor.Start();
                            
                        }
                        catch
                        {
                            MessageBox.Show("kineckt err");
                        }    
                    })));

            init.SetApartmentState(ApartmentState.STA);
            init.Start();           
        }

        

        static byte[] pixelData;
        static Bitmap s;
        static IntPtr colorPtr = new IntPtr();
        static void runtime_VideoFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            bool receivedData = false;
            using (ColorImageFrame CFrame = e.OpenColorImageFrame())
            {
                if (CFrame == null)
                {
                    // The image processing took too long. More than 2 frames behind.
                }
                else
                {
                    pixelData = new byte[CFrame.PixelDataLength];
                    CFrame.CopyPixelDataTo(pixelData);
                    receivedData = true;
                }
            }

            if (receivedData)
            {
                //i++;
                try
                {                  
                    Marshal.FreeHGlobal(colorPtr);
                    colorPtr = Marshal.AllocHGlobal(pixelData.Length);
                    Marshal.Copy(pixelData, 0, colorPtr, pixelData.Length);
                    s= new Bitmap(640, 480, 640 * 4, System.Drawing.Imaging.PixelFormat.Format32bppRgb, colorPtr);
                    K_IMG = new Image<Bgr, byte>(s);                   
                }
                catch
                { }
            }
        }

        /// <summary>
        /// get last image from kineckt
        /// </summary>
        /// <returns></returns>
        public static Image<Bgr, byte> get_frame()
        {           
            return K_IMG;
        }

        /// <summary>
        /// initialize robot when start 
        /// this function enable internal thread for update motors and other internal process
        /// </summary>
        public static void Robot_init()
        {
            reader = new SpeechSynthesizer();
            sound_player = new SoundPlayer();
            Body_Current_Orientation = 0;
            Robot_Is_Moving = 0;
            Arm_InitSpeed = 30;
            try
            {
                DXL.Open();
                SN_Device1.Open();
                SN_Device2.Open();
                
                Enable_Motors_Update = 1;

                THR_Update_Motors();
                THR_Update_Sensors();
                Init_Arm_speeds();
            }
            catch
            {
                MessageBox.Show("ERR... the ports is not initialize!");
            }
        }

        /// <summary>
        /// internal sensors update thread
        /// </summary>
        static void THR_Update_Sensors()
        {
            #region Varible definition
            Int16 RF11 = 0;
            Int16 RF12 = 0;
            Int16 RF13 = 0;
            Int16 RF14 = 0;
            Int16 RF15 = 0;
            Int16 RF16 = 0;

            Int16 RF21 = 0;
            Int16 RF22 = 0;
            Int16 RF23 = 0;
            Int16 RF24 = 0;
            Int16 RF25 = 0;
            Int16 RF26 = 0;

            Int16 COMX1 = 0;
            Int16 COMY1 = 0;
            Int16 COMZ1 = 0;

            Int16 COMX2 = 0;
            Int16 COMY2 = 0;
            Int16 COMZ2 = 0;
            #endregion Varible definition
            #region sensor update 1
            Thread Sensors_Update1 = new Thread(new ParameterizedThreadStart(
                               new Action<object>((t) =>
                               {
                                   Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                                   int tmp1 = 0;
                                   int tmp2 = 0;
                                   SN_Device1.LatencyTime = 1;
                                   SN_Device1.ReadExisting();
                                   while (true)
                                   {
                                       //System.Threading.Thread.Sleep(1);
                                       tmp1 = SN_Device1.ReadByte();
                                       if (tmp1 == 255)
                                       {
                                           tmp1 = SN_Device1.ReadByte();
                                           if (tmp1 == 255)
                                           {
                                               tmp1 = SN_Device1.ReadByte();
                                               if (tmp1 == 136)
                                               {
                                                   tmp1 = SN_Device1.ReadByte();
                                                   if (tmp1 == 255)
                                                   {
                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF11 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF12 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF13 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF14 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF15 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       RF16 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       COMX1 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       COMY1 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       tmp2 = SN_Device1.ReadByte();
                                                       COMZ1 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device1.ReadByte();
                                                       
                                                   }
                                               }
                                           }
                                       }
                                   }
                               })));
            Sensors_Update1.SetApartmentState(ApartmentState.STA);
            Sensors_Update1.Start();
            #endregion sensor update 1
            #region sensor update 2
            Thread Sensors_Update2 = new Thread(new ParameterizedThreadStart(
                               new Action<object>((t) =>
                               {
                                   Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
                                   int tmp1 = 0;
                                   int tmp2 = 0;
                                   SN_Device2.LatencyTime = 1;
                                   SN_Device2.ReadExisting();
                                   while (true)
                                   {
                                       //System.Threading.Thread.Sleep(1);
                                       tmp1 = SN_Device2.ReadByte();
                                       if (tmp1 == 255)
                                       {
                                           tmp1 = SN_Device2.ReadByte();
                                           if (tmp1 == 255)
                                           {
                                               tmp1 = SN_Device2.ReadByte();
                                               if (tmp1 == 136)
                                               {
                                                   tmp1 = SN_Device2.ReadByte();
                                                   if (tmp1 == 255)
                                                   {
                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF21 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF22 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF23 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF24 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF25 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       RF26 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       COMX2 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       COMY2 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));

                                                       tmp1 = SN_Device2.ReadByte();
                                                       tmp2 = SN_Device2.ReadByte();
                                                       COMZ2 = (Int16)((((Int16)tmp2) << 8) + ((byte)tmp1));
                                                   }
                                               }
                                           }
                                       }
                                   }
                               })));
            Sensors_Update2.SetApartmentState(ApartmentState.STA);
            Sensors_Update2.Start();
            #endregion sensor update 2
            #region global sensors
            Thread Sensors_Update_G = new Thread(new ParameterizedThreadStart(
                               new Action<object>((t) =>
                               {
                                   Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                                   Queue<int> SNN1 = new Queue<int>();
                                   Queue<int> SNN2 = new Queue<int>();
                                   Queue<int> SNN3 = new Queue<int>();
                                   Queue<int> SNN4 = new Queue<int>();
                                   Queue<int> SNN5 = new Queue<int>();
                                   Queue<int> SNN6 = new Queue<int>();
                                   Queue<int> SNN7 = new Queue<int>();
                                   Queue<int> SNN8 = new Queue<int>();

                                   Queue<int> COMPS = new Queue<int>();
                                   Queue<int> BDY = new Queue<int>();

                                   int compass_tmp = 0;
                                   int Number_Of_Q = 20;

                                   int sensor_thr=4;
                                   int sensor_thr_H=800;
                                   int sensor_devider = 20;

                                   while (true)
                                   {
                                       if ((RF15 > sensor_thr)&&(RF15<sensor_thr_H)) SNN1.Enqueue(RF15);
                                       if ((RF13 > sensor_thr)&&(RF15<sensor_thr_H)) SNN2.Enqueue(RF13);
                                       if ((RF12 > sensor_thr)&&(RF15<sensor_thr_H)) SNN3.Enqueue(RF12);
                                       if ((RF11 > sensor_thr)&&(RF15<sensor_thr_H)) SNN4.Enqueue(RF11);
                                       //if (RF15 > sensor_thr)//SNN5.Enqueue(RF15);
                                       if ((RF23 > sensor_thr)&&(RF15<sensor_thr_H)) SNN6.Enqueue(RF23);
                                       if ((RF22 > sensor_thr)&&(RF15<sensor_thr_H)) SNN7.Enqueue(RF22);
                                       if ((RF21 > sensor_thr)&&(RF15<sensor_thr_H)) SNN8.Enqueue(RF21);

                                       if (SNN1.Count() > Number_Of_Q) SNN1.Dequeue();
                                       if (SNN2.Count() > Number_Of_Q) SNN2.Dequeue();
                                       if (SNN3.Count() > Number_Of_Q) SNN3.Dequeue();
                                       if (SNN4.Count() > Number_Of_Q) SNN4.Dequeue();
                                       if (SNN5.Count() > Number_Of_Q) SNN5.Dequeue();
                                       if (SNN6.Count() > Number_Of_Q) SNN6.Dequeue();
                                       if (SNN7.Count() > Number_Of_Q) SNN7.Dequeue();
                                       if (SNN8.Count() > Number_Of_Q) SNN8.Dequeue();

                                       if (SNN1.Count > 0) SN1 = 35 - (int)(SNN1.Average() / sensor_devider);
                                       if (SNN2.Count > 0) SN2 = 35 - (int)(SNN2.Average() / sensor_devider);
                                       if (SNN3.Count > 0) SN3 = 35 - (int)(SNN3.Average() / sensor_devider);
                                       if (SNN4.Count > 0) SN4 = 35 - (int)(SNN4.Average() / sensor_devider);
                                       if (SNN5.Count > 0) SN5 = 0;
                                       if (SNN6.Count > 0) SN6 = 35 - (int)(SNN6.Average() / sensor_devider);
                                       if (SNN7.Count > 0) SN7 = 35 - (int)(SNN7.Average() / sensor_devider);
                                       if (SNN8.Count > 0) SN8 = 35 - (int)(SNN8.Average() / sensor_devider);

                                       COMPS.Enqueue((int)(Math.Atan2(COMX2, COMY2) * 57.28));
                                       if ((RF16 > 1) && (RF16 < 1000)) BDY.Enqueue(RF16);

                                       if (COMPS.Count() > Number_Of_Q) COMPS.Dequeue();
                                       if (BDY.Count() > 10) BDY.Dequeue();

                                       if(BDY.Count()>0) BODY_Current_Z = (int)BDY.Average();
                                       compass_tmp = (int)COMPS.Average();
                                       if (compass_tmp < 0) compass_tmp = compass_tmp+360;
                                       Body_Current_Orientation = compass_tmp;

                                       BODY_C_Z = BODY_Current_Z - 390;

                                       System.Threading.Thread.Sleep(5);
                                   }
                               })));
            Sensors_Update_G.SetApartmentState(ApartmentState.STA);
            Sensors_Update_G.Start();
            #endregion  global sensors
        }

        /// <summary>
        /// kill update of robot internal process
        /// </summary>
        public static void Kill()
        {
            Enable_Motors_Update = 0;          
        }

        /// <summary>
        /// internal Motors update thread
        /// </summary>
        static void THR_Update_Motors()
        {
            Thread Motors_Update = new Thread(new ParameterizedThreadStart(
                    new Action<object>((t) =>
                    {
                        int BSPD = 0;
                        int BERR = 0;
                        Queue<int> I_BERR = new Queue<int>();
                        int SUM_IBERR = 0;
                        double KP = 20;
                        double KI = 0.001;
                        while (true)
                        {
                            System.Threading.Thread.Sleep(2);
                            if (DXL.IsOpen)
                            {
                                if (Enable_Motors_Update == 1)
                                {
                                    #region moving motors
                                    int SP1 = MSPD1;
                                    int SP2 = MSPD2;
                                    int SP3 = MSPD3;

                                    //if (MSPD1 < 0) SP1 = Math.Abs(MSPD1) + 1023;
                                    //if (MSPD2 < 0) SP2 = Math.Abs(MSPD2) + 1023;
                                    //if (MSPD3 < 0) SP3 = Math.Abs(MSPD3) + 1023;

                                    DXL.WriteRegister(2, Register.MovingSpeed, SP1);
                                    DXL.WriteRegister(1, Register.MovingSpeed, SP2);
                                    DXL.WriteRegister(3, Register.MovingSpeed, SP3);
                                    #endregion moving motors
                                    #region moving Body

                                    if ((BODY_Current_Z < 300) || (BODY_Current_Z > 1000))
                                    {
                                        BSPD = 0;
                                    }
                                    else if ((BODY_Current_Z > 300) && (BODY_Current_Z < 1000))
                                    {
                                        if ((BODY_Z_SET > 100) && (BODY_Z_SET < 400))
                                        {
                                            BERR = BODY_Z_SET - (BODY_Current_Z - 290);
                                            I_BERR.Enqueue(BERR);
                                            if (I_BERR.Count() > 100) I_BERR.Dequeue();
                                            SUM_IBERR = I_BERR.Sum();
                                            BSPD = (int)((KP * BERR) + (KI * SUM_IBERR));
                                            if (BSPD > 1023) BSPD = 1023;
                                            if (BSPD < -1023) BSPD = -1023;
                                            if (BSPD < 0) BSPD = Math.Abs(BSPD) + 1023;
                                            if (Math.Abs(BERR) == 1) BSPD = 1;
                                            DXL.WriteRegister(50, Register.MovingSpeed, BSPD);
                                        }
                                    }
                                    #endregion moving Body
                                }
                                else
                                {
                                    DXL.WriteRegister(50, Register.MovingSpeed, 0);
                                    DXL.WriteRegister(1, Register.TorqueEnable, 0);
                                    DXL.WriteRegister(2, Register.TorqueEnable, 0);
                                    DXL.WriteRegister(3, Register.TorqueEnable, 0);
                                }
                            }
                        }
                    })));

            Motors_Update.SetApartmentState(ApartmentState.STA);
            Motors_Update.Start();
        }

        public static double Cos(double d)
        {
            return Math.Cos(d * Math.PI / 180d);
        }
        public static double Sin(double d)
        {
            return Math.Sin(d * Math.PI / 180d);
        }

        /// <summary>
        /// enable torque of moving motors on/off
        /// </summary>
        /// <param name="Mode">true turn torque on , false turn torque off</param>
        public static void Motors_Torque(bool Mode)
        {
            if (Mode == true)
            {
                if (DXL.IsOpen)
                {
                    Enable_Motors_Update = 1;
                    
                    DXL.WriteRegister(1, Register.TorqueEnable, 1);
                    DXL.WriteRegister(2, Register.TorqueEnable, 1);
                    DXL.WriteRegister(3, Register.TorqueEnable, 1);
                    
                }
            }
            else
            {
                if (DXL.IsOpen)
                {
                    Enable_Motors_Update = 0;
                    
                    DXL.WriteRegister(1, Register.TorqueEnable, 0);
                    DXL.WriteRegister(2, Register.TorqueEnable, 0);
                    DXL.WriteRegister(3, Register.TorqueEnable, 0);
                }
            }

        }

        /// <summary>
        /// move head position with desire speed
        /// </summary>
        /// <param name="pan">pan position min=-80 and max=80 (each number means 0.34 degree)  </param>
        /// <param name="tilt">tilt position of camera tilt min=0 and max=90 (each number means 1 degree) (0 is front view)</param>
        /// <param name="v">speed of moving (min=0 , max=100)</param>
        public static void head(double pan, double tilt, double v)
        {
            Action act = new Action(() =>
            {
                int d_pan = 512;
                int d_tilt = 512;
                int d_speed = 10;

                if (pan > 80) pan = 80;
                if (pan < -80) pan = -80;
                if (tilt > 90) tilt = 90;
                if (tilt < 0) tilt = 0;
                if (v > 100) v = 100;
                if (v < 2) v = 2;

                d_pan = 512 + (int)pan;
                d_tilt = (int)(512 + (288f / 90) * ((int)tilt));
                d_speed = (int)(v / 2);

                for (int i = 0; i < 3; i++)
                {
                    DXL.WriteRegister(31, Register.MovingSpeed, d_speed);
                    DXL.WriteRegister(31, Register.GoalPosition, d_pan);

                    DXL.WriteRegister(32, Register.MovingSpeed, d_speed);
                    DXL.WriteRegister(32, Register.GoalPosition, d_tilt);
                }
            });

            act.BeginInvoke(null, null);
        }

        /// <summary>
        /// initialize motors arm speed
        /// </summary>
        static void Init_Arm_speeds()
        {
            Action act = new Action(() =>
           {
               for (int i = 0; i < 3; i++)
               {
                   DXL.WriteRegister(51, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(53, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(55, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(57, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(6, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(7, Register.MovingSpeed, Arm_InitSpeed);

                   DXL.WriteRegister(52, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(54, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(56, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(58, Register.MovingSpeed, Arm_InitSpeed);
                   DXL.WriteRegister(10, Register.MovingSpeed, Arm_InitSpeed);
               }
           });

            act.BeginInvoke(null, null);

        }

        /// <summary>
        /// speech text to voice
        /// </summary>
        /// <param name="text">the string to speech</param>
        public static void Speech(string text)
        {
            //reader.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Child, 1);
            Action act = new Action(() =>
            {
                reader.SelectVoiceByHints(VoiceGender.NotSet);
                reader.SpeakAsync(text);
            });
            act.BeginInvoke(null, null);

        }
      
        /// <summary>
        /// plaing sound file
        /// </summary>
        /// <param name="name">file location (include absolute file name and local file name)</param>
        public static void SoundPlay(string name)
        {
            Action act = new Action(() =>
            {
                try
                {
                    sound_player.SoundLocation = name;
                    sound_player.Play();
                }
                catch
                {
                    MessageBox.Show("File Not Found !");
                }
            });
            act.BeginInvoke(null, null);
        }
      
        /// <summary>
        /// stop the current plaing sound file (it works dependig by soundplay() function)
        /// </summary>
        public static void SoundStop()
        {
            sound_player.Stop();
        }

        /// <summary>
        /// internal function for arm motors update position
        /// </summary>
        /// <param name="right"></param>
        /// <param name="left"></param>
        static void Update_ARM(int[] right,int[] left)
        {
          if ( right.Length != 6 ) return;
          if ( left.Length != 5 ) return;

          Action act = new Action(() =>
          {
              for (int i = 0; i < 3; i++)
              {
                  DXL.WriteRegister(51, Register.GoalPosition, right[0]);
                  DXL.WriteRegister(53, Register.GoalPosition, right[1]);
                  DXL.WriteRegister(55, Register.GoalPosition,right[2]);
                  DXL.WriteRegister(57, Register.GoalPosition,right[3]);
                  DXL.WriteRegister(6, Register.GoalPosition, right[4]);
                  DXL.WriteRegister(7, Register.GoalPosition, right[5]);

                  DXL.WriteRegister(52, Register.GoalPosition, left[0]);
                  DXL.WriteRegister(54, Register.GoalPosition, left[1]);
                  DXL.WriteRegister(56, Register.GoalPosition, left[2]);
                  DXL.WriteRegister(58, Register.GoalPosition, left[3]);
                  DXL.WriteRegister(10, Register.GoalPosition, left[4]);
                 
              }
          });
          act.BeginInvoke(null, null);
        }

        /// <summary>
        /// move the robot both arm to ready mode for gripe
        /// </summary>
        /// <param name="mode"></param>
        public static void both_ARM_ready(int mode)
        {

            if (mode == 0)
            {
                Update_ARM(new int[] { 567, 613, 437, 777, 512, 512 }, new int[] { 437, 411, 604, 777, 512 });
            }
            else if (mode == 1)
            {
                Update_ARM(new int[] { 567-40, 613, 437-310, 777+35, 512, 512 }, new int[] { 437+30, 411, 604+320, 777+45, 512 });
            }
        }
       
        /// <summary>
        /// move the robot right arm to ready mode for gripe
        /// </summary>
        /// <param name="mode"></param>
        public static void right_ARM_ready(int mode)
        {
                   
                   if (mode == 0)
                   {
                       Update_ARM(new int[] { 630-60, 672+40, 367-27, 678+95, 512, 512 }, new int[] { 512,512,512,512,512 });
                   }
                   else if (mode == 1)
                   {
                       Update_ARM(new int[] { 531, 646, 698,839, 512, 512 }, new int[] { 512, 512, 512, 512, 512 });
                   }
                   else if (mode == 2)
                   {
                       Update_ARM(new int[] { 573, 683, 346, 433, 512, 512 }, new int[] { 512, 512, 512, 512, 512 });
                   }

        }

        /// <summary>
        /// move the robot left arm to ready mode for gripe
        /// </summary>
        /// <param name="mode"></param>
        public static void left_ARM_ready(int mode)
        {

            if (mode == 0)
            {
                Update_ARM(new int[] { 500, 512, 512, 512, 512, 512 }, new int[] { 382, 304, 717, 693+50, 512 });
               
            }
            else if (mode == 1)
            {
                Update_ARM(new int[] { 500, 512, 512, 512, 512, 512 }, new int[] { 473, 326, 391, 805, 512 });
              
            }
            else if (mode == 2)
            {
                Update_ARM(new int[] { 500, 512, 512, 512, 512, 512 }, new int[] { 401, 314, 705, 418, 512 });
            }
         

        }

        /// <summary>
        /// init both of arms
        /// </summary>
        public static void both_ARM_stop()
        {

            Update_ARM(new int[] { 500, 512, 512, 512, 512, 512 }, new int[] { 490, 512, 512, 512, 512 });

        }

        /// <summary>
        /// Right automatic gripping
        /// </summary>
        /// <param name="MaxLoad">the value of grripping load, min=1 and max=100 - the zero(0) means ungrrip</param>
        public static void Right_Gripper(int MaxLoad)
        {
            Action act = new Action(() =>
            {
                if (MaxLoad < 0) MaxLoad = 0;
                if (MaxLoad > 100) MaxLoad = 100;
                MaxLoad = MaxLoad * 10;
                    if (MaxLoad==0)
                    {
                        DXL.WriteRegister(6, Register.GoalPosition, 635);
                        DXL.WriteRegister(7, Register.GoalPosition, 389);
                    }
                    else
                    {                
                        int load = DXL.ReadRegister(7, Register.CurrentLoad);
                        if (load < 0) load = 0;
                        if (load > 1023) load = load - 1023;

                        int position = DXL.ReadRegister(6, Register.CurrentPosition);
                        int position1 = DXL.ReadRegister(7, Register.CurrentPosition);
                        double avg = 0;
                        int number_of_q = 10;
                        Queue<int> queue = new Queue<int>();
                        for (int i = 1; i <= 5; i++)
                        {
                            queue.Enqueue(load);
                            avg = queue.Average();
                            load = DXL.ReadRegister(7, Register.CurrentLoad);
                            if (load < 0) load = 0;
                            if (load > 1023) load = load - 1023;
                            System.Threading.Thread.Sleep(1);
                        }

                        while (avg < MaxLoad)
                        {
                            System.Threading.Thread.Sleep(1);
                            load = DXL.ReadRegister(7, Register.CurrentLoad);
                            if (load < 0) load = 0;
                            if (load > 1023) load = load - 1023;

                            if ((load >= 0) && (load < 1023))
                            {
                                if (queue.Count > number_of_q) queue.Dequeue();
                                queue.Enqueue(load);

                                avg = queue.Average();

                                if (avg < MaxLoad)
                                {
                                    position = DXL.ReadRegister(6, Register.CurrentPosition);
                                    position1 = DXL.ReadRegister(7, Register.CurrentPosition);
                                    position -= 20;
                                    position1 += 20;
                                    DXL.WriteRegister(6, Register.GoalPosition, position);
                                    DXL.WriteRegister(7, Register.GoalPosition, position1);
                                }                        
                            }
                        }                         
                 }                
            });
            act.BeginInvoke(null, null);
        }

        /// <summary>
        /// Left automatic gripping
        /// </summary>
        /// <param name="MaxLoad">the value of grripping load, min=1 and max=100 - the zero(0) means ungrrip</param>
        public static void Left_Gripper(int MaxLoad)
        {
            Action act = new Action(() =>
            {
                if (MaxLoad < 0) MaxLoad = 0;
                if (MaxLoad > 100) MaxLoad = 100;
                MaxLoad = MaxLoad * 10;
                    if (MaxLoad==0)
                    {
                        DXL.WriteRegister(10, Register.GoalPosition, 512);
                    }
                    else
                    {
                        int load = DXL.ReadRegister(10, Register.CurrentLoad);
                        if (load < 0) load = 0;
                        if (load > 1023) load = load - 1023;
                        int position = DXL.ReadRegister(10, Register.CurrentPosition);
                        
                        int number_of_q = 10;
                        Queue<int> queue = new Queue<int>();
                        for (int i = 0; i <= 5; i++)
                        {
                            queue.Enqueue(load);
                            load = DXL.ReadRegister(10, Register.CurrentLoad);
                            if (load < 0) load = 0;
                            if (load > 1023) load = load - 1023;
                            System.Threading.Thread.Sleep(1);
                        }

                        while (queue.Average() < MaxLoad)
                        {

                          load = DXL.ReadRegister(10, Register.CurrentLoad);
                          if (load < 0) load = 0;
                          if (load > 1023) load = load - 1023;

                          if ((load >= 0) && (load < 1023))
                          {
                              if (queue.Count > number_of_q) queue.Dequeue();
                              queue.Enqueue(load);
                              position = DXL.ReadRegister(10, Register.CurrentPosition);
                              position += 30;
                              DXL.WriteRegister(10, Register.GoalPosition, position);
                          }
                          System.Threading.Thread.Sleep(2);
                        }                       
                    }               
            });
            act.BeginInvoke(null, null);
        }

        /// <summary>
        /// move robot
        /// </summary>
        /// <param name="distance_x">distance of moving in x direction, per cm</param>
        /// <param name="distance_y">distance of moving in y direction, per c</param>
        public static void Move_Robot(int distance_x,int distance_y)
        {
            Robot_Is_Moving = 1;
            if ((distance_x != 0) && (distance_y != 0))
            {
                if ((distance_x > 0) && (distance_y > 0))
                {
                    AUTRobot.Omni_Drive(16, 16, 0);
                }
                else if ((distance_x < 0) && (distance_y > 0))
                {
                    AUTRobot.Omni_Drive(-16, 16, 0);
                }
                else if ((distance_x > 0) && (distance_y < 0))
                {
                    AUTRobot.Omni_Drive(16, -16, 0);
                }
                else if ((distance_x < 0) && (distance_y < 0))
                {
                    AUTRobot.Omni_Drive(-16, -16, 0);
                }
                int time = (int)(Math.Sqrt((distance_x * distance_x) + (distance_y * distance_y)) * 70);
                System.Threading.Thread.Sleep(time);
                AUTRobot.Omni_Drive(0, 0, 0);
            }
            else
            {
                if (distance_x > 0)
                {
                    AUTRobot.Omni_Drive(16, 0, 0);
                    System.Threading.Thread.Sleep(Math.Abs(distance_x) * 100);
                    AUTRobot.Omni_Drive(0, 0, 0);
                }
                if (distance_x < 0)
                {
                    AUTRobot.Omni_Drive(-16, 0, 0);
                    System.Threading.Thread.Sleep(Math.Abs(distance_x) * 100);
                    AUTRobot.Omni_Drive(0, 0, 0);
                }

                if (distance_y > 0)
                {
                    AUTRobot.Omni_Drive(0, 16, 0);
                    System.Threading.Thread.Sleep(Math.Abs(distance_y) * 100);
                    AUTRobot.Omni_Drive(0, 0, 0);
                }
                if (distance_y < 0)
                {
                    AUTRobot.Omni_Drive(0, -16, 0);
                    System.Threading.Thread.Sleep(Math.Abs(distance_y) * 100);
                    AUTRobot.Omni_Drive(0, 0, 0);
                }
            }
            Robot_Is_Moving = 0;
        }

        /// <summary>
        /// turn robot 
        /// </summary>
        /// <param name="degree">the degree to turn (min=1, max=360)</param>
        public static void Turn(int degree)
        {
            if ((degree >= 0) && (degree <= 360))
            {
                if (degree > 0)
                {
                    AUTRobot.Omni_Drive(0, 0, 15);
                }
                if (degree < 0)
                {
                    AUTRobot.Omni_Drive(0, 0, -15);
                }
                if (degree == 0) return;
                System.Threading.Thread.Sleep((int)((Math.Abs(degree) * 2860) / 90));
                AUTRobot.Omni_Drive(0, 0, 0);
            }
        }

    }
}
