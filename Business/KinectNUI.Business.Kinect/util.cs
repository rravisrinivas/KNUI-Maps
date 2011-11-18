using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Runtime.InteropServices; // win32
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using Microsoft.Research.Kinect.Nui; // The big tamale

namespace KinectNUI.Business.Kinect
{
	public static class util
	{
		#region Conversions

		/// <summary>Calculates top position for drawing an object on the window</summary>
		/// <param name="joint">Joint to calculate</param>
		/// <returns>Top location</returns>
		public static double calcTop(Canvas canvas1, ref Joint joint)
		{
			return (canvas1.Height / 2) - (joint.Position.Y * 1000);
		}
		/// <summary>Calculates left position for drawing an object on the window</summary>
		/// <param name="joint">Joint to calculate</param>
		/// <returns>Left location</returns>
		public static double calcLeft(Canvas canvas1, ref Joint joint)
		{
			return (canvas1.Width / 2) + (joint.Position.X * 1000);
		}
		/// <summary>Converts meters to inches.</summary>
		/// <param name="meters">Number of meters</param>
		/// <returns>Number of inches</returns>
		public static double MetersToInches(double meters)
		{
			return meters * 39.37;
		}
		/// <summary>Converts inches to meters.</summary>
		/// <param name="inches">Number of inches</param>
		/// <returns>Number of meters</returns>
		public static double InchesToMeters(double inches)
		{
			return inches / 39.37;
		}
		/// <summary>Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame that displays different players in different colors</summary>
		/// <param name="depthFrame16">16-bit grayscale depth data</param>
		/// <returns>32-bit frame, supposedly with colorized players</returns>
		public static byte[] convertDepthFrame(byte[] depthFrame16, ref byte[] depthFrame32)
		{	
			// TODO: Surely this method is where the LSD Rainbow bug is coming from. Does it think every depth level is a player?
			int GREEN_IDX = 1, RED_IDX = 2, BLUE_IDX = 0;

			// For each player, from what I can tell.
			for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16 += 2, i32 += 4)
			{
				/* player values (Determined through trial and error)
					0 = noise / void / gray		5 = pink
					1 = red						6 = purple
					2 = green					7 = very dark gray
					3 = blue (teal, really)		8 = pitch black
					4 = yellow					Math = LSD Rainbow!
					But it makes the WHOLE CANVAS that color instead of just the player. */

				// Get the player color, and transform 13-bit depth information into an 8-bit intensity appropriate for display (we disregard information in most significant bit)
				int
					player = depthFrame16[i16] & 0x07,
					realDepth = (depthFrame16[i16 + 1] << 5) | (depthFrame16[i16] >> 3);
				byte intensity = (byte)(255 - (255 * realDepth / 0x0fff));

				depthFrame32[i32 + RED_IDX] = 0;
				depthFrame32[i32 + GREEN_IDX] = 0;
				depthFrame32[i32 + BLUE_IDX] = 0;

				// choose different display colors based on player
				switch (player)
				{
					case 0:
						depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
						depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
						depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 2);
						break;
					case 1:
						depthFrame32[i32 + RED_IDX] = intensity;
						break;
					case 2:
						depthFrame32[i32 + GREEN_IDX] = intensity;
						break;
					case 3:
						depthFrame32[i32 + RED_IDX] = (byte)(intensity / 4);
						depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
						depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
						break;
					case 4:
						depthFrame32[i32 + RED_IDX] = (byte)(intensity);
						depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
						depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 4);
						break;
					case 5:
						depthFrame32[i32 + RED_IDX] = (byte)(intensity);
						depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 4);
						depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
						break;
					case 6:
						depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
						depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
						depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
						break;
					case 7:
						depthFrame32[i32 + RED_IDX] = (byte)(255 - intensity);
						depthFrame32[i32 + GREEN_IDX] = (byte)(255 - intensity);
						depthFrame32[i32 + BLUE_IDX] = (byte)(255 - intensity);
						break;
				}
			}
			return depthFrame32;
		}

		#endregion Conversions
		#region Other Utilities

		/// <summary>Calculates the geometry of a chunk of a skeleton</summary>
		/// <param name="joints">List of joints</param>
		/// <param name="brush">Brush to render with</param>
		/// <param name="ids">Which joints to make a segment out of</param>
		/// <returns>PolyLine object representing the desired chunk/segment.</returns>
		public static Polyline getBodySegment(JointsCollection joints, Brush brush, ref Runtime nui, ref Canvas skeleton, params JointID[] ids)
		{
			PointCollection points = new PointCollection(ids.Length);
			for (int i = 0; i < ids.Length; ++i)
				points.Add(getDisplayPosition(joints[ids[i]], ref nui, ref skeleton));

			Polyline polyline = new Polyline();
			polyline.Points = points;
			polyline.Stroke = brush;
			polyline.StrokeThickness = 5;
			return polyline;
		}
		/// <summary>Determines where to draw a joint on the skeleton canvas.</summary>
		/// <param name="joint">Joint to determine the location of</param>
		/// <returns>Point representing where on the canvas to put the joint.</returns>
		public static Point getDisplayPosition(Joint joint, ref Runtime nui, ref Canvas skeleton)
		{
			float depthX, depthY;
			nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
			depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
			depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
			int colorX, colorY;
			ImageViewArea iv = new ImageViewArea();
			// only ImageResolution.Resolution640x480 is supported at this point
			// TODO: This may be the cause of the LSD Rainbow bug.
			nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

			// map back to skeleton.Width & skeleton.Height
			return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
		}
		/// <summary>Gets distance with a player index.</summary>
		/// <param name="firstFrame">One frame from the depth stream</param>
		/// <param name="secondFrame">The immediately proceeding frame after the first one you passed in</param>
		/// <returns>Distance from the sensor; I suspect in meters</returns>
		[Obsolete("Nothing seems to be using this")]
		public static int GetDistanceWithPlayerIndex(byte firstFrame, byte secondFrame)
		{	//offset by 3 in first byte to get value after player index 
			int distance = (int)(firstFrame >> 3 | secondFrame << 5);
			return distance;
		}

		#endregion Other Utilities
		#region Structs

		public struct WINDOWPLACEMENT {
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public System.Drawing.Rectangle rcNormalPosition; }
		[StructLayout(LayoutKind.Sequential)]
		public struct INPUT {
			public int type;
			public INPUTUNION union; };
		[StructLayout(LayoutKind.Explicit)]
		public struct INPUTUNION {
			[FieldOffset(0)]
			public MOUSEINPUT mouseInput;
			[FieldOffset(0)]
			public KEYBDINPUT keyboardInput; };
		[StructLayout(LayoutKind.Sequential)]
		public struct MOUSEINPUT {
			public int dx;
			public int dy;
			public int mouseData;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo; };
		[StructLayout(LayoutKind.Sequential)]
		public struct KEYBDINPUT {
			public short wVk;
			public short wScan;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo; };
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT {
			public int X;
			public int Y;

			public POINT(int x, int y) {
				this.X = x;
				this.Y = y; }

			// These implicit casts do not seem to work.
			public static implicit operator System.Drawing.Point(POINT p) {
				return new System.Drawing.Point(p.X, p.Y); }
			public static implicit operator POINT(System.Drawing.Point p) {
				return new POINT(p.X, p.Y); } }
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT {
			public int _Left;
			public int _Top;
			public int _Right;
			public int _Bottom; }

		#endregion Structs
		#region Enums

		[Flags()]
        public enum SetWindowPosFlags : uint
        {
            /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, 
            /// the system posts the request to the thread that owns the window. This prevents the calling thread from 
            /// blocking its execution while other threads process the request.</summary>
            /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
            SynchronousWindowPosition = 0x4000,
            /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
            /// <remarks>SWP_DEFERERASE</remarks>
            DeferErase = 0x2000,
            /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
            /// <remarks>SWP_DRAWFRAME</remarks>
            DrawFrame = 0x0020,
            /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to 
            /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE 
            /// is sent only when the window's size is being changed.</summary>
            /// <remarks>SWP_FRAMECHANGED</remarks>
            FrameChanged = 0x0020,
            /// <summary>Hides the window.</summary>
            /// <remarks>SWP_HIDEWINDOW</remarks>
            HideWindow = 0x0080,
            /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the 
            /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter 
            /// parameter).</summary>
            /// <remarks>SWP_NOACTIVATE</remarks>
            DoNotActivate = 0x0010,
            /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid 
            /// contents of the client area are saved and copied back into the client area after the window is sized or 
            /// repositioned.</summary>
            /// <remarks>SWP_NOCOPYBITS</remarks>
            DoNotCopyBits = 0x0100,
            /// <summary>Retains the current position (ignores X and Y parameters).</summary>
            /// <remarks>SWP_NOMOVE</remarks>
            IgnoreMove = 0x0002,
            /// <summary>Does not change the owner window's position in the Z order.</summary>
            /// <remarks>SWP_NOOWNERZORDER</remarks>
            DoNotChangeOwnerZOrder = 0x0200,
            /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to 
            /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent 
            /// window uncovered as a result of the window being moved. When this flag is set, the application must
            /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
            /// <remarks>SWP_NOREDRAW</remarks>
            DoNotRedraw = 0x0008,
            /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
            /// <remarks>SWP_NOREPOSITION</remarks>
            DoNotReposition = 0x0200,
            /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
            /// <remarks>SWP_NOSENDCHANGING</remarks>
            DoNotSendChangingEvent = 0x0400,
            /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
            /// <remarks>SWP_NOSIZE</remarks>
            IgnoreResize = 0x0001,
            /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
            /// <remarks>SWP_NOZORDER</remarks>
            IgnoreZOrder = 0x0004,
            /// <summary>Displays the window.</summary>
            /// <remarks>SWP_SHOWWINDOW</remarks>
            ShowWindow = 0x0040,
        }
        public enum SpecialWindowHandles
        {
            // ReSharper disable InconsistentNaming
            /// <summary>
            ///     Places the window at the bottom of the Z order. If the hWnd parameter identifies a topmost window, the window loses its topmost status and is placed at the bottom of all other windows.
            /// </summary>
            HWND_TOP = 0,
            /// <summary>
            ///     Places the window above all non-topmost windows (that is, behind all topmost windows). This flag has no effect if the window is already a non-topmost window.
            /// </summary>
            HWND_BOTTOM = 1,
            /// <summary>
            ///     Places the window at the top of the Z order.
            /// </summary>
            HWND_TOPMOST = -1,
            /// <summary>
            ///     Places the window above all non-topmost windows. The window maintains its topmost position even when it is deactivated.
            /// </summary>
            HWND_NOTOPMOST = -2
            // ReSharper restore InconsistentNaming
        }

		#endregion Enums
	}
}
