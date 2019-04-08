// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Launchers
{
    public class MenuCommandRegsvr32LauncherShow : MenuCommand
    {
        public MenuCommandRegsvr32LauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show Regsvr32Launcher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Regsvr32LauncherMenuItem regsvr32MenuItem = (Regsvr32LauncherMenuItem)menuItem;
                regsvr32MenuItem.regsvr32Launcher = this.CovenantClient.ApiLaunchersRegsvr32Get();
                Regsvr32Launcher launcher = regsvr32MenuItem.regsvr32Launcher;
                Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == regsvr32MenuItem.regsvr32Launcher.ListenerId);

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Regsvr32Launcher");
                menu.Rows.Add(new List<string> { "Name:", launcher.Name });
                menu.Rows.Add(new List<string> { "Description:", launcher.Description });
                menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
                menu.Rows.Add(new List<string> { "CommType:", launcher.CommType.ToString() });
                menu.Rows.Add(new List<string> { "  SMBPipeName:", launcher.SmbPipeName });
                menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
                menu.Rows.Add(new List<string> { "ScriptLanguage:", launcher.ScriptLanguage.ToString() });
                menu.Rows.Add(new List<string> { "ParameterString:", launcher.ParameterString });
                menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
                menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
                menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
                menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandRegsvr32LauncherGenerate : MenuCommand
    {
        public MenuCommandRegsvr32LauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a Regsvr32Launcher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Regsvr32LauncherMenuItem regsvr32MenuItem = (Regsvr32LauncherMenuItem)menuItem;
                regsvr32MenuItem.regsvr32Launcher = this.CovenantClient.ApiLaunchersRegsvr32Post();
                EliteConsole.PrintFormattedHighlightLine(regsvr32MenuItem.regsvr32Launcher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandRegsvr32LauncherCode : MenuCommand
    {
        public MenuCommandRegsvr32LauncherCode() : base()
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
            try
            {
                Regsvr32LauncherMenuItem regsvr32MenuItem = (Regsvr32LauncherMenuItem)menuItem;
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
                regsvr32MenuItem.Refresh();
                if (regsvr32MenuItem.regsvr32Launcher.LauncherString == "")
                {
                    regsvr32MenuItem.CovenantClient.ApiLaunchersRegsvr32Post();
                    regsvr32MenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated Regsvr32Launcher: " + regsvr32MenuItem.regsvr32Launcher.LauncherString);
                }
                if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
                {
                    EliteConsole.PrintInfoLine(regsvr32MenuItem.regsvr32Launcher.StagerCode);
                }
                else if (commands.Length == 2 && commands[1].ToLower() == "scriptlet")
                {
                    EliteConsole.PrintInfoLine(regsvr32MenuItem.regsvr32Launcher.DiskCode);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandRegsvr32LauncherHost : MenuCommand
    {
        public MenuCommandRegsvr32LauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a Regsvr32 stager on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Regsvr32LauncherMenuItem regsvr32MenuItem = (Regsvr32LauncherMenuItem)menuItem;
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "host")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                regsvr32MenuItem.regsvr32Launcher = this.CovenantClient.ApiLaunchersRegsvr32Post();
                HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(regsvr32MenuItem.regsvr32Launcher.ListenerId ?? default);
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
                    Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(regsvr32MenuItem.regsvr32Launcher.DiskCode))
                };

                fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
                regsvr32MenuItem.regsvr32Launcher = this.CovenantClient.ApiLaunchersRegsvr32HostedPost(fileToHost);

                Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
                EliteConsole.PrintFormattedHighlightLine("Regsvr32Launcher hosted at: " + hostedLocation);
                EliteConsole.PrintFormattedInfoLine("Launcher: " + regsvr32MenuItem.regsvr32Launcher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandRegsvr32LauncherWriteFile : MenuCommand
    {
        public MenuCommandRegsvr32LauncherWriteFile()
        {
            this.Name = "Write";
            this.Description = "Write scriptlet to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Output File",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Regsvr32LauncherMenuItem regsvr32LauncherMenuItem = ((Regsvr32LauncherMenuItem)menuItem);
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "write")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else
                {
                    regsvr32LauncherMenuItem.Refresh();
                    if (regsvr32LauncherMenuItem.regsvr32Launcher.LauncherString == "")
                    {
                        regsvr32LauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                        regsvr32LauncherMenuItem.Refresh();
                        EliteConsole.PrintFormattedHighlightLine("Generated Regsvr32Launcher: " + regsvr32LauncherMenuItem.regsvr32Launcher.LauncherString);
                    }

                    string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                    System.IO.File.WriteAllText(OutputFilePath, regsvr32LauncherMenuItem.regsvr32Launcher.DiskCode);
                    EliteConsole.PrintFormattedHighlightLine("Wrote Regsvr32Launcher's ScriptletCode to: \"" + OutputFilePath + "\"");
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandRegsvr32LauncherSet : MenuCommand
    {
        public MenuCommandRegsvr32LauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set Regsvr32Launcher option";
            try
            {
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
                            new MenuCommandParameterValue {
                                Value = "ScriptLanguage",
                                NextValueSuggestions = new List<string> { "JScript", "VBScript" }
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
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                Regsvr32Launcher regsvr32Launcher = ((Regsvr32LauncherMenuItem)menuItem).regsvr32Launcher;
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
                            regsvr32Launcher.ListenerId = listener.Id;
                        }
                    }
                    else if (commands[1].ToLower() == "scriptlanguage")
                    {
                        if (commands[2].ToLower().StartsWith("js"))
                        {
                            regsvr32Launcher.ScriptLanguage = ScriptingLanguage.JScript;
                        }
                        else if (commands[2].ToLower().StartsWith("vb"))
                        {
                            regsvr32Launcher.ScriptLanguage = ScriptingLanguage.VBScript;
                        }
                        else
                        {
                            EliteConsole.PrintFormattedErrorLine("Invalid ScriptLanguage \"" + commands[2] + "\". Valid options are: JScript, VBScript");
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].ToLower() == "parameterstring")
                    {
                        regsvr32Launcher.ParameterString = String.Join(" ", commands.TakeLast(commands.Length - 2).ToList());
                    }
                    else if (commands[1].ToLower() == "dotnetframeworkversion")
                    {
                        if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                        {
                            regsvr32Launcher.DotNetFrameworkVersion = DotNetVersion.Net35;
                        }
                        else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                        {
                            regsvr32Launcher.DotNetFrameworkVersion = DotNetVersion.Net40;
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
                            regsvr32Launcher.CommType = CommunicationType.SMB;
                        }
                        else
                        {
                            regsvr32Launcher.CommType = CommunicationType.HTTP;
                        }
                    }
                    else if (commands[1].ToLower() == "smbpipename")
                    {
                        regsvr32Launcher.SmbPipeName = commands[2];
                    }
                    else if (commands[1].ToLower() == "delay")
                    {
                        int.TryParse(commands[2], out int n);
                        regsvr32Launcher.Delay = n;
                    }
                    else if (commands[1].ToLower() == "jitter")
                    {
                        int.TryParse(commands[2], out int n);
                        regsvr32Launcher.Jitter = n;
                    }
                    else if (commands[1].ToLower() == "connectattempts")
                    {
                        int.TryParse(commands[2], out int n);
                        regsvr32Launcher.ConnectAttempts = n;
                    }
                    else if (commands[1].ToLower() == "launcherstring")
                    {
                        regsvr32Launcher.LauncherString = commands[2];
                    }
                    this.CovenantClient.ApiLaunchersRegsvr32Put(regsvr32Launcher);
                }
                else
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class Regsvr32LauncherMenuItem : MenuItem
    {
        public Regsvr32Launcher regsvr32Launcher { get; set; }

		public Regsvr32LauncherMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            try
            {
                this.regsvr32Launcher = CovenantClient.ApiLaunchersRegsvr32Get();
                this.MenuTitle = regsvr32Launcher.Name;
                this.MenuDescription = regsvr32Launcher.Description;

                this.AdditionalOptions.Add(new MenuCommandRegsvr32LauncherShow(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandRegsvr32LauncherGenerate(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandRegsvr32LauncherCode());
                this.AdditionalOptions.Add(new MenuCommandRegsvr32LauncherHost(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandRegsvr32LauncherWriteFile());
                var setCommand = new MenuCommandRegsvr32LauncherSet(CovenantClient);
                this.AdditionalOptions.Add(setCommand);
                this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));

                this.Refresh();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void Refresh()
        {
            try
            {
                this.regsvr32Launcher = this.CovenantClient.ApiLaunchersRegsvr32Get();
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name.ToLower() == "set").Parameters
                    .FirstOrDefault(P => P.Name.ToLower() == "option").Values
                    .FirstOrDefault(V => V.Value.ToLower() == "listenername")
                    .NextValueSuggestions = this.CovenantClient.ApiListenersGet()
                                                .Where(L => L.Status == ListenerStatus.Active)
                                                .Select(L => L.Name).ToList();
                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}
