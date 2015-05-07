//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.IO.Ports;


    //batman
    //using System;
    using System.Diagnostics;

    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    //robin

    using System;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        //batman

        /// <summary>
        /// The Arduino SerialPort object
        /// </summary>
        private SerialPort _arduinoSerialPort;

        private int counter = 0;
        double[] JointPos = new double[24];
        String[] Labels = { "LE", "RE", "LW", "RW", "LK", "RK", "LA", "RA" };

        /// <summary>
        /// The DateTime object that tracks the last write time to the serial port.
        /// I had trouble with saturating the serial port with data (at least, that's what
        /// I suspected) so this is just an easy way to limit data transfer
        /// </summary>
        private DateTime _serialPortLastWriteTime = DateTime.Now;

        /// <summary>
        /// This sets the maximum write interval on the serial port. It's currently set to 200ms (i.e.,
        /// 5 writes a second or 5Hz). The Kinect Sensor works at 30 fps (30Hz) so we lose a bit of data
        /// (and I don't do any smoothing; I just ignore data outside of the 5Hz rate).
        /// </summary>
        private TimeSpan _serialPortMaxWriteInterval = TimeSpan.FromMilliseconds(200);

        //robin


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //batman
            // Initialize the serial port object to communicate with the Arduino
            // Note: you will have to change the COM7 value to whatever port value your
            // Arduino happens to exist at on your computer
            _arduinoSerialPort = new SerialPort("COM7", 9600);

            // Open the Serial Port
            //_arduinoSerialPort.Open();

            // This is an unnecessary but helpful piece of code. I am hooking up an event handler
            // to call the method OnArduinoSerialPortDataReceived whenever data is received on the 
            // serial port. In other words, if the Arduino writes data on the serial port, this method
            // will get called (this is useful for debugging--so you can see the 'Serial.print' results from
            // Arduino on the Output window in the Visual Studio debugger).
            //_arduinoSerialPort.DataReceived += new SerialDataReceivedEventHandler(OnArduinoSerialPortDataReceived);

            //robin
        }

        //batman

        /// <summary>
        /// Prints out data received from the Arduino to the Output console in Visual Studio
        /// </summary>
        
        private void OnArduinoSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string line = _arduinoSerialPort.ReadLine();
            Trace.WriteLine("Rcvd from Arduino: " + line);
        }
        //robin


        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        WriteLimbTrackingStatesToArduino(skel);

                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }


        //batman
        // This is the code that I wrote (Phil) for finding the various limb vectors.
        private void WriteLimbTrackingStatesToArduino(Skeleton skel)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("10.109.89.244");
            IPEndPoint ep = new IPEndPoint(broadcast, 5000);

            Joint ShoulderCenter = skel.Joints[JointType.ShoulderCenter];
            Joint HipCenter = skel.Joints[JointType.HipCenter];

            if( ShoulderCenter.Position.X != 0 && HipCenter.Position.X != 0) 
            {
                int index = 0;
                int period = 10;

                Joint LeftElbow = skel.Joints[JointType.ElbowLeft];
                JointPos[index++] += (ShoulderCenter.Position.X - LeftElbow.Position.X)/period;
                JointPos[index++] += (ShoulderCenter.Position.Y - LeftElbow.Position.Y)/period;
                JointPos[index++] += (ShoulderCenter.Position.Z - LeftElbow.Position.Z)/period;

                Joint RightElbow = skel.Joints[JointType.ElbowRight];
                JointPos[index++] += (ShoulderCenter.Position.X - RightElbow.Position.X)/period;
                JointPos[index++] += (ShoulderCenter.Position.Y - RightElbow.Position.Y)/period;
                JointPos[index++] += (ShoulderCenter.Position.Z - RightElbow.Position.Z)/period;

                Joint LeftWrist = skel.Joints[JointType.WristLeft];
                JointPos[index++] += (ShoulderCenter.Position.X - LeftWrist.Position.X)/period;
                JointPos[index++] += (ShoulderCenter.Position.Y - LeftWrist.Position.Y)/period;
                JointPos[index++] += (ShoulderCenter.Position.Z - LeftWrist.Position.Z)/period;

                Joint RightWrist = skel.Joints[JointType.WristRight];
                JointPos[index++] += (ShoulderCenter.Position.X - RightWrist.Position.X)/period;
                JointPos[index++] += (ShoulderCenter.Position.Y - RightWrist.Position.Y)/period;
                JointPos[index++] += (ShoulderCenter.Position.Z - RightWrist.Position.Z)/period;

                Joint LeftKnee = skel.Joints[JointType.KneeLeft];
                JointPos[index++] += (HipCenter.Position.X - LeftKnee.Position.X)/period;
                JointPos[index++] += (HipCenter.Position.Y - LeftKnee.Position.Y)/period;
                JointPos[index++] += (HipCenter.Position.Z - LeftKnee.Position.Z)/period;

                Joint RightKnee = skel.Joints[JointType.KneeRight];
                JointPos[index++] += (HipCenter.Position.X - RightKnee.Position.X)/period;
                JointPos[index++] += (HipCenter.Position.Y - RightKnee.Position.Y)/period;
                JointPos[index++] += (HipCenter.Position.Z - RightKnee.Position.Z)/period;

                Joint LeftAnkle = skel.Joints[JointType.AnkleLeft];
                JointPos[index++] += (HipCenter.Position.X - LeftAnkle.Position.X)/period;
                JointPos[index++] += (HipCenter.Position.Y - LeftAnkle.Position.Y)/period;
                JointPos[index++] += (HipCenter.Position.Z - LeftAnkle.Position.Z)/period;

                Joint RightAnkle = skel.Joints[JointType.AnkleRight];
                JointPos[index++] += (HipCenter.Position.X - RightAnkle.Position.X)/period;
                JointPos[index++] += (HipCenter.Position.Y - RightAnkle.Position.Y)/period;
                JointPos[index++] += (HipCenter.Position.Z - RightAnkle.Position.Z)/period;

                counter++;
                if( counter % period == 0)
                    OutputCoordinates(s, ep);
            }
        }

        void OutputCoordinates(Socket s, IPEndPoint ep)
        {
                int precision = 3;
                String message = "";

                int size = 12;// pos.Length;

                for (int i = 0; i < size; i++)
                {
                    if( i%3 == 0)
                        message += Labels[i/3] + ",";

                    message += String.Format("{0:F" + precision + "}", JointPos[i]);
                    if ((i + 1) % 3 == 0)
                        message += ":";
                    else
                        message += ",";
                }

                ResetPosArray();
                
                byte[] sendbuf = Encoding.ASCII.GetBytes(message);
                s.SendTo(sendbuf, ep);
                Trace.WriteLine("Message sent to the broadcast address: " + message);
            
        }

        void ResetPosArray()
        {
            for (int i = 0; i < JointPos.Length; i++)
                JointPos[i] = 0;
        }

        /// <summary>
        /// Writes a single digit byte value from 0-9 based on the right hand position in the Kinect sensing
        /// space. If the right hand is all the way left, the function writes 0 to the serial port. If the
        /// right hand is all the way to the right, the function writes 9 to the serial port. Everything
        /// else is linearly interpolated.
        /// 
        /// Just a simple function to show how the skeleton position can be used to control Arduino
        /// </summary>
        /// <param name="skel"></param>
        private void WriteSkeletonTrackingStateToArduino(Skeleton skel)
        {

            Joint headJoint = skel.Joints[JointType.Head];
            BoneOrientation headBoneOrientation = skel.BoneOrientations[JointType.Head];

            if (headJoint.TrackingState == JointTrackingState.Tracked)
            {
                //Console.WriteLine("headJoint.Position: ({0}, {1}, {2}) headBoneOrientation.AbsoluteRotation: {3}",
                //    headJoint.Position.X, headJoint.Position.Y, headJoint.Position.Z, headBoneOrientation.AbsoluteRotation);
            }

            Joint handRightJoint = skel.Joints[JointType.HandRight];

            if (handRightJoint.TrackingState == JointTrackingState.Tracked)
            {
                // Convert the right hand position in the world to the screen. We will use this
                // to track how far along 
                Point handRightPositionOnScreen = this.SkeletonPointToScreen(handRightJoint.Position);

                //UNCOMMENT THIS LINE IF YOU WANT TO SEE MORE INFO ON THE JOINT OBJECT. I ALSO SUGGEST
                //SETTING BREAKPOINTS TO INVESTIGATE THE OBJECTS USING THE DEBUGGER (E.G., THE WATCH WINDOW)
                //Console.WriteLine("handRightJoint.Position: ({0}, {1}, {2}) OnScreen: ({3}, {4}) OnScreen %: ({5:0.0}%, {6:0.0}%)",
                //    handRightJoint.Position.X, handRightJoint.Position.Y, handRightJoint.Position.Z,
                //    handRightPositionOnScreen.X, handRightPositionOnScreen.Y,
                //    (handRightPositionOnScreen.X / 640f) * 100, (handRightPositionOnScreen.Y / 480f) * 100);

                // Check to make sure enough time has past since the last serial write
                if (DateTime.Now - _serialPortLastWriteTime > _serialPortMaxWriteInterval)
                {
                    // We discretize the x-position of the hand location into 9 equal bins (0-9)
                    // (for this example, we ignore the depth and y position of the hand).
                    // I divide by 640 because I know my depth image is 640x480 (see the XAML
                    // declaration in MainWindow.xaml).
                    byte handPositionVal = (byte)Math.Round((handRightPositionOnScreen.X / 640f) * 9);

                    // The SerialPort.Write method takes a byte array so convert the byte
                    // to this format
                    byte[] byteArray = new byte[1];
                    byteArray[0] = handPositionVal;
                    _arduinoSerialPort.Write(byteArray, 0, 1);

                    // Just some debugging
                    Trace.WriteLine(String.Format("Sent to Arduino: {0}", handPositionVal));

                    // Timestamp for last write time
                    _serialPortLastWriteTime = DateTime.Now;
                }
            }
        }
        //robin


        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
    }
}