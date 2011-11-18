using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices; 
using System.Windows.Forms;
using System.Diagnostics; 
using System.Threading;
using KinectNUI.Business.Kinect;
using Microsoft.Research.Kinect.Nui;
using System.Security.Permissions;
using KinectNUI.Presentation.KinectUI;

namespace KinectNUI.Presentation.KinectUI
{

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]

	public partial class MainWindow : Window
	{
		public MainWindow()
		{ InitializeComponent(); }

		#region Properties and Globals

		private Runtime nui = new Runtime(); // All Kinect communication goes through this object
		//private PieMenu pieMenu = null; // Master Pie Menu. Will have submenus as child objects.
		private GestureHandler handler; // Handles gestures

        private SpeechHandler speechhandler;//Handles Speech

		private DateTime lastTime = DateTime.MinValue; // Used to calculate FPS
		private byte[] depthFrame32 = new byte[320 * 240 * 4]; // How we get 3D spatial information
		private int totalFrames = 0, lastFrames = 0; // Used to calculate FPS

		#endregion Properties and Globals

		#region Event Handlers
        [MTAThread]
		/// <summary>Fires when the window loads.</summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            wbMain.Navigate(new Uri("C:/Users/t-ravir/Desktop/test.html"));
			// Inits
			handler = new GestureHandler(this, 30);
			handler.onMessage += new Message(handler_onMessage);
            //handler.onClearPies += new ClearPies(handler_onClearPies);
			//handler.onSpawnPie += new SpawnPie(handler_onSpawnPie);
			//handler.onPullPie += new PullPie(handler_onPullPie);
			
			AddMessage("Starting up");
			AddMessage("Joint histories initialized.");

			// Initialize the Kinect connection
			nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor | RuntimeOptions.UseDepth);
			nui.SkeletonEngine.TransformSmooth = true;
			AddMessage("Kinect runtime initialized.");
			nui.NuiCamera.ElevationAngle = 6;
			AddMessage("Kinect camera elevation set to " + nui.NuiCamera.ElevationAngle.ToString());

			// Wire up event handlers
			nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
			nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
			nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
			AddMessage("Event handlers wired up.");

			// Connect to the streams
			nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
			nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.Depth);
			AddMessage("Kinect Streams are open on device " + nui.NuiCamera.UniqueDeviceName);
		}
		protected void handler_onMessage(string message)
		{
            //this.wbMain.InvokeScript("plusZoom");
			AddMessage(message);
		}
		/// <summary>Fires when a skeleton frame is ready.</summary>
		protected void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
		{
			// Colors for each joint on the skeleton view
			Dictionary<JointID, Brush> jointColors = new Dictionary<JointID, Brush>() { 
				{JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
				{JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
				{JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
				{JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
				{JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
				{JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
				{JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
				{JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
				{JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
				{JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
				{JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
				{JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
				{JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
				{JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
				{JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
				{JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
				{JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
				{JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
				{JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
				{JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))} };

			// Prepare to draw skeleton
			SkeletonFrame skeletonFrame = e.SkeletonFrame;
			int iSkeleton = 0;
			Brush[] brushes = new Brush[6];
			brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
			brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
			brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
			brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
			brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
			brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));

			// Draw skeleton
			skeleton.Children.Clear();
			foreach (SkeletonData data in skeletonFrame.Skeletons) {
				if (SkeletonTrackingState.Tracked == data.TrackingState | SkeletonTrackingState.PositionOnly == data.TrackingState) {
					// Draw bones
					Brush brush = brushes[iSkeleton % brushes.Length];
					skeleton.Children.Add(util.getBodySegment(data.Joints, brush, ref nui, ref skeleton, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
					skeleton.Children.Add(util.getBodySegment(data.Joints, brush, ref nui, ref skeleton, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
					skeleton.Children.Add(util.getBodySegment(data.Joints, brush, ref nui, ref skeleton, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
					skeleton.Children.Add(util.getBodySegment(data.Joints, brush, ref nui, ref skeleton, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
					skeleton.Children.Add(util.getBodySegment(data.Joints, brush, ref nui, ref skeleton, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));
					
					// Draw joints
					foreach (Joint joint in data.Joints) {
						Point jointPos = util.getDisplayPosition(joint, ref nui, ref skeleton);
						Line jointLine = new Line();
						jointLine.X1 = jointPos.X - 3;
						jointLine.X2 = jointLine.X1 + 6;
						jointLine.Y1 = jointLine.Y2 = jointPos.Y;
						jointLine.Stroke = jointColors[joint.ID];
						jointLine.StrokeThickness = 6;
						skeleton.Children.Add(jointLine);
						handler.AppendJointHistory(joint); } }
				iSkeleton++; } // for each skeleton

			// If Gestures are turned on, detect them. Else, only detect the "Enable gestures" gesture.
            /*if (handler.isPanLeft == true)
            {
                AddMessage("Pan Left is True!");
                handler.DetectPanRight();
            }
            else*/ if (handler.isGesturesEnabled) handler.DetectGestures();
			else handler.DetectGestureMulti_EnableGestures();
		}
		/// <summary>Fires when a depth frame is ready.</summary>
		protected void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
		{
			// Convert depth frame to video frame to render it
			PlanarImage Image = e.ImageFrame.Image;
			byte[] convertedDepthFrame = util.convertDepthFrame(Image.Bits, ref depthFrame32);
			image2.Source = BitmapSource.Create(Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);

			// Clear extraneous canvas elements (bones)
			if (canvas1.Children.Count > 9 && handler.JointHistory[(int)JointID.HandLeft].Count > 0 && handler.JointHistory[(int)JointID.HandRight].Count > 0)
				canvas1.Children.RemoveRange(9, canvas1.Children.Count - 9);

			// Draw hand tracking lines and circle
			/*if (handler.JointHistory[(int)JointID.HandLeft].Count > 0)
				DrawCircle(handler.JointHistory[(int)JointID.HandLeft].Last());
			if (handler.JointHistory[(int)JointID.HandRight].Count > 0)
				DrawCircle(handler.JointHistory[(int)JointID.HandRight].Last());
			for (int i = 0; i < handler.JointHistory[(int)JointID.HandLeft].Count - 1; i++)
				DrawLine(handler.JointHistory[(int)JointID.HandLeft][i], handler.JointHistory[(int)JointID.HandLeft][i + 1]);
			for (int i = 0; i < handler.JointHistory[(int)JointID.HandRight].Count - 1; i++)
				DrawLine(handler.JointHistory[(int)JointID.HandRight][i], handler.JointHistory[(int)JointID.HandRight][i + 1]);*/

			// Calculate FPS
			++totalFrames;
			if (lastTime < DateTime.Now.AddSeconds(-1)) {
				int frameDiff = totalFrames - lastFrames;
				lastFrames = totalFrames;
				lastTime = DateTime.Now;
				Title = "KinectNUI - " + frameDiff.ToString() + " FPS"; }
		}
		/// <summary>Fires when a video frame is ready.</summary>
		protected void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
		{
			// Convert video from a useless planar format to a useful bitmap format we can actually display
			PlanarImage image = e.ImageFrame.Image;
			image1.Source = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null, image.Bits, image.Width * image.BytesPerPixel);
		}
		/// <summary>Fires when the gesture handler spawns a pie menu.</summary>
		/// <param name="parent">Parent pie menu, or null for origin</param>
		/// <param name="newChild">New child menu to spawn</param>
		/*void handler_onSpawnPie(PieMenu parent, PieMenu newChild)
		{
			if (parent == null)
				parent = pieMenu;
			if (parent == null)
			{
				pieMenu = PieMenuFactory.BuildPie(this);
				pieMenu.Show();
			}
			else
			{// crawl the tree of piemenus and find that one
				bool hasFound = false;
				foreach (PieMenu menu in parent.subMenus)
					if (menu.guid == parent.guid)
					{
						menu.AddSubmenu(newChild);
						hasFound = true;
						menu.subMenus.Last().Show();
					}
				if (!hasFound)
					foreach (PieMenu menu in parent.subMenus)
						handler_onSpawnPie(menu, newChild);
			}
		}
		/// <summary>Fires when the gesture handler says to clear all pie menus</summary>
		void handler_onClearPies()
		{
			// TODO: Get pie menu result
			if (pieMenu == null) return;
			pieMenu.Close();
			pieMenu = null;
		}
		/// <summary>Fires when the gesture handler detects a pie menu selection was selected</summary>
		/// <returns>PieMenu which was selected</returns>
		PieMenu handler_onPullPie()
		{
			// Get selected pie
			if (pieMenu == null) return null;
			else return pieMenu.GetSelectedPie();
		}*/
		/// <summary>When we close the window, make sure we close the Kinect stream. Be a good code citizen.</summary>
		/// <remarks>One would expect this to turn off the depth sensor. Alas, it does not.</remarks>
		private void Window_Closed(object sender, EventArgs e)
		{
			nui.Uninitialize();
			System.Windows.Application.Current.Shutdown();
		}

		#endregion Event Handlers
		#region Helper Methods

		/// <summary>Draws a circle (used for hand tracking)</summary>
		/// <param name="joint">Joint around which to draw a circle. Appears directly on the window.</param>
		/*private void DrawCircle(Joint joint)
		{
			Ellipse e = new Ellipse();
			e.Stroke =
				joint.ID == JointID.HandRight ? Brushes.LightSteelBlue
				: joint.ID == JointID.HandLeft ? Brushes.DarkBlue
				: Brushes.Black;
			e.StrokeThickness = 1 + Math.Pow(joint.Position.Z, 5);
			e.Width = e.StrokeThickness * 10;
			e.Height = e.StrokeThickness * 10;
			
			// Add the circle to the page directly, and set its position relative to the hand's spatial information
			canvas1.Children.Add(e);
			Canvas.SetLeft(e, util.calcLeft(canvas1, ref joint));
			Canvas.SetTop(e, util.calcTop(canvas1, ref joint));
		}*/
		/// <summary>Draws a line (used for hand tracking)</summary>
		/// <param name="j1">Joint to start the line at (old joint)</param>
		/// <param name="j2">Joint to end the line at (new joint)</param>
		/*private void DrawLine(Joint j1, Joint j2)
		{
			Line line = new Line();
			line.X1 = util.calcLeft(canvas1, ref j1) + (1 + Math.Pow(j1.Position.Z, 5) * 10 / 2);
			line.Y1 = util.calcTop(canvas1, ref j1) + (1 + Math.Pow(j1.Position.Z, 5) * 10 / 2);
			line.X2 = util.calcLeft(canvas1, ref j2) + (1 + Math.Pow(j2.Position.Z, 5) * 10 / 2);
			line.Y2 = util.calcTop(canvas1, ref j2) + (1 + Math.Pow(j2.Position.Z, 5) * 10 / 2);
			line.Stroke =
				j1.ID == JointID.HandRight ? Brushes.LightSteelBlue // light blue for right hand
				: j1.ID == JointID.HandLeft ? Brushes.DarkBlue		// dark  blue for left hand
				: Brushes.Black;									// black for any other joint
			line.StrokeThickness = 1 + Math.Pow(j1.Position.Z, 5);	// Exponentially thicker lines for further-out Z distances
			canvas1.Children.Add(line);
		}*/

		/// <summary>Adds a message to the debug output textbox.</summary>
		/// <param name="message">Message to add</param>
		private void AddMessage(string message)
		{
			if (textBox1.Text.Length > 100000)
				textBox1.Text = textBox1.Text.Substring(textBox1.Text.Length - 100000);
			textBox1.Text += message.Trim() + "\r\n";
			textBox1.SelectionStart = textBox1.Text.Length;
			textBox1.ScrollToEnd();
		}

		#endregion Helper Methods
		#region DllImports

		/*[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern int SendInput(int nInputs, ref util.INPUT mi, int cbSize);
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(IntPtr hWnd, int CmdShow);
		[DllImport("user32.dll")]
		static extern bool GetWindowPlacement(IntPtr hWnd, ref util.WINDOWPLACEMENT lpwndl);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, util.SetWindowPosFlags uFlags);
		[DllImport("user32.dll")]
		public static extern bool GetCursorPos(ref Point pt);
		[DllImport("user32.dll")]
		static extern IntPtr WindowFromPoint(util.POINT Point);
		[DllImport("user32.dll")]
		static extern bool SetCursorPos(int X, int Y);
		[DllImport("user32.dll")]
		static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hwnd, out util.RECT lpRect);*/

		#endregion DllImports
		#region Event declarations

		public delegate void Message(string message);
		public delegate void SpawnPie(PieMenu parent, PieMenu newChild);
		public delegate void ClearPies();
		public delegate PieMenu PullPie();
		
		#endregion Event declarations

        [MTAThread]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //speechhandler = new SpeechHandler(this);
            System.Windows.MessageBox.Show(Thread.CurrentThread.GetApartmentState().ToString());
            SpeechHandler.listen();
            //speechhandler = new EventHandler<SpeechHandler>(KinectNUI.Presentation.KinectUI.SpeechHandler.listen);
            //handler_onMessage("Button CLicked!");
        }
	}
}
