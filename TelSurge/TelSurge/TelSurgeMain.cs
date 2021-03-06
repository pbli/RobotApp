﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TelSurge.DataModels;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;


using Emgu.CV.UI;

using System.Drawing;
using System.Windows.Forms;

namespace TelSurge
{
    public partial class TelSurgeMain : Form
    {
        public User User { get; set; }
        public AudioConference AudioConference { get; set; }
        public VideoCapture VideoCapture { get; set; }
        public Surgery Surgery { get; set; }
        public Markup Markup { get; set; }
        public SocketData SocketData { get; set; }
        private string logFile = "log.csv";
        private string sendGRAddr = "";
        public int networkDelay = 0; //in ms
        private int controlPort = 11005;
        private int connectionPort = 11004;
        private int audioPort = 11003;
        private int markingsPort = 11002;
        private int dataPort = 11001;
        private int videoPort = 11000;
        private bool isDrawing = false;
        private List<Point> tmpPoints = new List<Point>();
        private Color penColor = Color.Red;
        private bool isFirstPointOfFigure = true;
        public OmniPosition HapticForces { get; set; }
        private bool logDataTurnAroundTime = false;
        private System.Windows.Forms.Timer turnAroundTimer = new System.Windows.Forms.Timer();
        private TcpListener grantReqListener = null;
        private bool telSurgeOnly = false;
        public bool SendFrozen { get; set; }
        //OUTPUTS
        public OmniPosition OutputPosition { get; set; }
        private Point videoClickPoint = new Point();
        public int messageCount { get; set; }

        /*
        double forceOffset_LX = 0;
        double forceOffset_LY = 0;
        double forceOffset_LZ = 0;
        double forceOffset_RX = 0;
        double forceOffset_RY = 0;
        double forceOffset_RZ = 0; 
        bool sendFreezeComd = false;
        private OmniPosition inControlPosition;
        private OmniPosition frozenPosition;
        private OmniPosition positionOffset = new OmniPosition();
        public OmniPosition currentPosition;
        
        private bool buttonPressed = false;
        private bool feedbackEnabled = false;
        //private volatile bool isListeningForData = false;
        private volatile string inControlIP;
        
        
        private volatile bool applicationRunning = true;
        string exeFile = (new System.Uri(Assembly.GetEntryAssembly().CodeBase)).AbsolutePath;
        
        private bool EmergencySwitch = false;
        public bool networkDataDelayChanged = false;
        
        public string EmergencySwitchBoundBtn;
        public int EmergencySwitchBoundValue;
        */

        public TelSurgeMain()
        {
            InitializeComponent();
            try
            {
                this.User = new User(this, connectionPort);
                this.AudioConference = new TelSurge.AudioConference(this, audioPort);
                this.VideoCapture = new VideoCapture(this, videoPort);
                this.Surgery = new Surgery();
                this.Markup = new Markup(this, markingsPort);
                this.SocketData = new SocketData(this, dataPort);
                OutputPosition = new OmniPosition();
                TelSurgeMain.CheckForIllegalCrossThreadCalls = false;
                fillOmniDDL();
                fillAudioDeviceDDL();
                HapticForces = new OmniPosition();
                SendFrozen = false;
                messageCount = 0;

                //Set Force Trackbar
                //want force divider between 20 and 220
                //divider = 220 - trackbarValue 
                trb_forceStrength.Minimum = 0;
                trb_forceStrength.Maximum = 200;
                // The TickFrequency property establishes how many positions are between each tick-mark.
                trb_forceStrength.TickFrequency = 20;
                // The LargeChange property sets how many positions to move if the bar is clicked on either side of the slider.
                trb_forceStrength.LargeChange = 2;
                // The SmallChange property sets how many positions to move if the keyboard arrows are used to move the slider.
                trb_forceStrength.SmallChange = 1;
                //set initial value of trackbar
                trb_forceStrength.Value = 170;

                CaptureImageBox.FunctionalMode = Emgu.CV.UI.ImageBox.FunctionalModeOption.Minimum;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
        public void ClearMarkup() 
        {
            Markup.MyMarkings.Clear();
        }
        private void showOmniPositions(OmniPosition currentPosition)
        {
            lbX1value.Text = "X : " + currentPosition.LeftX.ToString();
            lbY1value.Text = "Y : " + currentPosition.LeftY.ToString();
            lbZ1value.Text = "Z : " + currentPosition.LeftZ.ToString();

            lbGimbal11.Text = "Gimbal 1 : " + currentPosition.Gimbal1Left.ToString();
            lbGimbal21.Text = "Gimbal 2 : " + currentPosition.Gimbal2Left.ToString();
            lbGimbal31.Text = "Gimbal 3 : " + currentPosition.Gimbal3Left.ToString();

            lbButtons1.Text = "Buttons : " + currentPosition.ButtonsLeft;
            lbInk1.Text = "InkWell : " + currentPosition.InkwellLeft.ToString();
            bool pedPress = false;
            for (int i = 0; i < User.NumExternalButtons; i++)
            {
                if (currentPosition.ExtraButtons[i])
                {
                    lbl_ExBtns.Text = "Ex. Buttons :" + (i + 1).ToString();
                    pedPress = true;
                    break;
                }
            }
            if (!pedPress)
                lbl_ExBtns.Text = "Ex. Buttons : 0";

            lbX2Value.Text = "X : " + currentPosition.RightX.ToString();
            lbY2Value.Text = "Y : " + currentPosition.RightY.ToString();
            lbZ2Value.Text = "Z : " + currentPosition.RightZ.ToString();

            lbGimbal12.Text = "Gimbal 1 : " + currentPosition.Gimbal1Right.ToString();
            lbGimbal22.Text = "Gimbal 2 : " + currentPosition.Gimbal2Right.ToString();
            lbGimbal32.Text = "Gimbal 3 : " + currentPosition.Gimbal3Right.ToString();

            lbButtons2.Text = "Buttons : " + currentPosition.ButtonsRight.ToString();
            lbInk2.Text = "InkWell : " + currentPosition.InkwellRight.ToString();
        }
        private void showOutputPosition()
        {
            tb_SendingLeft.Text = "X = " + OutputPosition.LeftX + Environment.NewLine + "Y = " + OutputPosition.LeftY + Environment.NewLine + "Z = " + OutputPosition.LeftZ;
            tb_SendingRight.Text = "X = " + OutputPosition.RightX + Environment.NewLine + "Y = " + OutputPosition.RightY + Environment.NewLine + "Z = " + OutputPosition.RightZ;
        }
        public void SetForceX(double ForceX, bool IsLeft)
        {
            if (User.IsInControl)
                User.SetOmniForceX(ForceX, IsLeft);
            else
            {
                if (IsLeft)
                    HapticForces.LeftX = ForceX;
                else
                    HapticForces.RightX = ForceX;
            }
        }
        public void SetForceY(double ForceY, bool IsLeft)
        {
            if (User.IsInControl)
                User.SetOmniForceY(ForceY, IsLeft);
            else
            {
                if (IsLeft)
                    HapticForces.LeftY = ForceY;
                else
                    HapticForces.RightY = ForceY;
            }
        }
        public void SetForceZ(double ForceZ, bool IsLeft)
        {
            if (User.IsInControl)
                User.SetOmniForceZ(ForceZ, IsLeft);
            else
            {
                if (IsLeft)
                    HapticForces.LeftZ = ForceZ;
                else
                    HapticForces.RightZ = ForceZ;
            }
        }
        private void fillOmniDDL()
        {
            if (!cb_noOmnisAttached.Checked)
            {
                spLeftOmni.DataSource = GetGeomagicDevices();
                spRightOmni.DataSource = GetGeomagicDevices();
            }
        }
        private string[] GetGeomagicDevices()
        {
            string[] fileNames = new string[1];
            try
            {
                //try both locations
                fileNames = Directory.GetFiles(@"C:\Users\Public\Documents\3DSystems\", "*.config");
                for (int i = 0; i < fileNames.Length; i++)
                {
                    fileNames[i] = Path.GetFileNameWithoutExtension(fileNames[i]);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
            return fileNames;
        }
        public void LogMessage(string msg, string detail, Logging.StatusTypes msgType)
        {
            Logging log = new Logging(msg, detail, logFile, msgType);
            log.Record();
        }
        public void ShowError(string msg, string detail)
        {
            //save error to log
            LogMessage(msg, detail, Logging.StatusTypes.Error);
            //show error on form
            if (msg.Length > 150)
                lbl_Errors.Text = "Error: " + msg.Remove(150);
            else
                lbl_Errors.Text = "Error: " + msg;
            //set timer for displaying error
            errorTimer.Start();
        }
        private void fillAudioDeviceDDL()
        {
            try
            {
                ddl_AudioDevices.DataSource = AudioConference.GetAvailableInputDevices();
                if (ddl_AudioDevices.Items.Count > 0)
                    ddl_AudioDevices.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
        private void sendGrantReq(object emergency)
        {
            bool _emergency = (bool)emergency;
            TcpClient c = new TcpClient();
            try
            {
                c.Connect(IPAddress.Parse(sendGRAddr), controlPort);
                if (c.Connected)
                {
                    using (Stream s = c.GetStream())
                    {
                        byte[] arry = { Convert.ToByte(_emergency) };
                        s.Write(arry, 0, arry.Length);
                        arry = new byte[1];
                        s.Read(arry, 0, arry.Length);
                        if (arry[0] == 1)
                        {
                            //Accepted or Granted
                            if (User.IsInControl)
                            {
                                //Give control
                                switchControl(false);
                            }
                            else
                            {
                                //take control
                                switchControl(true);
                                //stop listening for data
                                //isListeningForData = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("The user denied your request/grant.");
                            btn_ReqControl.Enabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult.Equals(-2146232800))
                    ShowError("Request was not accepted by other user.", "An IO exception was caught after trying to read an accept for passing control. It is assumed that the remote user denied the request.");
                else
                    ShowError(ex.Message, ex.ToString());
            }
        }
        private void switchControl(bool takeControl)
        {
            User.IsInControl = takeControl;
            if (takeControl)
            {
                if (btn_Initialize.Enabled)
                    tb_InControl.Text = "You are in control.";
                else
                {
                    User.FrozenPosition = Surgery.InControlPosition;
                    Freeze();
                }
                Surgery.UserInControl = User;
                tb_InControl.BackColor = Color.Red;
            }
            else
            {
                User.IsFrozen = false;
                //if (User.FrozenPosition != null)
                //    Surgery.InControlPosition = User.FrozenPosition;
                while (Surgery.UserInControl.MyName == User.MyName) { } //Allow for new InControl user to update Surgery
                tb_InControl.Text = Surgery.UserInControl.MyName + " is in control.";
                tb_InControl.BackColor = Color.Green;
            }
            if (!btn_Initialize.Enabled)
                SendFrozen = true; //Always freeze when switching control

            groupBox3.Enabled = !takeControl;
            tb_forces.Enabled = !takeControl;
            btn_zeroForces.Enabled = !takeControl;
            lbl_forceStrength.Enabled = !takeControl;
            trb_forceStrength.Enabled = !takeControl;
            btn_ReqControl.Enabled = !takeControl;
        }
        public void emergencySwitchControl()
        {
            if (User.IsMaster && !User.IsInControl)
            {
                sendGRAddr = Surgery.UserInControl.MyIPAddress;
                Thread t = new Thread(new ParameterizedThreadStart(sendGrantReq));
                t.IsBackground = true;
                t.Start(true);
            }
        }
        public async void sendCmdToCamera(string cmd)
        {
            if (VideoCapture.PTZAddress != "")
            {
                WebRequestHandler handler = new WebRequestHandler();
                handler.Credentials = new NetworkCredential("admin", "admin");
                HttpClient _client = new HttpClient(handler);
                HttpResponseMessage result;
                result = await _client.GetAsync("http://" + VideoCapture.PTZAddress + "/cgi-bin/ptzctrl.cgi?ptzcmd&" + cmd);
                result.EnsureSuccessStatusCode();
            }
        }
        public void ShowVideoFrame(Image<Bgr, Byte> Frame)
        {
            CaptureImageBox.Image = Frame;
        }
        public void Freeze()
        {
            if (User.IsInControl)
                User.IsFrozen = true;
            else
                SocketData.sendToggleFrozen = true;
            if (telSurgeOnly)
            {
                User.FrozenPosition = User.GetOmniPositions();
            }
        }
        public void UnFreeze()
        {
            if (User.IsInControl)
                User.IsFrozen = false;
            else
                SocketData.sendToggleFrozen = true;
        }
        private void forceOmnisToPosition()
        {
            int resumeRange = 2;
            int maxForce = 1;
            OmniPosition differencePos = User.FrozenPosition.Subtract(User.GetOmniPositions());
            while (differencePos.LeftX > resumeRange || differencePos.LeftY > resumeRange || differencePos.LeftZ > resumeRange || differencePos.RightX > resumeRange || differencePos.RightY > resumeRange || differencePos.RightZ > resumeRange)
            {
                OmniPosition Forces = differencePos;
                if (Forces.LeftX > maxForce)
                    Forces.LeftX = maxForce;
                if (Forces.LeftY > maxForce)
                    Forces.LeftY = maxForce;
                if (Forces.LeftZ > maxForce)
                    Forces.LeftZ = maxForce;
                if (Forces.RightX > maxForce)
                    Forces.RightX = maxForce;
                if (Forces.RightY > maxForce)
                    Forces.RightY = maxForce;
                if (Forces.RightZ > maxForce)
                    Forces.RightZ = maxForce;
                User.SetOmniForce(Forces);
                differencePos = User.FrozenPosition.Subtract(User.GetOmniPositions());
            }
            User.SetOmniForce(new OmniPosition());
        }
        private void disconnectClient(User client)
        {
            foreach (ToolStripItem item in ss_Connections.Items)
            {
                if (item.ToolTipText == client.MyIPAddress)
                {
                    ss_Connections.Items.Remove(item);
                    break;
                }
            }
            int indexOfName = lbl_Connections.Text.IndexOf(client.MyName);
            if (indexOfName > -1)
                lbl_Connections.Text = lbl_Connections.Text.Remove(indexOfName, client.MyName.Length);
            Surgery.ConnectedClients.Remove(client);

            if (Surgery.UserInControl.MyIPAddress == client.MyIPAddress)
                switchControl(true);

            if (Surgery.ConnectedClients.Count == 0)
            {
                VideoCapture.IsStreaming = false;
                Markup.IsListeningForMarkup = false;
                lbl_Connections.Text = "Connections: None";
            }
        }
        private int calculatePanTiltDuration(int location, int ff, bool isX) //in ms
        {
            //calculate origin with respect to the capture image box
            Point origin = new Point(Convert.ToInt32(CaptureImageBox.Width / 2), Convert.ToInt32(CaptureImageBox.Height / 2));
            double calcDuration = 0;
            if (isX)
            {
                calcDuration = location - origin.X;
                calcDuration /= CaptureImageBox.Width;
            }
            else
            {
                calcDuration = origin.Y - location;
                calcDuration /= CaptureImageBox.Height;
            }
            calcDuration *= 100;
            return Convert.ToInt32(ff * calcDuration);
        }
        private void moveCamera()
        {
            //Move PTZ camera to center on click location
            int speedX = 10;
            int speedY = 10;
            //Get duration of pan (X)
            int panTime = calculatePanTiltDuration(videoClickPoint.X, 23, true);
            bool turnRight = panTime > 0;
            panTime = Math.Abs(panTime);

            //Get duration of tilt (Y)
            int tiltTime = calculatePanTiltDuration(videoClickPoint.Y, 23, false);
            bool turnUp = tiltTime > 0;
            tiltTime = Math.Abs(tiltTime);

            //move camera
            Stopwatch movePTZWatch = new Stopwatch();
            movePTZWatch.Start();
            sendCmdToCamera((turnRight ? "right" : "left") + "&" + speedX + "&" + speedY); //send start moving
            while (movePTZWatch.ElapsedMilliseconds < panTime) { };
            //Send multiple "STOP" because some do not register
            for (int i = 0; i < 5; i++)
                sendCmdToCamera("ptzstop&3&3"); //send stop command
            movePTZWatch.Restart();
            sendCmdToCamera((turnUp ? "up" : "down") + "&" + speedX + "&" + speedY); //send start moving
            while (movePTZWatch.ElapsedMilliseconds < tiltTime) { };
            for (int i = 0; i < 5; i++)
                sendCmdToCamera("ptzstop&3&3"); //send stop command
            movePTZWatch.Stop();
        }
        //Old Methods        
        /*
        private void disconnect(string ipAddress)
        {
            try
            {
                if (isMaster)
                {
                    //if disconnected client was in control, take control
                    if (inControlIP.Equals(ipAddress))
                    {
                        inControlIsFrozen = true;
                        switchControl(true, myIPAddress);
                    }
                    connectedClients.Remove(ipAddress);
                    foreach (ToolStripItem itm in ss_Connections.Items)
                    {
                        if (itm.ToolTipText != null && itm.ToolTipText.Equals(ipAddress))
                        {
                            ss_Connections.Items.Remove(itm);
                            break;
                        }
                    }
                    if (connectedClients.Count < 1)
                    {
                        lbl_Connections.Text = "Connections: None";
                    }
                }
                else
                {
                    //send disconnect to Master
                    SocketMessage sm = new SocketMessage(myName, myIPAddress);
                    using (TcpClient c = new TcpClient())
                    {
                        c.Connect(IPAddress.Parse(tb_ipAddress.Text), connectionPort);
                        Stream s = c.GetStream();
                        string json = JsonConvert.SerializeObject(sm);
                        byte[] arry = Encoding.ASCII.GetBytes(json);
                        s.Write(arry, 0, arry.Length);
                    }
                }

                logMessage("Successfully disconnected.", "This machine was successfully disconnected with the surgery.", Logging.StatusTypes.Running);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
         
        private void drawZoomingRect()
        {
            while (isZooming)
            {
                Point zoomBoxLoc = new Point();
                Size zoomBoxSize = new Size();
                //Size imageSize = captureImageBox.Image.Size;
                double theta = Math.Atan(videoImgSize.Width / videoImgSize.Height);
                zoomBoxLoc.X = Convert.ToInt32(startZoomPt.X - (zoomingRadius * Math.Sin(theta)));
                zoomBoxLoc.Y = Convert.ToInt32(startZoomPt.Y - (zoomingRadius * Math.Cos(theta)));
                zoomBoxSize.Width = 2 * (startZoomPt.X - zoomBoxLoc.X);
                zoomBoxSize.Height = 2 * (startZoomPt.Y - zoomBoxLoc.Y);
                Rectangle zoomBox = new Rectangle(zoomBoxLoc, zoomBoxSize);
                //draw zoomBox
                Image<Bgr, Byte> img = (Image<Bgr, Byte>)CaptureImageBox.Image;
                img.Draw(zoomBox, new Bgr(Color.Red), 2);
                CaptureImageBox.Image = img;
                //Thread.Sleep(10);
            }
        }
        */

        //Events
        private void captureImageBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                if (!tmpPoints.Contains(e.Location))
                {
                    try
                    {
                        tmpPoints.Add(e.Location);
                        List<Figure> tmpFig = Markup.GetCurrentFigureList(penColor);
                        if (isFirstPointOfFigure)
                        {
                            tmpFig.Add(new Figure(penColor));
                            isFirstPointOfFigure = false;
                        }
                        tmpFig[tmpFig.Count - 1].Path = tmpPoints.ToArray();
                        Markup.SetCurrentFigureList(penColor, tmpFig);
                        
                        if (!User.IsMaster)
                            Markup.SendMarkup(IPAddress.Parse(Surgery.Master.MyIPAddress));
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex.Message, ex.ToString());
                    }
                }
            }
            //else if (!startZoomPt.IsEmpty)
            //{
            //    //calculate zoom radius
            //    int deltaX = Math.Abs(e.X - startZoomPt.X);
            //    int deltaY = Math.Abs(e.Y - startZoomPt.Y);
            //    zoomingRadius = Convert.ToInt32(Math.Sqrt(deltaX * deltaX + deltaY * deltaY));
            //    if (zoomingRadius > 10)
            //    {
            //        if (!isZooming)
            //        {
            //            //start thread to draw zooming box
            //            Thread t = new Thread(new ThreadStart(drawZoomingRect));
            //            t.IsBackground = true;
            //            t.Start();
            //        }
            //        isZooming = true;
            //    }
            //    else
            //    {
            //        isZooming = false;
            //    }
            //}
        }
        private void captureImageBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (VideoCapture.PTZAddress != "")
            {
                if (e.Location.Equals(videoClickPoint) && tmpPoints.Count.Equals(0))
                {
                    Thread moveCamThread = new Thread(new ThreadStart(moveCamera));
                    moveCamThread.IsBackground = true;
                    moveCamThread.Start();
                }
            }
            if (isDrawing)
            {
                try
                {
                    tmpPoints = new List<Point>();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message, ex.ToString());
                }
                //btn_UndoMark.Visible = true;
            }
            isDrawing = false;
            //If camera can be controlled over network and mouse down event occured in captureImageBox
            //if (videoIsPTZ && !startZoomPt.IsEmpty)
            //{
            //    //To center around a point, AXIS 215 PTZ needs: the point, image width, and image height
            //    Point frame = CaptureImageBox.Location;
            //    Point clickRelLoc = new Point(e.Location.X - frame.X + camClickPosFactor.X, e.Location.Y - frame.Y + camClickPosFactor.Y);
            //    //Size imageSize = captureImageBox.Image.Size;
            //    if (isZooming)
            //    {
            //        //Calculate zoom factor
            //        int zoomFactor = Convert.ToInt32(zoomScalingFactor * Math.Sqrt((videoImgSize.Width * videoImgSize.Width + videoImgSize.Height * videoImgSize.Height) / zoomingRadius));
            //        //Send Zoom cammand to camera
            //        startZoomPt.Offset(camZoomPosFactor.X, camZoomPosFactor.Y);
            //        sendCmdToCamera("areazoom=" + startZoomPt.X + "," + startZoomPt.Y + "," + zoomFactor + "&imagewidth=" + videoImgSize.Width + "&imageheight=" + videoImgSize.Height);
            //    }
            //    else
            //    {
            //        //center camera around click
            //        //Send movement command to camera
            //        sendCmdToCamera("center=" + clickRelLoc.X + "," + clickRelLoc.Y + "&imagewidth=" + videoImgSize.Width + "&imageheight=" + videoImgSize.Height);
            //    }
            //    startZoomPt = new Point();
            //    isZooming = false;
            //}
        }
        private void captureImageBox_MouseDown(object sender, MouseEventArgs e)
        {
            if ((User.IsMaster && VideoCapture.IsCapturing) || VideoCapture.IsListeningForVideo)
            {
                isDrawing = true;
                isFirstPointOfFigure = true;
            }
            videoClickPoint = e.Location;
        }
        private void btn_PenColor_Click(object sender, EventArgs e)
        {
            Button tmpBtn = (Button)sender;
            penColor = tmpBtn.BackColor;
        }
        private void btn_ClearMarks_Click(object sender, EventArgs e)
        {
            Markup.MyMarkings.Clear();
            tmpPoints = new List<Point>();
            if (!User.IsMaster)
            {
                SocketMessage sm = new SocketMessage(Surgery, User);
                sm.ClearMarkingsReq = true;
                SocketData.SendUDPDataTo(IPAddress.Parse(Surgery.Master.MyIPAddress), SocketData.SerializeObject<SocketMessage>(sm));
            }
            else
                Markup.ClearMarkingsReq = true;
        }
        private void btn_Capture_Click(object sender, EventArgs e)
        {
            try
            {
                if (VideoCapture.IsCapturing)
                {
                    VideoCapture.StopCapturing();
                    btn_Capture.Text = "Start Capture";
                }
                else
                {
                    VideoCapture.StartCapturing();
                    btn_Capture.Text = "Stop";
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
        private void btn_UndoMark_Click(object sender, EventArgs e)
        {
            
        }
        private void InitializeOmnis_Click(object sender, EventArgs e)
        {
            //Clients must have a name, Master is just called "Master"
            if (!User.IsMaster && tb_InstanceName.Text.Equals(""))
            {
                MessageBox.Show("Please enter a name.");
            }
            else
            {
                bool success = false;
                if( User.HasOmnis )
                {
                    if (spLeftOmni.SelectedIndex.Equals(spRightOmni.SelectedIndex))
                    {
                        MessageBox.Show("Please select two different Omni Devices");
                    }
                    else
                    {
                        //Try to connect to Omnis
                        if (spLeftOmni.Items.Count > 0 && spRightOmni.Items.Count > 0)
                            success = User.InitializeOmnis(spLeftOmni.SelectedItem.ToString(), spRightOmni.SelectedItem.ToString());
                    }
                }
                else
                {
                    //Connect without Omnis
                    success = true;
                }
                if (success)
                {
                    //Only allow one successful connection to Omnis
                    cb_noOmnisAttached.Enabled = false;
                    UnderlyingTimer.Enabled = true;
                    //do not allow switching between master and slave state once omnis are initialized
                    cb_isMaster.Enabled = false;
                    tb_InstanceName.Enabled = false;

                    if (User.IsMaster)
                    {
                        User.MyName = "Master";
                        Surgery.Master = User;
                        //For now, display in control here
                        switchControl(true);
                        Surgery.UserInControl = User;

                        //start listening for clients wanting to connect
                        Thread t = new Thread(new ThreadStart(listenForNewConnections));
                        t.IsBackground = true;
                        t.Start();

                        //start listening for data
                        SocketData.IsListeningForData = true;
                        Thread t2 = new Thread(new ThreadStart(SocketData.ListenForData));
                        t2.IsBackground = true;
                        t2.Start();
                    }
                    else
                    {
                        ConnectToMasterButton.Visible = true;
                        User.MyName = tb_InstanceName.Text;
                    }

                    //Don't show force controls if no omnis attched
                    if (User.HasOmnis)
                    {
                        btn_zeroForces.Visible = false;
                        groupBox3.Visible = false;
                        tb_forces.Visible = false;
                        lbl_forceStrength.Visible = false;
                        trb_forceStrength.Visible = false;
                    }

                    gb_SendingLeft.Visible = true;
                    tb_SendingLeft.Visible = true;
                    gb_SendingRight.Visible = true;
                    tb_SendingRight.Visible = true;
                    btn_Initialize.Enabled = false;
                }
            }
        }
        private void UnderlyingTimerTick(object sender, EventArgs e)
        {
            try
            {
                //Get current position
                OmniPosition currentPos = new OmniPosition();
                if( User.HasOmnis )
                {
                    currentPos = User.GetOmniPositions();
                }
                showOmniPositions(currentPos);
                if (User.IsInControl)
                {
                    Surgery.UserInControl = User;
                    if (telSurgeOnly && User.IsFrozen)
                    {
                        Surgery.InControlPosition = User.FrozenPosition;
                    }
                    else
                        Surgery.InControlPosition = currentPos;
                    if (!User.IsMaster)
                    {
                        //Only send data to Master while InControl
                        SocketData.SendUDPDataTo(IPAddress.Parse(Surgery.Master.MyIPAddress), SocketData.CreateMessageToSend());
                        messageCount++;
                    }
                    //Check for freeze button press
                    if (telSurgeOnly && this.User.CheckForFreeze(currentPos))
                        Freeze();
                    //Display Frozen status
                    if (User.IsFrozen)
                        tb_InControl.Text = "You are frozen.";
                    else
                        tb_InControl.Text = "You are in control!";
                }
                else
                {
                    if (User.IsMaster)
                    {
                        //check for emergency switch
                        if (this.User.CheckForEmergencySwitch(currentPos))
                            emergencySwitchControl();
                    }
                    //Check for following
                    this.User.CheckIfFollowing(currentPos);
                    if (User.IsFollowing)
                        User.OmniFollow(Surgery.InControlPosition);
                    else
                        User.SetOmniForce(new OmniPosition());
                }
                if (User.IsMaster)
                {
                    //Master always sends data
                    if (Surgery.ConnectedClients.Count > 0)
                        SocketData.MasterSendData();
                    //Check for "stay alive" from clients
                    foreach (User u in Surgery.ConnectedClients)
                    {
                        if (DateTime.Now.Subtract(u.LastHeardFrom).Seconds > 30)
                        {
                            disconnectClient(u);
                            break;
                        }
                    }
                }
                else
                {
                    //Send "stay alive to Master
                    if (User.ConnectedToMaster)
                    {
                        if (DateTime.Now.Subtract(User.LastHeardFrom).Seconds > 20)
                        {
                            TcpClient tCPClient = new TcpClient();
                            tCPClient.Connect(IPAddress.Parse(Surgery.Master.MyIPAddress), connectionPort);
                            SocketMessage sm = new SocketMessage(Surgery, User);
                            SocketData.SendTCPDataTo(tCPClient, SocketData.SerializeObject<SocketMessage>(sm));

                            User.LastHeardFrom = DateTime.Now;
                        }
                    }
                }
                //Update and show output data (RobotApp)
                if (Surgery.InControlPosition != null)
                {
                    OutputPosition = Surgery.InControlPosition;
                    showOutputPosition();
                }
            }
            catch (Exception ex)
            {
                UnderlyingTimer.Enabled = false;
                ShowError("An error occured during routine program timer. Application must be reset.", ex.Message + " => " + ex.ToString());
            }
        }
        private void ConnectToMasterButtonClick(object sender, EventArgs e)
        {
            Surgery.Master.MyIPAddress = tb_ipAddress.Text;
            if (User.ConnectToMaster())
            {
                try
                {
                    TcpClient tCPClient = new TcpClient();
                    tCPClient.Connect(IPAddress.Parse(Surgery.Master.MyIPAddress), connectionPort);
                    SocketMessage sm = new SocketMessage(Surgery, User);
                    SocketData.SendTCPDataTo(tCPClient, SocketData.SerializeObject<SocketMessage>(sm));

                    //start listening for master's position
                    SocketData.IsListeningForData = true;
                    Thread t1 = new Thread(new ThreadStart(SocketData.ListenForData));
                    t1.IsBackground = true;
                    t1.Start();

                    VideoCapture.IsListeningForVideo = true;
                    Thread t2 = new Thread(new ThreadStart(VideoCapture.ListenForVideo));
                    t2.IsBackground = true;
                    t2.Start();

                    Markup.IsListeningForMarkup = true;
                    Thread t3 = new Thread(new ThreadStart(Markup.ListenForMarkup));
                    t3.IsBackground = true;
                    t3.Start();

                    Thread t4 = new Thread(new ThreadStart(listenForGrantReq));
                    t4.IsBackground = true;
                    t4.Start();

                    if (User.HasOmnis)
                    {
                        lbl_Connections.Text = "Connected to: ";
                        string dir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"..\..\Content\pc.png");
                        Image pcImg = Image.FromFile(dir);
                        ToolStripItem newItem = new ToolStripButton("Master", pcImg, sendClientGrantReq, "btn_Master");
                        newItem.ToolTipText = tb_ipAddress.Text;
                        ss_Connections.Items.Add(newItem);

                        btn_ReqControl.Enabled = true;
                    }
                    else
                    {
                        lbl_Connections.Text = "Connected to: Master";
                    }

                    lbl_Connections.Visible = true;
                    gb_SendingLeft.Visible = true;
                    tb_SendingLeft.Visible = true;
                    gb_SendingRight.Visible = true;
                    tb_SendingRight.Visible = true;
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message, ex.ToString());
                }
            }

            ConnectToMasterButton.Enabled = !User.ConnectedToMaster;
            tb_ipAddress.Enabled = !User.ConnectedToMaster;
            btn_zeroForces.Enabled = User.ConnectedToMaster;
            groupBox3.Enabled = User.ConnectedToMaster;
            tb_forces.Enabled = User.ConnectedToMaster;
            lbl_forceStrength.Enabled = User.ConnectedToMaster;
            trb_forceStrength.Enabled = User.ConnectedToMaster;
        }
        private void cb_isMaster_CheckedChanged(object sender, EventArgs e)
        {
            User.IsMaster = cb_isMaster.Checked;
            if (User.IsMaster)
            {
                //user wants to be master
                lbl_myIP.Text = "My IP Address";
                tb_ipAddress.Text = User.MyIPAddress;
            }
            else
            {
                lbl_myIP.Text = "Master's IP Address";
                tb_ipAddress.Text = "";
            }
            tb_ipAddress.Enabled = !User.IsMaster;
            //cb_noOmnisAttached.Visible = !isChecked;
            btn_Capture.Visible = User.IsMaster;
        }
        private void cb_noOmnisAttached_CheckedChanged(object sender, EventArgs e)
        {
            User.HasOmnis = !cb_noOmnisAttached.Checked;
            if (User.HasOmnis)
            {
                cb_isMaster.Enabled = true;
                groupBox3.Text = "Forces";
                btn_zeroForces.Enabled = true;
                spLeftOmni.Enabled = true;
                spRightOmni.Enabled = true;
            }
            else
            {
                spLeftOmni.Enabled = false;
                spRightOmni.Enabled = false;
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                User.DisconnectOmnis();
            }
            catch (Exception)
            {

            }
        }
        private void btn_StartAudio_Click(object sender, EventArgs e)
        {
            try
            {
                if (AudioConference.InConference)
                {
                    AudioConference.LeaveConference();
                    btn_StartAudio.BackColor = Color.Green;
                    ddl_AudioDevices.Enabled = true;
                }
                else
                {
                    AudioConference.JoinConference(ddl_AudioDevices.SelectedIndex);
                    btn_StartAudio.BackColor = Color.Red;
                    ddl_AudioDevices.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not start/stop audio. Check input/output device.", ex.Message);
            }
        }
        void errorTimer_Tick(object sender, EventArgs e)
        {
            lbl_Errors.Text = "";
            errorTimer.Enabled = false;
            errorTimer.Stop();
        }
        private void btn_ReqControl_Click(object sender, EventArgs e)
        {
            sendGRAddr = Surgery.UserInControl.MyIPAddress;
            Thread t = new Thread(new ParameterizedThreadStart(sendGrantReq));
            t.IsBackground = true;
            t.Start(false);
            btn_ReqControl.Enabled = false;
        }
        private void sendClientGrantReq(object sender, EventArgs e)
        {
            ToolStripButton btn = (ToolStripButton)sender;
            sendGRAddr = btn.ToolTipText;
            Thread t = new Thread(new ParameterizedThreadStart(sendGrantReq));
            t.IsBackground = true;
            t.Start(false);
        }
        private void networkDelayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetDelay _netDelay;
            if (networkDelay.Equals(0))
                _netDelay = new NetDelay(this, false, networkDelay);
            else
                _netDelay = new NetDelay(this, true, networkDelay);

            _netDelay.Show();
        }
        private void connectButtonsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectButtons cbForm = new ConnectButtons(this);
            cbForm.Show();
        }
        private void changeMyIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeMyIP changeMyIP = new ChangeMyIP(this);
            changeMyIP.Show();
        }
        private void changeVideoSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeVideoSource changeVidSource = new ChangeVideoSource(this);
            changeVidSource.Show();
        }
        private void addIPCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IPCameras ipCamerasForm = new IPCameras();
            ipCamerasForm.Show();
        }
        private void lbl_Zoom_Click(object sender, EventArgs e)
        {

                Label l = (Label)sender;
                int zoom = 0;
                if (l.Text.Equals("+"))
                    zoom = 2000;
                else
                    zoom = -2000;
                sendCmdToCamera("rzoom=" + zoom);

        }
        private void iPCameraControlsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraControl cameraControlForm = new CameraControl(this);
            cameraControlForm.Show();
        }
        private void logDataTurnAroundTime_Click(object sender, EventArgs e)
        {
            if (logDataTurnAroundTime)
                turnAroundTimer.Stop();

            logDataTurnAroundTime = !logDataTurnAroundTime;
            logDataTimesToolStripMenuItem.Checked = logDataTurnAroundTime;
        }
        private void assignButtonsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AssignButtons form = new AssignButtons(this);
            form.ShowDialog();
        }
        private void telSurgeOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            telSurgeOnly = !telSurgeOnly;
            telSurgeOnlyToolStripMenuItem.Checked = telSurgeOnly;
        }

        //Thread Methods
        private void MasterSendVideo()
        {
            VideoCapture.IsStreaming = true;
        }
        private void listenForNewConnections()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Parse(User.MyIPAddress), connectionPort);
                listener.Start();
                while (true)
                {
                    //Accept Incoming Connection
                    TcpClient client = listener.AcceptTcpClient();
                    //get Name and IP of Incoming Connection
                    Stream s = client.GetStream();
                    byte[] arry = new byte[50000];
                    s.Read(arry, 0, arry.Length);
                    SocketMessage sm = SocketData.DeserializeObject<SocketMessage>(arry);
                    if(sm != null)
                    {
                        int connectedUserIndex = -1;
                        for (int i = 0; i < Surgery.ConnectedClients.Count; i++)
                        {
                            if (Surgery.ConnectedClients[i].MyIPAddress.Equals(sm.User.MyIPAddress))
                            {
                                connectedUserIndex = i;
                                break;
                            }
                        }
                        if (connectedUserIndex > -1)
                        {
                            Surgery.ConnectedClients[connectedUserIndex].LastHeardFrom = DateTime.Now;
                        }
                        else
                        {
                            if (lbl_Connections.Text.Equals("Connections: None"))
                                lbl_Connections.Text = "Connections: ";

                            if (!sm.User.HasOmnis)
                            {
                                //If client does not have Omnis, don't allow control to be given
                                lbl_Connections.Text += sm.User.MyName;
                            }
                            else
                            {
                                string dir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"..\..\Content\pc.png");
                                Image clientImg = Image.FromFile(dir);
                                ToolStripItem newItem = new ToolStripButton(sm.User.MyName, clientImg, sendClientGrantReq, "btn_" + sm.User.MyName);
                                newItem.ToolTipText = sm.User.MyIPAddress;
                                ss_Connections.Items.Add(newItem);
                            }

                            if (Surgery.ConnectedClients.Count == 0)
                            {
                                Thread sendVideoThread = new Thread(new ThreadStart(MasterSendVideo));
                                sendVideoThread.IsBackground = true;
                                sendVideoThread.Start();

                                Markup.IsListeningForMarkup = true;
                                Thread listenForNewMarkings = new Thread(new ThreadStart(Markup.ListenForMarkup));
                                listenForNewMarkings.IsBackground = true;
                                listenForNewMarkings.Start();

                                Thread listenForControlReq = new Thread(new ThreadStart(listenForGrantReq));
                                listenForControlReq.IsBackground = true;
                                listenForControlReq.Start();

                                //Thread readFromDataBuffer = new Thread(new ThreadStart(readDataBuffer));
                                //readFromDataBuffer.IsBackground = true;
                                //readFromDataBuffer.Start();
                            }
                            Surgery.ConnectedClients.Add(sm.User);

                            //Log Connection
                            LogMessage("Connection successfully made to " + sm.User.MyName + ".", "A client successfully connected to this machine with the name " + sm.User.MyName + " and address " + sm.User.MyIPAddress + " .", Logging.StatusTypes.Running);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
        private void listenForGrantReq()
        {
            try
            {
                if (grantReqListener == null)
                {
                    grantReqListener = new TcpListener(IPAddress.Parse(User.MyIPAddress), controlPort);
                    grantReqListener.Start();
                }
                TcpClient client = grantReqListener.AcceptTcpClient();
                IPEndPoint remoteEP = (IPEndPoint)client.Client.RemoteEndPoint;

                Thread t = new Thread(new ThreadStart(listenForGrantReq));
                t.IsBackground = true;
                t.Start();

                //check if emergency switch
                int emergBit = 0;
                Stream s = client.GetStream();
                byte[] arry = new byte[1];
                s.Read(arry, 0, arry.Length);
                emergBit = arry[0];

                if (emergBit.Equals(0))
                {
                    //normal switch procedure
                    if (User.IsInControl)
                    {
                        DialogResult res = MessageBox.Show("Grant control?", remoteEP.Address.ToString() + " has requested control.", MessageBoxButtons.YesNo);
                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            //send grant
                            arry = new byte[1] { 1 };
                            s.Write(arry, 0, arry.Length);

                            switchControl(false);
                        }
                        else
                        {
                            //send Deny
                            arry = new byte[1] { 0 };
                            s.Write(arry, 0, arry.Length);
                        }
                    }
                    else
                    {
                        DialogResult res = MessageBox.Show("Accept control?", remoteEP.Address.ToString() + " wants to give you control.", MessageBoxButtons.YesNo);
                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            //send accept
                            arry = new byte[1] { 1 };
                            s.Write(arry, 0, arry.Length);

                            //take control
                            switchControl(true);
                            //stop listening for position
                            //isListeningForData = false;
                        }
                        else
                        {
                            //send decline
                            arry = new byte[1] { 0 };
                            s.Write(arry, 0, arry.Length);
                        }

                    }
                }
                else
                {
                    //emergency switch procedure
                    arry = new byte[1] { 1 };
                    s.Write(arry, 0, arry.Length);

                    switchControl(false);
                }
                s.Close();
                btn_ReqControl.Enabled = true;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message, ex.ToString());
            }
        }
    }
}
