using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectNUI.Business.Kinect
{
    public static class HardwareEvents
    {
        public static int MouseMove = 0x0001;
        public static int MouseLeftDown = 0x0002, MouseLeftUp = 0x0004;
        public static int MouseRightDown = 0x0008, MouseRightUp = 0x0010; // best guess on the 0010
        public static int MiddleMouseDown = 0x20, MiddleMouseUp = 0x40;
        public static int MouseWheel = 0x800;

        // http://msdn.microsoft.com/en-us/library/ms646280(v=vs.85).aspx
        public static int WinLeftDown = 0x5B, WinRigtDown = 0x5C, WinContext = 0x5D, WinKeyUp = 0x0101;
    }
}
