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
using System.Windows.Shapes;
using KinectNUI.Business.Kinect;

namespace KinectNUI.Presentation.KinectUI
{
    public partial class PieMenu : Window
    {
		public PieMenu(MainWindow mainWin, string _name)
		{
			InitializeComponent();
			mainWindow = mainWin;
			name = _name;
			label1.Content = name;
			subMenus = new List<PieMenu>();
		}

		/// <summary>Fires when the PieMenu window loads.</summary>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Select();
			subMenus.Add(new PieMenu_Apps(mainWindow, this));
			if (ParentMenu != null)
			{
				Point moveTo = GetNewSubmenuPoint(ParentMenu);
				Left = moveTo.X;
				Top = moveTo.Y;
			}
		}

		#region Properties and globals

		public MainWindow mainWindow = null;
		public List<PieMenu> subMenus { get; private set; }
		public PieMenu ParentMenu { get; private set; }
		private const int DistanceBetweenPieLevelCenters = 300;
		public string name { get; set; }
		public string guid = Guid.NewGuid().ToString();
		public bool isSelected { get; private set; }

		#endregion Properties and globals
		#region Helper Methods

		/// <summary>Adds a new submenu to this menu.</summary>
		/// <param name="menu">PieMenu to add to submenus.</param>
		public void AddSubmenu(PieMenu menu)
		{
			Point newPt = GetNewSubmenuPoint(this);
			menu.Left = newPt.X;
			menu.Top = newPt.Y;
			subMenus.Add(menu);
			subMenus.Last().Show();
		}
		/// <summary>Determines where to put a new Submenu.</summary>
		/// <param name="parent">Parent submenu</param>
		/// <returns>Point where a new submenu on the parent would be located</returns>
		protected Point GetNewSubmenuPoint(PieMenu parent)
		{
			Point ptThisMenu = new Point(parent.Width - parent.Left, parent.Height - parent.Top), ptNewMenu = new Point(0, 0);
			int SubmenuCenterDistance = 0;

			for (int iLevel = 1; iLevel < subMenus.Count; iLevel ++)
			{
				ptNewMenu = new Point(Math.Cos(45 * iLevel), Math.Sin(45 * iLevel));
				if (iLevel % 7 == 0)
					SubmenuCenterDistance += (int)(Width * 1.5D);
			}
			return ptNewMenu;
		}
		/// <summary>Selects this PieMenu, and deselects all ancestors and decendants</summary>
		public void Select()
		{
			isSelected = true;

			// Deselect all ancestors
			PieMenu temp = ParentMenu;
			while (temp != null) {
				temp.Deselect();
				temp = temp.ParentMenu; }

			// Deselect all decendants
			foreach (PieMenu menu in subMenus)
				menu.DeselectRecursive();
		}
		/// <summary>Deselects this PieMenu.</summary>
		private void Deselect()
		{
			isSelected = false;
		}
		/// <summary>Deselects this PieMenu and all descendants.</summary>
		private void DeselectRecursive()
		{
			isSelected = false;
			foreach (PieMenu menu in subMenus)
				menu.DeselectRecursive();
		}
		/// <summary>For this PieMenu and all descendant submenus, returns the first one that is selected (there shall be only one).</summary>
		/// <returns>Selected PieMenu object/window.</returns>
		public PieMenu GetSelectedPie()
		{
			if (isSelected)
				return this;
			else { 
				PieMenu result = null;
				foreach (PieMenu submenu in subMenus) {
					result = submenu.GetSelectedPie();
					if (result!= null)
						return result; } }
			return null;
		}
		/// <summary>All PieMenu classes shall have a command to execute when selected.  Override and put that in here when you inherit.</summary>
		public virtual void Execute()
		{
			return;
		}

		#endregion Helper Methods

    }

	#region Inherited PieMenu classes
	
	public class PieMenu_Apps : PieMenu
	{
		public PieMenu_Apps(MainWindow mainWin, PieMenu parent) : base(mainWin, "Apps")
		{
			parent = ParentMenu;
		}
		public override void Execute()
		{
			
		}
	}

	#endregion Inherited PieMenu classes
}
