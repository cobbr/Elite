// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

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
            List<Listener> Listeners = ((ListenersMenuItem)menuItem).Listeners;
            List<ListenerType> ListenerTypes = ((ListenersMenuItem)menuItem).ListenerTypes;

            EliteConsoleMenu typeMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Listener Types");
            typeMenu.Columns.Add("ListenerName");
            typeMenu.Columns.Add("Description");
            ListenerTypes.ToList().ForEach(L =>
            {
                typeMenu.Rows.Add(new List<string> { L.Name, L.Description });
            });
            typeMenu.PrintEndBuffer = false;
            typeMenu.Print();

            EliteConsoleMenu instanceMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Active Listeners");
            instanceMenu.Columns.Add("Name");
            instanceMenu.Columns.Add("TypeName");
            instanceMenu.Columns.Add("Status");
            instanceMenu.Columns.Add("StartTime");
            instanceMenu.Columns.Add("BindAddress");
            instanceMenu.Columns.Add("BindPort");
            Listeners.ToList().ForEach(L =>
            {
                instanceMenu.Rows.Add(new List<string> {
                    L.Name,
                    ListenerTypes.FirstOrDefault(LT => LT.Id == L.ListenerTypeId).Name,
                    L.Status.ToString(),
                    L.StartTime.ToString(),
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
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "Old Name" },
                    new MenuCommandParameter { Name = "New Name" }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }

                List<Listener> Listeners = ((ListenersMenuItem)menuItem).Listeners;
                Listener listener = Listeners.FirstOrDefault(L => L.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
                if (listener == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Listener with name: " + commands[1] + " does not exist.");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }

                if (Listeners.Any(L => L.Name.Equals(commands[2], StringComparison.OrdinalIgnoreCase)))
                {
                    EliteConsole.PrintFormattedErrorLine("Listener with name: " + commands[2] + " already exists.");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }

                listener.Name = commands[2];
                await this.CovenantClient.ApiListenersPutAsync(listener);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public sealed class ListenersMenuItem : MenuItem
    {
        public List<ListenerType> ListenerTypes { get; set; }
        public List<Listener> Listeners { get; set; }

		public ListenersMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Listeners";
            this.MenuDescription = "Displays list of listeners.";
            
			this.MenuOptions.Add(new HTTPListenerMenuItem(this.CovenantClient));
			this.MenuOptions.Add(new ListenerInteractMenuItem(this.CovenantClient));

            this.AdditionalOptions.Add(new MenuCommandListenersShow());
            this.AdditionalOptions.Add(new MenuCommandListenersRename(CovenantClient));
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
            try
            {
                this.ListenerTypes = this.CovenantClient.ApiListenersTypesGet().ToList();
                this.Listeners = this.CovenantClient.ApiListenersGet().Where(L => L.Status != ListenerStatus.Uninitialized).ToList();
                List<MenuCommandParameterValue> listenerNames = this.Listeners.Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList();

                this.MenuOptions.FirstOrDefault(M => M.MenuTitle == "Interact")
                    .MenuItemParameters
                    .FirstOrDefault(P => P.Name == "Listener Name")
                    .Values = listenerNames;

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Rename")
                    .Parameters
                    .FirstOrDefault(P => P.Name == "Old Name")
                    .Values = listenerNames;

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
	}
}
