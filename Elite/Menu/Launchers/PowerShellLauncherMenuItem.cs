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
    public class MenuCommandPowerShellLauncherShow : MenuCommand
    {
        public MenuCommandPowerShellLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show PowerShellLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            PowerShellLauncherMenuItem powershellMenuItem = (PowerShellLauncherMenuItem)menuItem;
            powershellMenuItem.powerShellLauncher = this.CovenantClient.ApiLaunchersPowershellGet();
            PowerShellLauncher launcher = powershellMenuItem.powerShellLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == powershellMenuItem.powerShellLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "PowerShellLauncher");
            menu.Rows.Add(new List<string> { "Name:", launcher.Name });
            menu.Rows.Add(new List<string> { "Description:", launcher.Description });
            menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
            menu.Rows.Add(new List<string> { "CommType:", launcher.CommType.ToString() });
            menu.Rows.Add(new List<string> { "  SMBPipeName:", launcher.SmbPipeName });
            menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
            menu.Rows.Add(new List<string> { "ParameterString:", launcher.ParameterString });
            menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
            menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
            menu.Print();
        }
    }

    public class MenuCommandPowerShellLauncherGenerate : MenuCommand
    {
        public MenuCommandPowerShellLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a PowerShell stager";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            PowerShellLauncherMenuItem powershellMenuItem = (PowerShellLauncherMenuItem)menuItem;
            powershellMenuItem.powerShellLauncher = this.CovenantClient.ApiLaunchersPowershellPost();
            EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + powershellMenuItem.powerShellLauncher.LauncherString);
        }
    }

    public class MenuCommandPowerShellLauncherCode : MenuCommand
    {
        public MenuCommandPowerShellLauncherCode() : base()
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager or Scriptlet code.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Type",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Stager" },
                        new MenuCommandParameterValue { Value = "GruntStager" },
                        new MenuCommandParameterValue { Value = "PowerShell" },
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            PowerShellLauncherMenuItem powerShellMenuItem = (PowerShellLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 1 || commands.Length > 2 || commands[0].ToLower() != "code")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            else if (commands.Length == 2 && (!new List<string> { "stager", "gruntstager", "powershell" }.Contains(commands[1].ToLower())))
            {
                EliteConsole.PrintFormattedErrorLine("Type must be one of: \"Stager\"\\\"GruntStager\" or \"PowerShell\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            powerShellMenuItem.Refresh();
            if (powerShellMenuItem.powerShellLauncher.LauncherString == "")
            {
                powerShellMenuItem.CovenantClient.ApiLaunchersPowershellPost();
                powerShellMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + powerShellMenuItem.powerShellLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(powerShellMenuItem.powerShellLauncher.StagerCode);
            }
            else if (commands.Length == 2 && commands[1].ToLower() == "powershell")
            {
                EliteConsole.PrintInfoLine(powerShellMenuItem.powerShellLauncher.PowerShellCode);
            }
        }
    }

    public class MenuCommandPowerShellLauncherHost : MenuCommand
    {
        public MenuCommandPowerShellLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a PowerShellLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            PowerShellLauncherMenuItem powershellMenuItem = (PowerShellLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            powershellMenuItem.powerShellLauncher = this.CovenantClient.ApiLaunchersPowershellPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(powershellMenuItem.powerShellLauncher.ListenerId ?? default);
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
                Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(powershellMenuItem.powerShellLauncher.PowerShellCode))
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            powershellMenuItem.powerShellLauncher = this.CovenantClient.ApiLaunchersPowershellHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("PowerShellLauncher hosted at: " + hostedLocation);
            EliteConsole.PrintFormattedInfoLine("Launcher (Command):        " + powershellMenuItem.powerShellLauncher.LauncherString);
            EliteConsole.PrintFormattedInfoLine("Launcher (EncodedCommand): " + powershellMenuItem.powerShellLauncher.EncodedLauncherString);
        }
    }

    public class MenuCommandPowerShellLauncherWriteFile : MenuCommand
    {
        public MenuCommandPowerShellLauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write PowerShellLauncher to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            PowerShellLauncherMenuItem PowerShellLauncherMenuItem = ((PowerShellLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if(commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                PowerShellLauncherMenuItem.Refresh();
                if (PowerShellLauncherMenuItem.powerShellLauncher.LauncherString == "")
                {
                    PowerShellLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    PowerShellLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + PowerShellLauncherMenuItem.powerShellLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, PowerShellLauncherMenuItem.powerShellLauncher.LauncherString);
                EliteConsole.PrintFormattedHighlightLine("Wrote PowerShellLauncher to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandPowerShellLauncherSet : MenuCommand
    {
        public MenuCommandPowerShellLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set PowerShellLauncher option";
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
                            Value = "CommType",
                            NextValueSuggestions = new List<string> { "HTTP", "SMB" }
                        },
                        new MenuCommandParameterValue { Value = "SMBPipeName" },
                        new MenuCommandParameterValue {
                            Value = "DotNetFrameworkVersion",
                            NextValueSuggestions = new List<string> { "net35", "net40" }
                        },
                        new MenuCommandParameterValue { Value = "ParameterString" },
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
            PowerShellLauncher PowerShellLauncher = ((PowerShellLauncherMenuItem)menuItem).powerShellLauncher;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 3 || commands[0].ToLower() != "set")
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
                        EliteConsole.PrintFormattedErrorLine("Invalid ListenerName");
                    }
                    else
                    {
                        PowerShellLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "parameterstring")
                {
                    PowerShellLauncher.ParameterString = String.Join(" ", commands.TakeLast(commands.Length - 2).ToList());
                }
                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        PowerShellLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        PowerShellLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
                    }
                }
                else if (commands[1].ToLower() == "commtype")
                {
                    if (commands[2].ToLower() == "smb")
                    {
                        PowerShellLauncher.CommType = CommunicationType.SMB;
                    }
                    else
                    {
                        PowerShellLauncher.CommType = CommunicationType.HTTP;
                    }
                }
                else if (commands[1].ToLower() == "smbpipename")
                {
                    PowerShellLauncher.SmbPipeName = commands[2];
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    PowerShellLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    PowerShellLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    PowerShellLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    PowerShellLauncher.LauncherString = commands[2];
                }
                CovenantAPIExtensions.ApiLaunchersPowershellPut(this.CovenantClient, PowerShellLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class PowerShellLauncherMenuItem : MenuItem
    {
        public PowerShellLauncher powerShellLauncher { get; set; }

		public PowerShellLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.powerShellLauncher = CovenantClient.ApiLaunchersPowershellGet();
            this.MenuTitle = powerShellLauncher.Name;
            this.MenuDescription = powerShellLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherWriteFile());
            var setCommand = new MenuCommandPowerShellLauncherSet(CovenantClient);
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
            this.powerShellLauncher = this.CovenantClient.ApiLaunchersPowershellGet();
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
