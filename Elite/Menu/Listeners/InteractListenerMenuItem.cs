// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

using Elite.Menu.Listeners;

namespace Elite.Menu.Listeners
{
    public class MenuCommandListenerInteractShow : MenuCommand
    {
        public MenuCommandListenerInteractShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show details of the Listener.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            ListenerInteractMenuItem listenerInteractMenuItem = (ListenerInteractMenuItem)menuItem;
            Listener listener = listenerInteractMenuItem.listener;
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, listenerInteractMenuItem.listenerType.Name + " Listener: " + listener.Name);
            switch (listenerInteractMenuItem.listenerType.Name)
            {
                case "HTTP":
                    HttpListener httpListener = this.CovenantClient.ApiListenersHttpByIdGet(listener.Id ?? default);
                    HttpProfile httpProfile = this.CovenantClient.ApiListenersByIdProfileGet(httpListener.Id ?? default);
                    menu.Rows.Add(new List<string> { "Name:", httpListener.Name });
                    menu.Rows.Add(new List<string> { "Status:", httpListener.Status.ToString() });
                    menu.Rows.Add(new List<string> { "StartTime:", httpListener.StartTime.ToString() });
                    menu.Rows.Add(new List<string> { "Description:", httpListener.Description });
                    menu.Rows.Add(new List<string> { "URL:", httpListener.Url });
                    menu.Rows.Add(new List<string> { "  ConnectAddress:", httpListener.ConnectAddress });
                    menu.Rows.Add(new List<string> { "  BindAddress:", httpListener.BindAddress });
                    menu.Rows.Add(new List<string> { "  BindPort:", httpListener.BindPort.ToString() });
                    menu.Rows.Add(new List<string> { "  UseSSL:", (httpListener.UseSSL ?? default) ? "True" : "False" });
                    menu.Rows.Add(new List<string> { "SSLCertPath:", listenerInteractMenuItem.SSLCertPath });
                    menu.Rows.Add(new List<string> { "SSLCertPassword:", httpListener.SslCertificatePassword });
                    menu.Rows.Add(new List<string> { "SSLCertHash:", httpListener.SslCertHash });
                    menu.Rows.Add(new List<string> { "HttpProfile:", httpProfile.Name });
                    break;
            }
            menu.Print();
        }
    }

    public class MenuCommandListenerInteractStart : MenuCommand
    {
		public MenuCommandListenerInteractStart(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.Name = "Start";
            this.Description = "Start the Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            ListenerInteractMenuItem listenerInteractMenuItem = (ListenerInteractMenuItem)menuItem;
            // TODO: error if http lsitener already on this port
            if (listenerInteractMenuItem.listener.Status == ListenerStatus.Active)
            {
                EliteConsole.PrintFormattedErrorLine("Listener: " + listenerInteractMenuItem.listener.Name + " is already active.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                switch(listenerInteractMenuItem.listenerType.Name)
                {
                    case "HTTP":
                        HttpListener httpListener = this.CovenantClient.ApiListenersHttpByIdGet(listenerInteractMenuItem.listener.Id ?? default);
                        httpListener.Status = ListenerStatus.Active;
                        this.CovenantClient.ApiListenersHttpPut(httpListener);
                        break;
                }
                listenerInteractMenuItem.Refresh();
            }
        }
    }

    public class MenuCommandListenerInteractStop : MenuCommand
    {
		public MenuCommandListenerInteractStop(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.Name = "Stop";
            this.Description = "Stop the Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            ListenerInteractMenuItem listenerInteractMenuItem = (ListenerInteractMenuItem)menuItem;
            if (listenerInteractMenuItem.listener.Status == ListenerStatus.Stopped)
            {
                EliteConsole.PrintFormattedErrorLine("Listener: " + listenerInteractMenuItem.listener.Name + " is already stopped.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
			{
                switch (listenerInteractMenuItem.listenerType.Name)
                {
                    case "HTTP":
                        HttpListener httpListener = this.CovenantClient.ApiListenersHttpByIdGet(listenerInteractMenuItem.listener.Id ?? default);
                        httpListener.Status = ListenerStatus.Stopped;
                        httpListener = this.CovenantClient.ApiListenersHttpPut(httpListener);
                        break;
                }
                listenerInteractMenuItem.Refresh();
            }
        }
    }

    public class ListenerInteractMenuItem : MenuItem
    {
        public Listener listener { get; set; }
        public ListenerType listenerType { get; set; }
        public string SSLCertPath { get; set; }

		public ListenerInteractMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Interact";
            this.MenuDescription = "Interact with a Listener.";
            this.MenuItemParameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Listener Name",
                    Values = CovenantClient.ApiListenersGet().Select(L => new MenuCommandParameterValue { Value = L.Name}).ToList()
                }
            };

			this.MenuOptions.Add(new HostedFilesMenuItem(this.CovenantClient, this.EventPrinter, listener));

            this.AdditionalOptions.Add(new MenuCommandListenerInteractShow(this.CovenantClient));
			this.AdditionalOptions.Add(new MenuCommandListenerInteractStart(this.CovenantClient, this.EventPrinter));
			this.AdditionalOptions.Add(new MenuCommandListenerInteractStop(this.CovenantClient, this.EventPrinter));

            this.SetupMenuAutoComplete();
        }

		public override void Refresh()
		{
            this.listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == this.listener.Name);
            this.listenerType = this.CovenantClient.ApiListenersTypesByIdGet(this.listener.ListenerTypeId ?? default);

            List<MenuCommandParameterValue> listenerNames = CovenantClient.ApiListenersGet().Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList();
            this.MenuItemParameters.FirstOrDefault(L => L.Name == "Listener Name").Values = listenerNames;
		}

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            if (forwardEntrance)
            {
                if (parameters.Length != 1)
                {
                    EliteConsole.PrintFormattedErrorLine("Must specify a ListenerName.");
                    EliteConsole.PrintFormattedErrorLine("Usage: Interact <listener_name>");
                    return false;
                }
                string listenerName = parameters[0].ToLower();
                Listener specifiedListener = CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name.ToLower() == listenerName);
                if (specifiedListener == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Specified invalid ListenerName: " + listenerName);
                    EliteConsole.PrintFormattedErrorLine("Usage: Interact <listener_name>");
                    return false;
                }
                this.listener = specifiedListener;
                this.listenerType = this.CovenantClient.ApiListenersTypesByIdGet(this.listener.ListenerTypeId ?? default);
                ((HostedFilesMenuItem)this.MenuOptions.FirstOrDefault(MO => MO.MenuTitle == "HostedFiles")).Listener = listener;

                this.MenuTitle = listenerName;
            }
            this.Refresh();
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void LeavingMenuItem()
        {
            this.MenuTitle = "Interact";
        }
    }
}
