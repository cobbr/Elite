// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Launchers
{
    public class MenuCommandLaunchersShow : MenuCommand
    {
        public MenuCommandLaunchersShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Displays list of Launcher options.";
            this.Parameters = new List<MenuCommandParameter>();
        }
        
        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<Launcher> launchers = this.CovenantClient.ApiLaunchersGet().ToList();
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Launchers");
            menu.Columns.Add("Name");
            menu.Columns.Add("Description");
            launchers.ForEach(L =>
            {
                menu.Rows.Add(new List<string> { L.Name, L.Description });
            });
            menu.Print();
        }
    }

    public class LaunchersMenuItem : MenuItem
    {
		public LaunchersMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Launchers";
            this.MenuDescription = "Displays list of launcher options.";
			this.MenuOptions.Add(new WmicLauncherMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new Regsvr32LauncherMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new MshtaLauncherMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new CscriptLauncherMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new WscriptLauncherMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new InstallUtilLauncherMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new MSBuildLauncherMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new PowerShellLauncherMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new BinaryLauncherMenuItem(this.CovenantClient, this.EventPrinter));

            this.AdditionalOptions.Add(new MenuCommandLaunchersShow(this.CovenantClient));

            this.SetupMenuAutoComplete();
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
    }
}
