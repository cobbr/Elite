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
    public class MenuCommandMshtaLauncherShow : MenuCommand
    {
        public MenuCommandMshtaLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show MshtaLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                MshtaLauncherMenuItem mshtaMenuItem = (MshtaLauncherMenuItem)menuItem;
                mshtaMenuItem.mshtaLauncher = this.CovenantClient.ApiLaunchersMshtaGet();
                MshtaLauncher launcher = mshtaMenuItem.mshtaLauncher;
                Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == mshtaMenuItem.mshtaLauncher.ListenerId);

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "MshtaLauncher");
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
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandMshtaLauncherGenerate : MenuCommand
    {
        public MenuCommandMshtaLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a MshtaLauncher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                MshtaLauncherMenuItem mshtaMenuItem = (MshtaLauncherMenuItem)menuItem;
                mshtaMenuItem.mshtaLauncher = this.CovenantClient.ApiLaunchersMshtaPost();
                EliteConsole.PrintFormattedHighlightLine("Generated MshtaLauncher: " + mshtaMenuItem.mshtaLauncher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandMshtaLauncherCode : MenuCommand
    {
        public MenuCommandMshtaLauncherCode() : base()
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
                MshtaLauncherMenuItem mshtaMenuItem = (MshtaLauncherMenuItem)menuItem;
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
                mshtaMenuItem.Refresh();
                if (mshtaMenuItem.mshtaLauncher.LauncherString == "")
                {
                    mshtaMenuItem.CovenantClient.ApiLaunchersMshtaPost();
                    mshtaMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated MshtaLauncher: " + mshtaMenuItem.mshtaLauncher.LauncherString);
                }
                if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
                {
                    EliteConsole.PrintInfoLine(mshtaMenuItem.mshtaLauncher.StagerCode);
                }
                else if (commands.Length == 2 && commands[1].ToLower() == "scriptlet")
                {
                    EliteConsole.PrintInfoLine(mshtaMenuItem.mshtaLauncher.DiskCode);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandMshtaLauncherHost : MenuCommand
    {
        public MenuCommandMshtaLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a MshtaLauncher on an HTTP Listener";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter { Name = "Path" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                MshtaLauncherMenuItem mshtaMenuItem = (MshtaLauncherMenuItem)menuItem;
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "host")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                mshtaMenuItem.mshtaLauncher = this.CovenantClient.ApiLaunchersMshtaPost();
                HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(mshtaMenuItem.mshtaLauncher.ListenerId ?? default);
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
                    Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(mshtaMenuItem.mshtaLauncher.DiskCode))
                };

                fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
                mshtaMenuItem.mshtaLauncher = this.CovenantClient.ApiLaunchersMshtaHostedPost(fileToHost);

                Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
                EliteConsole.PrintFormattedHighlightLine("MshtaLauncher hosted at: " + hostedLocation);
                EliteConsole.PrintFormattedInfoLine("Launcher: " + mshtaMenuItem.mshtaLauncher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandMshtaLauncherWriteFile : MenuCommand
    {
        public MenuCommandMshtaLauncherWriteFile()
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
            try
            {
                MshtaLauncherMenuItem mshtaLauncherMenuItem = ((MshtaLauncherMenuItem)menuItem);
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "write")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                }
                else
                {
                    mshtaLauncherMenuItem.Refresh();
                    if (mshtaLauncherMenuItem.mshtaLauncher.LauncherString == "")
                    {
                        mshtaLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                        mshtaLauncherMenuItem.Refresh();
                        EliteConsole.PrintFormattedHighlightLine("Generated MshtaLauncher: " + mshtaLauncherMenuItem.mshtaLauncher.LauncherString);
                    }

                    string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                    System.IO.File.WriteAllText(OutputFilePath, mshtaLauncherMenuItem.mshtaLauncher.DiskCode);
                    EliteConsole.PrintFormattedHighlightLine("Wrote MshtaLauncher's hta to: \"" + OutputFilePath + "\"");
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandMshtaLauncherSet : MenuCommand
    {
        public MenuCommandMshtaLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set MshtaLauncher option";
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
                MshtaLauncher mshtaLauncher = ((MshtaLauncherMenuItem)menuItem).mshtaLauncher;
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
                            mshtaLauncher.ListenerId = listener.Id;
                        }
                    }
                    else if (commands[1].ToLower() == "dotnetframeworkversion")
                    {
                        if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                        {
                            mshtaLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                        }
                        else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                        {
                            mshtaLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
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
                            mshtaLauncher.CommType = CommunicationType.SMB;
                        }
                        else
                        {
                            mshtaLauncher.CommType = CommunicationType.HTTP;
                        }
                    }
                    else if (commands[1].ToLower() == "smbpipename")
                    {
                        mshtaLauncher.SmbPipeName = commands[2];
                    }
                    else if (commands[1].ToLower() == "scriptlanguage")
                    {
                        if (commands[2].ToLower().StartsWith("js"))
                        {
                            mshtaLauncher.ScriptLanguage = ScriptingLanguage.JScript;
                        }
                        else if (commands[2].ToLower().StartsWith("vb"))
                        {
                            mshtaLauncher.ScriptLanguage = ScriptingLanguage.VBScript;
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
                        mshtaLauncher.Delay = n;
                    }
                    else if (commands[1].ToLower() == "jitter")
                    {
                        int.TryParse(commands[2], out int n);
                        mshtaLauncher.Jitter = n;
                    }
                    else if (commands[1].ToLower() == "connectattempts")
                    {
                        int.TryParse(commands[2], out int n);
                        mshtaLauncher.ConnectAttempts = n;
                    }
                    else if (commands[1].ToLower() == "launcherstring")
                    {
                        mshtaLauncher.LauncherString = commands[2];
                    }
                    this.CovenantClient.ApiLaunchersMshtaPut(mshtaLauncher);
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

    public class MshtaLauncherMenuItem : MenuItem
    {
        public MshtaLauncher mshtaLauncher { get; set; }

		public MshtaLauncherMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            try
            {
                this.mshtaLauncher = CovenantClient.ApiLaunchersMshtaGet();
                this.MenuTitle = mshtaLauncher.Name;
                this.MenuDescription = mshtaLauncher.Description;

                this.AdditionalOptions.Add(new MenuCommandMshtaLauncherShow(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandMshtaLauncherGenerate(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandMshtaLauncherCode());
                this.AdditionalOptions.Add(new MenuCommandMshtaLauncherHost(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandMshtaLauncherWriteFile());
                var setCommand = new MenuCommandMshtaLauncherSet(CovenantClient);
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
                this.mshtaLauncher = this.CovenantClient.ApiLaunchersMshtaGet();
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
