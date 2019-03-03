// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;

using Elite.Menu.Listeners;
using Elite.Menu.Launchers;
using Elite.Menu.Grunts;
using Elite.Menu.Users;
using Elite.Menu.Indicators;

namespace Elite.Menu
{
    public class MenuCommandCovenantBaseItemShow : MenuCommand
    {
        public override void Command(MenuItem menuItem, string UserInput)
        {
            new MenuCommandHelp().Command(menuItem, UserInput);
        }
    }

    public class CovenantBaseMenuItem : MenuItem
    {
		public CovenantBaseMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Covenant";
            this.MenuDescription = "Base Covenant menu.";
			this.MenuOptions.Add(new GruntsMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new LaunchersMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new ListenersMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new CredentialsMenuItem(this.CovenantClient, this.EventPrinter));
            this.MenuOptions.Add(new IndicatorsMenuItem(this.CovenantClient, this.EventPrinter));
			try
			{
				this.MenuOptions.Add(new UsersMenuItem(this.CovenantClient, this.EventPrinter));
			}
			catch (Microsoft.Rest.HttpOperationException)
			{ }
            this.AdditionalOptions.Remove(this.AdditionalOptions.FirstOrDefault(O => O.Name == "Back"));
            this.AdditionalOptions.Add(
                new MenuCommandCovenantBaseItemShow()
                {
                    Name = "Show",
                    Description = "Show Help menu.",
                    Parameters = new List<MenuCommandParameter>()
                }
            );
            this.SetupMenuAutoComplete();
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
    }
}
