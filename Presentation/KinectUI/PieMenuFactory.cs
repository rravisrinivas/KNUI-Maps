using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectNUI.Presentation.KinectUI
{
	class PieMenuFactory
	{
		public static PieMenu BuildPie(MainWindow window)
		{
			PieMenu menu = new PieMenu(window, "root");
			AddOptions(ref menu);
			return menu;
		}
		public static PieMenu BuildPie(PieMenu parent, string _name)
		{
			PieMenu menu = new PieMenu(parent.mainWindow, _name);
			AddOptions(ref menu);
			return menu;
		}

		public static void AddOptions(ref PieMenu menu)
		{
			// I don't remember what this was supposed to do.
			switch (menu.Name)
			{
				case "apps":
					break;
				case "root":
				default:
					break;
			}
		}
		public static void BuildSubmenus(ref PieMenu piemenu)
		{
			switch (piemenu.name)
			{
				case "root":
					piemenu.AddSubmenu(new PieMenu_Apps(piemenu.mainWindow, piemenu));
					break;
				case "apps":
					break; // TODO: Add pies for Apps
				default:
					break;
			}
		}
	}
}
