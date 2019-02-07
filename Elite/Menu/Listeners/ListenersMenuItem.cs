// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Listeners
{
    public class MenuCommandListenersShow : MenuCommand
    {
        public MenuCommandListenersShow()
        {
            this.Name = "Show";
            this.Description = "Show Listener types";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            ListenersMenuItem listenersMenu = ((ListenersMenuItem)menuItem);
            EliteConsoleMenu typeMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Listener Types");
            typeMenu.Columns.Add("ListenerName");
            typeMenu.Columns.Add("Description");
            listenersMenu.ListenerTypes.ToList().ForEach(L =>
            {
                typeMenu.Rows.Add(new List<string> { L.Name, L.Description });
            });
            typeMenu.PrintEndBuffer = false;
            typeMenu.Print();

            EliteConsoleMenu instanceMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Active Listeners");
            instanceMenu.Columns.Add("Name");
            instanceMenu.Columns.Add("TypeName");
            instanceMenu.Columns.Add("Status");
            instanceMenu.Columns.Add("BindAddress");
            instanceMenu.Columns.Add("BindPort");
            listenersMenu.Listeners.ToList().ForEach(L =>
            {
                instanceMenu.Rows.Add(new List<string> {
                    L.Name,
                    listenersMenu.ListenerTypes.FirstOrDefault(LT => LT.Id == L.ListenerTypeId).Name,
                    L.Status.ToString(),
                    L.BindAddress,
                    L.BindPort.ToString()
                });
            });
            instanceMenu.Print();
        }
    }

    public class MenuCommandListenersRename : MenuCommand
    {
        public MenuCommandListenersRename(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Rename";
            this.Description = "Rename a listener";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Old Name",
                    Values = this.CovenantClient.ApiListenersGet().Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList()
                },
                new MenuCommandParameter { Name = "New Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            ListenersMenuItem listenersMenu = ((ListenersMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || commands[0].ToLower() != "rename")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            Listener listener = listenersMenu.Listeners.FirstOrDefault(L => L.Name.ToLower() == commands[1]);
            if (listener == null)
            {
                EliteConsole.PrintFormattedErrorLine("Listener with name: " + commands[1] + " does not exist.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (listenersMenu.Listeners.Where(L => L.Name.ToLower() == commands[2].ToLower()).Any())
            {
                EliteConsole.PrintFormattedErrorLine("Listener with name: " + commands[2] + " already exists.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                listener.Name = commands[2];
                this.CovenantClient.ApiListenersPut(listener);
            }
        }
    }

    public sealed class ListenersMenuItem : MenuItem
    {
        public List<ListenerType> ListenerTypes { get; set; }
        public List<Listener> Listeners { get; set; }

		public ListenersMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Listeners";
            this.MenuDescription = "Displays list of listeners.";
            
			this.MenuOptions.Add(new HTTPListenerMenuItem(this.CovenantClient, this.EventPrinter));
			this.MenuOptions.Add(new ListenerInteractMenuItem(this.CovenantClient, this.EventPrinter));

            this.AdditionalOptions.Add(new MenuCommandListenersShow());
            this.AdditionalOptions.Add(new MenuCommandListenersRename(CovenantClient));

            this.SetupMenuAutoComplete();
            this.Refresh();
        }

        public override bool ValidateMenuParameters(string[] parameters = null, bool forwardEntrance = true)
        {
            this.Refresh();
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

		public override void Refresh()
		{
            ListenerTypes = this.CovenantClient.ApiListenersTypesGet().ToList();
            Listeners = this.CovenantClient.ApiListenersGet().Where(L => L.Status != ListenerStatus.Uninitialized).ToList();
            List<MenuCommandParameterValue> listenerNames = this.Listeners.Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList();

            this.MenuOptions.FirstOrDefault(M => M.MenuTitle == "Interact")
                .MenuItemParameters.FirstOrDefault(P => P.Name == "Listener Name").Values = listenerNames;
            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Rename")
                .Parameters.FirstOrDefault(P => P.Name == "Old Name").Values = listenerNames;
            
            this.SetupMenuAutoComplete();
		}
	}
}
