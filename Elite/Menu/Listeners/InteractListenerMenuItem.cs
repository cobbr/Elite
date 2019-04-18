// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

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
            try
            {
                menuItem.Refresh();
                Listener listener = ((ListenerInteractMenuItem)menuItem).Listener;
                ListenerType listenerType = ((ListenerInteractMenuItem)menuItem).ListenerType;
                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, listenerType.Name + " Listener: " + listener.Name);
                switch (listenerType.Name)
                {
                    case "HTTP":
                        HttpListener httpListener = ((ListenerInteractMenuItem)menuItem).HttpListener;
                        HttpProfile httpProfile = ((ListenerInteractMenuItem)menuItem).HttpProfile;
                        menu.Rows.Add(new List<string> { "Name:", httpListener.Name });
                        menu.Rows.Add(new List<string> { "Status:", httpListener.Status.ToString() });
                        menu.Rows.Add(new List<string> { "StartTime:", httpListener.StartTime.ToString() });
                        menu.Rows.Add(new List<string> { "Description:", httpListener.Description });
                        menu.Rows.Add(new List<string> { "URL:", httpListener.Url });
                        menu.Rows.Add(new List<string> { "  ConnectAddress:", httpListener.ConnectAddress });
                        menu.Rows.Add(new List<string> { "  BindAddress:", httpListener.BindAddress });
                        menu.Rows.Add(new List<string> { "  BindPort:", httpListener.BindPort.ToString() });
                        menu.Rows.Add(new List<string> { "  UseSSL:", (httpListener.UseSSL ?? default) ? "True" : "False" });
                        menu.Rows.Add(new List<string> { "SSLCertPath:", ((ListenerInteractMenuItem)menuItem).SSLCertPath });
                        menu.Rows.Add(new List<string> { "SSLCertPassword:", httpListener.SslCertificatePassword });
                        menu.Rows.Add(new List<string> { "SSLCertHash:", httpListener.SslCertHash });
                        menu.Rows.Add(new List<string> { "HttpProfile:", httpProfile.Name });
                        break;
                }
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandListenerInteractStart : MenuCommand
    {
		public MenuCommandListenerInteractStart(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Start";
            this.Description = "Start the Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                Listener listener = ((ListenerInteractMenuItem)menuItem).Listener;
                ListenerType listenerType = ((ListenerInteractMenuItem)menuItem).ListenerType;
                if (listener.Status == ListenerStatus.Active)
                {
                    EliteConsole.PrintFormattedErrorLine("Listener: " + listener.Name + " is already active.");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                switch (listenerType.Name)
                {
                    case "HTTP":
                        HttpListener httpListener = ((ListenerInteractMenuItem)menuItem).HttpListener;
                        httpListener.Status = ListenerStatus.Active;
                        await this.CovenantClient.ApiListenersHttpPutAsync(httpListener);
                        break;
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandListenerInteractStop : MenuCommand
    {
		public MenuCommandListenerInteractStop(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Stop";
            this.Description = "Stop the Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 1 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                Listener listener = ((ListenerInteractMenuItem)menuItem).Listener;
                ListenerType listenerType = ((ListenerInteractMenuItem)menuItem).ListenerType;
                if (listener.Status == ListenerStatus.Stopped)
                {
                    EliteConsole.PrintFormattedErrorLine("Listener: " + listener.Name + " is already stopped.");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                switch (listenerType.Name)
                {
                    case "HTTP":
                        HttpListener httpListener = ((ListenerInteractMenuItem)menuItem).HttpListener;
                        httpListener.Status = ListenerStatus.Stopped;
                        await this.CovenantClient.ApiListenersHttpPutAsync(httpListener);
                        break;
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class ListenerInteractMenuItem : MenuItem
    {
        public Listener Listener { get; set; }
        public ListenerType ListenerType { get; set; }

        public HttpListener HttpListener { get; set; }
        public HttpProfile HttpProfile { get; set; }
        public string SSLCertPath { get; set; }

		public ListenerInteractMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            try
            {
                this.MenuTitle = "Interact";
                this.MenuDescription = "Interact with a Listener.";
                this.MenuItemParameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter {
                        Name = "Listener Name",
                        Values = this.CovenantClient.ApiListenersGet().Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList()
                    }
                };
                this.MenuOptions.Add(new HostedFilesMenuItem(this.CovenantClient, Listener));
                this.AdditionalOptions.Add(new MenuCommandListenerInteractShow(this.CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandListenerInteractStart(this.CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandListenerInteractStop(this.CovenantClient));
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

		public override void Refresh()
		{
            try
            {
                this.Listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == this.Listener.Name);
                this.ListenerType = this.CovenantClient.ApiListenersTypesByIdGet(this.Listener.ListenerTypeId ?? default);

                switch (this.ListenerType.Name)
                {
                    case "HTTP":
                        this.HttpListener = this.CovenantClient.ApiListenersHttpByIdGet(this.Listener.Id ?? default);
                        this.HttpProfile = this.CovenantClient.ApiProfilesHttpByIdGet(this.Listener.ProfileId ?? default);
                        break;
                }

                List<MenuCommandParameterValue> listenerNames = this.CovenantClient.ApiListenersGet().Select(L => new MenuCommandParameterValue { Value = L.Name }).ToList();
                this.MenuItemParameters.FirstOrDefault(L => L.Name == "Listener Name").Values = listenerNames;

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            try
            {
                if (forwardEntrance)
                {
                    if (parameters.Length != 1)
                    {
                        EliteConsole.PrintFormattedErrorLine("Must specify a ListenerName.");
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <listener_name>");
                        return false;
                    }
                    string listenerName = parameters[0];
                    Listener specifiedListener = CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name.Equals(listenerName, StringComparison.OrdinalIgnoreCase));
                    if (specifiedListener == null)
                    {
                        EliteConsole.PrintFormattedErrorLine("Specified invalid ListenerName: " + listenerName);
                        EliteConsole.PrintFormattedErrorLine("Usage: Interact <listener_name>");
                        return false;
                    }
                    this.Listener = specifiedListener;
                    ((HostedFilesMenuItem)this.MenuOptions.FirstOrDefault(MO => MO.MenuTitle == "HostedFiles")).Listener = this.Listener;
                    this.MenuTitle = listenerName;
                    this.Refresh();
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
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
