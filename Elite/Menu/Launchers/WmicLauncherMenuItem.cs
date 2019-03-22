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
    public class MenuCommandWmicLauncherShow : MenuCommand
    {
        public MenuCommandWmicLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show WmicLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            WmicLauncherMenuItem wmicMenuItem = (WmicLauncherMenuItem)menuItem;
            wmicMenuItem.wmicLauncher = this.CovenantClient.ApiLaunchersWmicGet();
            WmicLauncher launcher = wmicMenuItem.wmicLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == wmicMenuItem.wmicLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "WmicLauncher");
            menu.Rows.Add(new List<string> { "Name:", launcher.Name });
            menu.Rows.Add(new List<string> { "Description:", launcher.Description });
            menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
            menu.Rows.Add(new List<string> { "CommType:", launcher.CommType.ToString() });
            menu.Rows.Add(new List<string> { "  SMBPipeName:", launcher.SmbPipeName });
            menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
            menu.Rows.Add(new List<string> { "ScriptLanguage:", launcher.ScriptLanguage.ToString() });
            menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
            menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
            menu.Print();
        }
    }

    public class MenuCommandWmicLauncherGenerate : MenuCommand
    {
        public MenuCommandWmicLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a WmicLauncher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            WmicLauncherMenuItem wmicMenuItem = (WmicLauncherMenuItem)menuItem;
            wmicMenuItem.wmicLauncher = this.CovenantClient.ApiLaunchersWmicPost();
            EliteConsole.PrintFormattedHighlightLine("Generated WmicLauncher: " + wmicMenuItem.wmicLauncher.LauncherString);
        }
    }

    public class MenuCommandWmicLauncherCode : MenuCommand
    {
        public MenuCommandWmicLauncherCode() : base()
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager or Scriptlet code.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Type",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Scriptlet" },
                        new MenuCommandParameterValue { Value = "Stager" },
                        new MenuCommandParameterValue { Value = "GruntStager" }
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            WmicLauncherMenuItem wmicMenuItem = (WmicLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length < 1 || commands.Length > 2 || commands[0].ToLower() != "code")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            else if (commands.Length == 2 && (!new List<string> { "stager", "gruntstager", "scriptlet" }.Contains(commands[1].ToLower())))
            {
                EliteConsole.PrintFormattedErrorLine("Type must be one of: \"Stager\"\\\"GruntStager\" or \"Scriptlet\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            wmicMenuItem.Refresh();
            if (wmicMenuItem.wmicLauncher.LauncherString == "")
            {
                wmicMenuItem.CovenantClient.ApiLaunchersWmicPost();
                wmicMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated WmicLauncher: " + wmicMenuItem.wmicLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(wmicMenuItem.wmicLauncher.StagerCode);
            }
            else if (commands.Length == 2 && commands[1].ToLower() == "scriptlet")
            {
                EliteConsole.PrintInfoLine(wmicMenuItem.wmicLauncher.DiskCode);
            }
        }
    }

    public class MenuCommandWmicLauncherHost : MenuCommand
    {
        public MenuCommandWmicLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a WmicLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            WmicLauncherMenuItem wmicMenuItem = (WmicLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            wmicMenuItem.wmicLauncher = this.CovenantClient.ApiLaunchersWmicPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(wmicMenuItem.wmicLauncher.ListenerId ?? default);
            if (listener == null)
            {
                EliteConsole.PrintFormattedErrorLine("Can only host a file on a valid HttpListener.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (!commands[1].EndsWith(".xsl"))
            {
                EliteConsole.PrintFormattedErrorLine("WmicLaunchers must end with the extension: .xsl");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            HostedFile fileToHost = new HostedFile
            {
                ListenerId = listener.Id,
                Path = commands[1],
                Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(wmicMenuItem.wmicLauncher.DiskCode))
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            wmicMenuItem.wmicLauncher = this.CovenantClient.ApiLaunchersWmicHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("WmicLauncher hosted at: " + hostedLocation);
            EliteConsole.PrintFormattedInfoLine("Launcher (cmd.exe):        " + wmicMenuItem.wmicLauncher.LauncherString);
            EliteConsole.PrintFormattedInfoLine("Launcher (powershell.exe): " + wmicMenuItem.wmicLauncher.LauncherString.Replace("\"", "`\""));
        }
    }

    public class MenuCommandWmicLauncherWriteFile : MenuCommand
    {
        public MenuCommandWmicLauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write xls to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            WmicLauncherMenuItem wmicLauncherMenuItem = ((WmicLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                wmicLauncherMenuItem.Refresh();
                if (wmicLauncherMenuItem.wmicLauncher.LauncherString == "")
                {
                    wmicLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    wmicLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated WmicLauncher: " + wmicLauncherMenuItem.wmicLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, wmicLauncherMenuItem.wmicLauncher.DiskCode);
                EliteConsole.PrintFormattedHighlightLine("Wrote WmicLauncher's xls to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandWmicLauncherSet : MenuCommand
    {
        public MenuCommandWmicLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set WmicLauncher option";
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
                        new MenuCommandParameterValue {
                            Value = "ScriptLanguage",
                            NextValueSuggestions = new List<string> { "JScript", "VBScript" }
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
            WmicLauncher wmicLauncher = ((WmicLauncherMenuItem)menuItem).wmicLauncher;
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
                        wmicLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "scriptlanguage")
                {
                    if (commands[2].ToLower().StartsWith("js"))
                    {
                        wmicLauncher.ScriptLanguage = ScriptingLanguage.JScript;
                    }
                    else if (commands[2].ToLower().StartsWith("vb"))
                    {
                        wmicLauncher.ScriptLanguage = ScriptingLanguage.VBScript;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid ScriptLanguage \"" + commands[2] + "\". Valid options are: JScript, VBScript");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                }
                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        wmicLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        wmicLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
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
                        wmicLauncher.CommType = CommunicationType.SMB;
                    }
                    else
                    {
                        wmicLauncher.CommType = CommunicationType.HTTP;
                    }
                }
                else if (commands[1].ToLower() == "smbpipename")
                {
                    wmicLauncher.SmbPipeName = commands[2];
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    wmicLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    wmicLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    wmicLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    wmicLauncher.LauncherString = commands[2];
                }
                this.CovenantClient.ApiLaunchersWmicPut(wmicLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class WmicLauncherMenuItem : MenuItem
    {
        public WmicLauncher wmicLauncher { get; set; }

		public WmicLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.wmicLauncher = CovenantClient.ApiLaunchersWmicGet();
            this.MenuTitle = wmicLauncher.Name;
            this.MenuDescription = wmicLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandWmicLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandWmicLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandWmicLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandWmicLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandWmicLauncherWriteFile());
            var setCommand = new MenuCommandWmicLauncherSet(CovenantClient);
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
            this.wmicLauncher = this.CovenantClient.ApiLaunchersWmicGet();
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
