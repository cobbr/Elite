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
    public class MenuCommandBinaryLauncherShow : MenuCommand
    {
        public MenuCommandBinaryLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show BinaryLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            BinaryLauncherMenuItem binaryMenuItem = (BinaryLauncherMenuItem)menuItem;
            binaryMenuItem.binaryLauncher = this.CovenantClient.ApiLaunchersBinaryGet();
            BinaryLauncher launcher = binaryMenuItem.binaryLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == binaryMenuItem.binaryLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "BinaryLauncher");
            menu.Rows.Add(new List<string> { "Name:", launcher.Name });
            menu.Rows.Add(new List<string> { "Description:", launcher.Description });
            menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
            menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
            menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
            menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
            menu.Print();
        }
    }

    public class MenuCommandBinaryLauncherGenerate : MenuCommand
    {
        public MenuCommandBinaryLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a base64 encoded Binary stager";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            BinaryLauncherMenuItem binaryLauncherMenuItem = (BinaryLauncherMenuItem)menuItem;
            this.CovenantClient.ApiLaunchersBinaryPost();
            binaryLauncherMenuItem.binaryLauncher = this.CovenantClient.ApiLaunchersBinaryGet();
            EliteConsole.PrintFormattedHighlightLine("Generated BinaryLauncher: " + binaryLauncherMenuItem.binaryLauncher.LauncherString);
        }
    }

    public class MenuCommandBinaryLauncherCode : MenuCommand
    {
        public MenuCommandBinaryLauncherCode() : base()
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager code.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Type",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Stager" },
                        new MenuCommandParameterValue { Value = "GruntStager" }
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {

            BinaryLauncherMenuItem binaryLauncherMenuItem = (BinaryLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 1 || commands.Length > 2 || commands[0].ToLower() != "code")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            else if (commands.Length == 2 && (!new List<string> { "stager", "gruntstager" }.Contains(commands[1].ToLower())))
            {
                EliteConsole.PrintFormattedErrorLine("Type must be one of: \"Stager\"\\\"GruntStager\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            binaryLauncherMenuItem.Refresh();
            if (binaryLauncherMenuItem.binaryLauncher.LauncherString == "")
            {
                binaryLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                binaryLauncherMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated BinaryLauncher: " + binaryLauncherMenuItem.binaryLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(binaryLauncherMenuItem.binaryLauncher.StagerCode);
            }
        }
    }

    public class MenuCommandBinaryLauncherHost : MenuCommand
    {
        public MenuCommandBinaryLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a BinaryLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            BinaryLauncherMenuItem binaryLauncherMenuItem = (BinaryLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            binaryLauncherMenuItem.binaryLauncher = this.CovenantClient.ApiLaunchersBinaryPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(binaryLauncherMenuItem.binaryLauncher.ListenerId ?? default);
            if (listener == null)
            {
                EliteConsole.PrintFormattedErrorLine("Can only host a file on a valid HttpListener.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            HostedFile fileToHost = new HostedFile
            {
                ListenerId = listener.Id,
                Path = commands[1],
                Content = binaryLauncherMenuItem.binaryLauncher.LauncherString
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            binaryLauncherMenuItem.binaryLauncher = this.CovenantClient.ApiLaunchersBinaryHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("BinaryLauncher hosted at: " + hostedLocation);
        }
    }

    public class MenuCommandBinaryLauncherWriteFile : MenuCommand
    {
        public MenuCommandBinaryLauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write BinaryLauncher to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            BinaryLauncherMenuItem binaryLauncherMenuItem = ((BinaryLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                binaryLauncherMenuItem.Refresh();
                if (binaryLauncherMenuItem.binaryLauncher.LauncherString == "")
                {
                    binaryLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    binaryLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated BinaryLauncher: " + binaryLauncherMenuItem.binaryLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllBytes(OutputFilePath, Convert.FromBase64String(binaryLauncherMenuItem.binaryLauncher.LauncherString));
                EliteConsole.PrintFormattedHighlightLine("Wrote BinaryLauncher to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandBinaryLauncherSet : MenuCommand
    {
        public MenuCommandBinaryLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set BinaryLauncher option";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue {
                            Value = "ListenerName",
                            NextValueSuggestions =  this.CovenantClient.ApiListenersGet()
                                            .Where(L => L.Status == ListenerStatus.Active)
                                            .Select(L => L.Name).ToList()
                        },
                        new MenuCommandParameterValue {
                            Value = "DotNetFrameworkVersion",
                            NextValueSuggestions = new List<string> { "net35", "net40" }
                        },
                        new MenuCommandParameterValue { Value = "Delay" },
                        new MenuCommandParameterValue { Value = "Jitter" },
                        new MenuCommandParameterValue { Value = "ConnectAttempts" },
                        new MenuCommandParameterValue { Value = "LauncherString" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            BinaryLauncher binaryLauncher = ((BinaryLauncherMenuItem)menuItem).binaryLauncher;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || commands[0].ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                if (commands[1].ToLower() == "listenername")
                {
                    Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == commands[2]);
                    if (listener == null || listener.Name != commands[2])
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid ListenerName: \"" + commands[2] + "\"");
                    }
                    else
                    {
                        binaryLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        binaryLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        binaryLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
                    }
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    binaryLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    binaryLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    binaryLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    binaryLauncher.LauncherString = commands[2];
                }
                this.CovenantClient.ApiLaunchersBinaryPut(binaryLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class BinaryLauncherMenuItem : MenuItem
    {
        public BinaryLauncher binaryLauncher { get; set; }

        public BinaryLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.binaryLauncher = CovenantClient.ApiLaunchersBinaryGet();
            this.MenuTitle = binaryLauncher.Name;
            this.MenuDescription = binaryLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandBinaryLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandBinaryLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandBinaryLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandBinaryLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandBinaryLauncherWriteFile());
            var setCommand = new MenuCommandBinaryLauncherSet(CovenantClient);
            this.AdditionalOptions.Add(setCommand);
            this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));

            this.Refresh();
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void Refresh()
        {
            this.binaryLauncher = this.CovenantClient.ApiLaunchersBinaryGet();
            this.AdditionalOptions.FirstOrDefault(AO => AO.Name.ToLower() == "set").Parameters
                .FirstOrDefault(P => P.Name.ToLower() == "option").Values
                .FirstOrDefault(V => V.Value.ToLower() == "listenername")
                .NextValueSuggestions = this.CovenantClient.ApiListenersGet()
                                            .Where(L => L.Status == ListenerStatus.Active)
                                            .Select(L => L.Name).ToList();
            this.SetupMenuAutoComplete();
        }
    }
}
