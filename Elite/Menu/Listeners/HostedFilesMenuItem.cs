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
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;
                List<HostedFile> hostedFiles = this.CovenantClient.ApiListenersByIdHostedfilesGet(listener.Id ?? default).ToList();

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "HostedFiles");
                menu.Columns.Add("Listener");
                // menu.Columns.Add("HostUri");
                menu.Columns.Add("Path");
                hostedFiles.ForEach(HF =>
                {
                    menu.Rows.Add(new List<string> { listener.Name, HF.Path });
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
                new MenuCommandParameter {
                    Name = "LocalFilePath",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                },
                new MenuCommandParameter{ Name = "HostPath" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;

                string[] commands = UserInput.Split(" ");
                if (commands.Length != 3 || commands[0].ToLower() != "host")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else
                {
                    FileInfo file = new FileInfo(commands[1]);
                    if (!file.Exists)
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("File: \"" + commands[1] + "\" does not exist on the local system.");
                        return;
                    }
                    HostedFile hostedFile = new HostedFile
                    {
                        ListenerId = listener.Id,
                        Path = commands[2],
                        Content = Convert.ToBase64String(File.ReadAllBytes(commands[1]))
                    };
                    this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, hostedFile);
                }
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
                    new MenuCommandParameter {
                        Name = "HostPath",
                        Values = this.CovenantClient.ApiListenersGet()
                                     .Select(L => this.CovenantClient.ApiListenersByIdHostedfilesGet(L.Id ?? default(int))
                                                      .Select(HF => new MenuCommandParameterValue { Value = HF.Path })
                                            ).FirstOrDefault().ToList()
                    }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Listener listener = ((HostedFilesMenuItem)menuItem).Listener;

                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "remove")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else
                {
                    HostedFile hostedFile = this.CovenantClient.ApiListenersByIdHostedfilesGet(listener.Id ?? default).FirstOrDefault(HF => HF.Path == commands[1]);
                    if (hostedFile == null)
                    {
                        menuItem.PrintInvalidOptionError(UserInput);
                        EliteConsole.PrintFormattedErrorLine("No file is currently being hosted at: \"" + commands[1] + "\".");
                        return;
                    }
                    this.CovenantClient.ApiListenersByIdHostedfilesByHfidDelete(listener.Id ?? default, hostedFile.Id ?? default);
                }
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

        public HostedFilesMenuItem(CovenantAPI CovenantClient, Listener Listener) : base(CovenantClient)
        {
            this.Listener = Listener;
            this.MenuTitle = "HostedFiles";
            this.MenuDescription = "Files hosted by the HTTP Listener.";
            this.AdditionalOptions.Add(new MenuCommandHostedFilesShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandHostedFilesHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandHostedFilesRemove(CovenantClient));

            this.SetupMenuAutoComplete();
        }

		public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
    }
}