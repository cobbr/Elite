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
    public class MenuCommandCscriptLauncherShow : MenuCommand
    {
        public MenuCommandCscriptLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show CscriptLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            CscriptLauncherMenuItem cscriptMenuItem = (CscriptLauncherMenuItem)menuItem;
            cscriptMenuItem.cscriptLauncher = this.CovenantClient.ApiLaunchersCscriptGet();
            CscriptLauncher launcher = cscriptMenuItem.cscriptLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == cscriptMenuItem.cscriptLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "CscriptLauncher");
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

    public class MenuCommandCscriptLauncherGenerate : MenuCommand
    {
        public MenuCommandCscriptLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a Cscript stager";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            CscriptLauncherMenuItem cscriptMenuItem = (CscriptLauncherMenuItem)menuItem;
            cscriptMenuItem.cscriptLauncher = this.CovenantClient.ApiLaunchersCscriptPost();
            EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + cscriptMenuItem.cscriptLauncher.LauncherString);
        }
    }

    public class MenuCommandCscriptLauncherCode : MenuCommand
    {
        public MenuCommandCscriptLauncherCode() : base()
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
            CscriptLauncherMenuItem cscriptMenuItem = (CscriptLauncherMenuItem)menuItem;
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
            cscriptMenuItem.Refresh();
            if (cscriptMenuItem.cscriptLauncher.LauncherString == "")
            {
                cscriptMenuItem.CovenantClient.ApiLaunchersCscriptPost();
                cscriptMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + cscriptMenuItem.cscriptLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(cscriptMenuItem.cscriptLauncher.StagerCode);
            }
            else if(commands.Length == 2 && commands[1].ToLower() == "scriptlet")
            {
                EliteConsole.PrintInfoLine(cscriptMenuItem.cscriptLauncher.DiskCode);
            }
        }
    }

    public class MenuCommandCscriptLauncherHost : MenuCommand
    {
        public MenuCommandCscriptLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a CscriptLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            CscriptLauncherMenuItem cscriptMenuItem = (CscriptLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            cscriptMenuItem.cscriptLauncher = this.CovenantClient.ApiLaunchersCscriptPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(cscriptMenuItem.cscriptLauncher.ListenerId ?? default);
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
                Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(cscriptMenuItem.cscriptLauncher.DiskCode))
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            cscriptMenuItem.cscriptLauncher = this.CovenantClient.ApiLaunchersCscriptHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("CscriptLauncher hosted at: " + hostedLocation);
            EliteConsole.PrintFormattedWarningLine("cscript.exe cannot execute remotely hosted files, the payload must first be written to disk");
            EliteConsole.PrintFormattedInfoLine("Launcher: " + cscriptMenuItem.cscriptLauncher.LauncherString);
        }
    }

    public class MenuCommandCscriptLauncherWriteFile : MenuCommand
    {
        public MenuCommandCscriptLauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write hta to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            CscriptLauncherMenuItem cscriptLauncherMenuItem = ((CscriptLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                cscriptLauncherMenuItem.Refresh();
                if (cscriptLauncherMenuItem.cscriptLauncher.LauncherString == "")
                {
                    cscriptLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    cscriptLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + cscriptLauncherMenuItem.cscriptLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, cscriptLauncherMenuItem.cscriptLauncher.DiskCode);
                EliteConsole.PrintFormattedHighlightLine("Wrote CscriptLauncher to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandCscriptLauncherSet : MenuCommand
    {
        public MenuCommandCscriptLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set CscriptLauncher option";
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
                            Value = "ScriptLanguage",
                            NextValueSuggestions = new List<string> { "JScript", "VBScript" }
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
            CscriptLauncher cscriptLauncher = ((CscriptLauncherMenuItem)menuItem).cscriptLauncher;
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
                        cscriptLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "commtype")
                {
                    if (commands[2].ToLower() == "smb")
                    {
                        cscriptLauncher.CommType = CommunicationType.SMB;
                    }
                    else
                    {
                        cscriptLauncher.CommType = CommunicationType.HTTP;
                    }
                }
                else if (commands[1].ToLower() == "smbpipename")
                {
                    cscriptLauncher.SmbPipeName = commands[2];
                }

                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        cscriptLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        cscriptLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid DotNetFrameworkVersion \"" + commands[2] + "\". Valid options are: v3.5, v4.0");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                }
                else if (commands[1].ToLower() == "scriptlanguage")
                {
                    if (commands[2].ToLower().StartsWith("js"))
                    {
                        cscriptLauncher.ScriptLanguage = ScriptingLanguage.JScript;
                    }
                    else if (commands[2].ToLower().StartsWith("vb"))
                    {
                        cscriptLauncher.ScriptLanguage = ScriptingLanguage.VBScript;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid ScriptLanguage \"" + commands[2] + "\". Valid options are: JScript, VBScript");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    cscriptLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    cscriptLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    cscriptLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    cscriptLauncher.LauncherString = commands[2];
                }

                CovenantAPIExtensions.ApiLaunchersCscriptPut(this.CovenantClient, cscriptLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class CscriptLauncherMenuItem : MenuItem
    {
        public CscriptLauncher cscriptLauncher { get; set; }

		public CscriptLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.cscriptLauncher = CovenantClient.ApiLaunchersCscriptGet();
            this.MenuTitle = cscriptLauncher.Name;
            this.MenuDescription = cscriptLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandCscriptLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandCscriptLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandCscriptLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandCscriptLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandCscriptLauncherWriteFile());
            var setCommand = new MenuCommandCscriptLauncherSet(CovenantClient);
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
            this.cscriptLauncher = this.CovenantClient.ApiLaunchersCscriptGet();
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
