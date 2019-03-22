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
    public class MenuCommandInstallUtilLauncherShow : MenuCommand
    {
        public MenuCommandInstallUtilLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show InstallUtilLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            InstallUtilLauncherMenuItem installutilMenuItem = (InstallUtilLauncherMenuItem)menuItem;
            installutilMenuItem.installutilLauncher = this.CovenantClient.ApiLaunchersInstallutilGet();
            InstallUtilLauncher launcher = installutilMenuItem.installutilLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == installutilMenuItem.installutilLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "InstallUtilLauncher");
            menu.Rows.Add(new List<string> { "Name:", launcher.Name });
            menu.Rows.Add(new List<string> { "Description:", launcher.Description });
            menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
            menu.Rows.Add(new List<string> { "CommType:", launcher.CommType.ToString() });
            menu.Rows.Add(new List<string> { "  SMBPipeName:", launcher.SmbPipeName });
            menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
            menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
            menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
            menu.Print();
        }
    }

    public class MenuCommandInstallUtilLauncherGenerate : MenuCommand
    {
        public MenuCommandInstallUtilLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a InstallUtilLauncher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            InstallUtilLauncherMenuItem installutilMenuItem = (InstallUtilLauncherMenuItem)menuItem;
            installutilMenuItem.installutilLauncher = this.CovenantClient.ApiLaunchersInstallutilPost();
            EliteConsole.PrintFormattedHighlightLine("Generated InstallUtilLauncher: " + installutilMenuItem.installutilLauncher.LauncherString);
        }
    }

    public class MenuCommandInstallUtilLauncherCode : MenuCommand
    {
        public MenuCommandInstallUtilLauncherCode() : base()
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager or Scriptlet code.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Type",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "XML" },
                        new MenuCommandParameterValue { Value = "Stager" },
                        new MenuCommandParameterValue { Value = "GruntStager" }
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            InstallUtilLauncherMenuItem installutilMenuItem = (InstallUtilLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 1 || commands.Length > 2 || commands[0].ToLower() != "code")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            else if (commands.Length == 2 && (!new List<string> { "stager", "gruntstager", "xml" }.Contains(commands[1].ToLower())))
            {
                EliteConsole.PrintFormattedErrorLine("Type must be one of: \"Stager\"\\\"GruntStager\" or \"XML\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            installutilMenuItem.Refresh();
            if (installutilMenuItem.installutilLauncher.LauncherString == "")
            {
                installutilMenuItem.CovenantClient.ApiLaunchersInstallutilPost();
                installutilMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated InstallUtilLauncher: " + installutilMenuItem.installutilLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(installutilMenuItem.installutilLauncher.StagerCode);
            }
            else if (commands.Length == 2 && commands[1].ToLower() == "xml")
            {
                EliteConsole.PrintInfoLine(installutilMenuItem.installutilLauncher.DiskCode);
            }
        }
    }

    public class MenuCommandInstallUtilLauncherHost : MenuCommand
    {
        public MenuCommandInstallUtilLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a InstallUtilLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            InstallUtilLauncherMenuItem installutilMenuItem = (InstallUtilLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            installutilMenuItem.installutilLauncher = this.CovenantClient.ApiLaunchersInstallutilPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(installutilMenuItem.installutilLauncher.ListenerId ?? default);
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
                Content = installutilMenuItem.installutilLauncher.DiskCode
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            installutilMenuItem.installutilLauncher = this.CovenantClient.ApiLaunchersInstallutilHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("InstallUtilLauncher hosted at: " + hostedLocation);
            EliteConsole.PrintFormattedWarningLine("installutil.exe cannot execute remotely hosted files, the payload must first be written to disk");
            EliteConsole.PrintFormattedInfoLine("Launcher: " + installutilMenuItem.installutilLauncher.LauncherString);

        }
    }

    public class MenuCommandInstallUtilLauncherWriteFile : MenuCommand
    {
        public MenuCommandInstallUtilLauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write xml to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            InstallUtilLauncherMenuItem installutilLauncherMenuItem = ((InstallUtilLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                installutilLauncherMenuItem.Refresh();
                if (installutilLauncherMenuItem.installutilLauncher.LauncherString == "")
                {
                    installutilLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    installutilLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated InstallUtilLauncher: " + installutilLauncherMenuItem.installutilLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllBytes(OutputFilePath, Convert.FromBase64String(installutilLauncherMenuItem.installutilLauncher.DiskCode));
                EliteConsole.PrintFormattedHighlightLine("Wrote InstallUtilLauncher to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandInstallUtilLauncherSet : MenuCommand
    {
        public MenuCommandInstallUtilLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set InstallUtilLauncher option";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue {
                            Value = "ListenerName",
                            NextValueSuggestions = this.CovenantClient.ApiListenersGet()
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
            InstallUtilLauncher installutilLauncher = ((InstallUtilLauncherMenuItem)menuItem).installutilLauncher;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 3 || commands[0].ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                if (commands[1].ToLower() == "listenername")
                {
                    Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == commands[2]);
                    if (listener == null || listener.Name != commands[2])
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid ListenerName: \"" + commands[2] + "\"");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                    else
                    {
                        installutilLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        installutilLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        installutilLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid DotNetFrameworkVersion \"" + commands[2] + "\". Valid options are: v3.5, v4.0");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                }
                else if (commands[1].ToLower() == "commtype")
                {
                    if (commands[2].ToLower() == "smb")
                    {
                        installutilLauncher.CommType = CommunicationType.SMB;
                    }
                    else
                    {
                        installutilLauncher.CommType = CommunicationType.HTTP;
                    }
                }
                else if (commands[1].ToLower() == "smbpipename")
                {
                    installutilLauncher.SmbPipeName = commands[2];
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    installutilLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    installutilLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    installutilLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    installutilLauncher.LauncherString = commands[2];
                }
                CovenantAPIExtensions.ApiLaunchersInstallutilPut(this.CovenantClient, installutilLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class InstallUtilLauncherMenuItem : MenuItem
    {
        public InstallUtilLauncher installutilLauncher { get; set; }

        public InstallUtilLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.installutilLauncher = CovenantClient.ApiLaunchersInstallutilGet();
            this.MenuTitle = installutilLauncher.Name;
            this.MenuDescription = installutilLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandInstallUtilLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandInstallUtilLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandInstallUtilLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandInstallUtilLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandInstallUtilLauncherWriteFile());
            var setCommand = new MenuCommandInstallUtilLauncherSet(CovenantClient);
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
            this.installutilLauncher = this.CovenantClient.ApiLaunchersInstallutilGet();
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
