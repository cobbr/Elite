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

namespace Elite.Menu.Listeners
{
    public class MenuCommandHostedFilesShow : MenuCommand
    {
        public MenuCommandHostedFilesShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "List files hosted by the HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                menuItem.Refresh();
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;
                List<HostedFile> HostedFiles = ((HostedFilesMenuItem)menuItem).HostedFiles;

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "HostedFiles");
                menu.Columns.Add("Listener");
                menu.Columns.Add("Address");
                menu.Columns.Add("Path");
                HostedFiles.ForEach(HF =>
                {
                    menu.Rows.Add(new List<string> { listener.Name, listener.ConnectAddress, HF.Path });
                });
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandHostedFilesHost : MenuCommand
    {
        public MenuCommandHostedFilesHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a new file.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "LocalFilePath" },
                new MenuCommandParameter { Name = "HostPath" }
            };
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
                FileInfo file = new FileInfo(Path.Combine(Common.EliteDataFolder, commands[1]));
                if (!file.Exists)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("File: \"" + file.FullName + "\" does not exist on the local system.");
                    return;
                }
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;
                HostedFile hostedFile = new HostedFile
                {
                    ListenerId = listener.Id,
                    Path = commands[2],
                    Content = Convert.ToBase64String(File.ReadAllBytes(file.FullName))
                };
                await this.CovenantClient.ApiListenersByIdHostedfilesPostAsync(listener.Id ?? default, hostedFile);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandHostedFilesRemove : MenuCommand
    {
        public MenuCommandHostedFilesRemove(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Remove";
            this.Description = "Remove a file being hosted.";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "HostPath" }
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
                if (commands.Length != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;
                HostedFile hostedFile = ((HostedFilesMenuItem)menuItem).HostedFiles.FirstOrDefault(HF => HF.Path == commands[1]);
                if (hostedFile == null)
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    EliteConsole.PrintFormattedErrorLine("No file is currently being hosted at: \"" + commands[1] + "\".");
                    return;
                }
                await this.CovenantClient.ApiListenersByIdHostedfilesByHfidDeleteAsync(listener.Id ?? default, hostedFile.Id ?? default);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class HostedFilesMenuItem : MenuItem
    {
        public Listener Listener { get; set; }
        public List<HostedFile> HostedFiles { get; set; }

        public HostedFilesMenuItem(CovenantAPI CovenantClient, Listener Listener) : base(CovenantClient)
        {
            this.Listener = Listener;
            this.MenuTitle = "HostedFiles";
            this.MenuDescription = "Files hosted by the HTTP Listener.";
            this.AdditionalOptions.Add(new MenuCommandHostedFilesShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandHostedFilesHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandHostedFilesRemove(CovenantClient));
        }

		public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void Refresh()
        {
            try
            {
                this.Listener = this.CovenantClient.ApiListenersByIdGet(this.Listener.Id ?? default);
                this.HostedFiles = this.CovenantClient.ApiListenersByIdHostedfilesGet(this.Listener.Id ?? default).ToList();

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Remove").Parameters
                    .FirstOrDefault(P => P.Name == "HostPath").Values = 
                        this.HostedFiles
                            .Select(HF => new MenuCommandParameterValue { Value = HF.Path })
                            .ToList();

                var filevalues = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder);
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Host").Parameters
                    .FirstOrDefault(P => P.Name == "LocalFilePath").Values = filevalues;

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}