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
            try
            {
                menuItem.Refresh();
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == launcher.ListenerId);

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "CscriptLauncher");
                menu.Rows.Add(new List<string> { "Name:", launcher.Name });
                menu.Rows.Add(new List<string> { "Description:", launcher.Description });
                menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
                menu.Rows.Add(new List<string> { "CommType:", launcher.CommType.ToString() });
                if (launcher.CommType == CommunicationType.HTTP)
                {
                    menu.Rows.Add(new List<string> { "  ValidateCert:", launcher.ValidateCert.ToString() });
                    menu.Rows.Add(new List<string> { "  UseCertPinning:", launcher.UseCertPinning.ToString() });
                }
                else if (launcher.CommType == CommunicationType.SMB)
                {
                    menu.Rows.Add(new List<string> { "  SMBPipeName:", launcher.SmbPipeName });
                }
                menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion == DotNetVersion.Net35 ? "v3.5" : "v4.0" });
                menu.Rows.Add(new List<string> { "ScriptLanguage:", launcher.ScriptLanguage.ToString() });
                menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
                menu.Rows.Add(new List<string> { "JitterPercent:", (launcher.JitterPercent ?? default).ToString() });
                menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
                menu.Rows.Add(new List<string> { "KillDate:", launcher.KillDate.ToString() });
                menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
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
            try
            {
                this.CovenantClient.ApiLaunchersCscriptPost();
                menuItem.Refresh();
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + launcher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandCscriptLauncherCode : MenuCommand
    {
        public MenuCommandCscriptLauncherCode(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager or Scriptlet code.";
            this.Parameters = new List<MenuCommandParameter>
            {
                new MenuCommandParameter {
                    Name = "Type",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Scriptlet" },
                        new MenuCommandParameterValue { Value = "GruntStager" }
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length < 1 || commands.Length > 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                if (commands.Length == 2 && (!new List<string> { "gruntstager", "scriptlet" }.Contains(commands[1], StringComparer.OrdinalIgnoreCase)))
                {
                    EliteConsole.PrintFormattedErrorLine("Type must be one of: \"GruntStager\" or \"Scriptlet\"");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                if (launcher.LauncherString == "")
                {
                    this.CovenantClient.ApiLaunchersCscriptPost();
                    menuItem.Refresh();
                    launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                    EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + launcher.LauncherString);
                }
                if (commands.Length == 1 || (commands.Length == 2 && commands[1].Equals("gruntstager", StringComparison.OrdinalIgnoreCase)))
                {
                    EliteConsole.PrintInfoLine(launcher.StagerCode);
                }
                else if (commands.Length == 2 && commands[1].Equals("scriptlet", StringComparison.OrdinalIgnoreCase))
                {
                    EliteConsole.PrintInfoLine(launcher.DiskCode);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
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
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                this.CovenantClient.ApiLaunchersCscriptPost();
                menuItem.Refresh();
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(launcher.ListenerId ?? default);
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
                    Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(launcher.DiskCode))
                };

                fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
                launcher = this.CovenantClient.ApiLaunchersCscriptHostedPost(fileToHost);

                Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
                EliteConsole.PrintFormattedHighlightLine("CscriptLauncher hosted at: " + hostedLocation);
                EliteConsole.PrintFormattedWarningLine("cscript.exe cannot execute remotely hosted files, the payload must first be written to disk");
                EliteConsole.PrintFormattedInfoLine("Launcher: " + launcher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandCscriptLauncherWriteFile : MenuCommand
    {
        public MenuCommandCscriptLauncherWriteFile(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Write";
            this.Description = "Write CscriptLauncher to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Output File" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                menuItem.Refresh();
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                if (launcher.LauncherString == "")
                {
                    this.CovenantClient.ApiLaunchersBinaryPost();
                    menuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated CscriptLauncher: " + launcher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, launcher.DiskCode);
                EliteConsole.PrintFormattedHighlightLine("Wrote CscriptLauncher to: \"" + OutputFilePath + "\"");
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandCscriptLauncherSet : MenuCommand
    {
        public MenuCommandCscriptLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set CscriptLauncher option";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter {
                        Name = "Option",
                        Values = new List<MenuCommandParameterValue> {
                            new MenuCommandParameterValue { Value = "ListenerName" },
                            new MenuCommandParameterValue {
                                Value = "CommType",
                                NextValueSuggestions = new List<string> { "HTTP", "SMB" }
                            },
                            new MenuCommandParameterValue { Value = "SMBPipeName" },
                            new MenuCommandParameterValue { Value = "ValidateCert" },
                            new MenuCommandParameterValue { Value = "UseCertPinning" },
                            new MenuCommandParameterValue {
                                Value = "DotNetFrameworkVersion",
                                NextValueSuggestions = new List<string> { "net35", "net40" }
                            },
                            new MenuCommandParameterValue {
                                Value = "ScriptLanguage",
                                NextValueSuggestions = new List<string> { "JScript", "VBScript" }
                            },
                            new MenuCommandParameterValue { Value = "Delay" },
                            new MenuCommandParameterValue { Value = "JitterPercent" },
                            new MenuCommandParameterValue { Value = "ConnectAttempts" },
                            new MenuCommandParameterValue { Value = "KillDate" },
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

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                List<string> commands = Utilities.ParseParameters(UserInput);
                if (commands.Count() != 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                CscriptLauncher launcher = ((CscriptLauncherMenuItem)menuItem).CscriptLauncher;
                if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value).Contains(commands[1], StringComparer.OrdinalIgnoreCase))
                {
                    if (commands[1].Equals("listenername", StringComparison.OrdinalIgnoreCase))
                    {
                        Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == commands[2]);
                        if (listener == null || listener.Name != commands[2])
                        {
                            EliteConsole.PrintFormattedErrorLine("Invalid ListenerName: \"" + commands[2] + "\"");
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                        launcher.ListenerId = listener.Id;
                    }
                    else if (commands[1].Equals("dotnetframeworkversion", StringComparison.OrdinalIgnoreCase))
                    {
                        if (commands[2].Contains("35", StringComparison.OrdinalIgnoreCase) || commands[2].Contains("3.5", StringComparison.OrdinalIgnoreCase))
                        {
                            launcher.DotNetFrameworkVersion = DotNetVersion.Net35;
                        }
                        else if (commands[2].Contains("40", StringComparison.OrdinalIgnoreCase) || commands[2].Contains("4.0", StringComparison.OrdinalIgnoreCase))
                        {
                            launcher.DotNetFrameworkVersion = DotNetVersion.Net40;
                        }
                        else
                        {
                            EliteConsole.PrintFormattedErrorLine("Invalid DotNetFrameworkVersion \"" + commands[2] + "\". Valid options are: v3.5, v4.0");
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].Equals("commtype", StringComparison.OrdinalIgnoreCase))
                    {
                        if (commands[2].Equals("smb", StringComparison.OrdinalIgnoreCase))
                        {
                            launcher.CommType = CommunicationType.SMB;
                        }
                        else
                        {
                            launcher.CommType = CommunicationType.HTTP;
                        }
                    }
                    else if (commands[1].Equals("validatecert", StringComparison.OrdinalIgnoreCase))
                    {
                        bool parsed = bool.TryParse(commands[2], out bool validate);
                        if (parsed)
                        {
                            launcher.ValidateCert = validate;
                        }
                        else
                        {
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].Equals("usecertpinning", StringComparison.OrdinalIgnoreCase))
                    {
                        bool parsed = bool.TryParse(commands[2], out bool pin);
                        if (parsed)
                        {
                            launcher.UseCertPinning = pin;
                        }
                        else
                        {
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].Equals("smbpipename", StringComparison.OrdinalIgnoreCase))
                    {
                        launcher.SmbPipeName = commands[2];
                    }
                    else if (commands[1].Equals("scriptlanguage", StringComparison.OrdinalIgnoreCase))
                    {
                        if (commands[2].StartsWith("js", StringComparison.OrdinalIgnoreCase))
                        {
                            launcher.ScriptLanguage = ScriptingLanguage.JScript;
                        }
                        else if (commands[2].StartsWith("vb", StringComparison.OrdinalIgnoreCase))
                        {
                            launcher.ScriptLanguage = ScriptingLanguage.VBScript;
                        }
                        else
                        {
                            EliteConsole.PrintFormattedErrorLine("Invalid ScriptLanguage \"" + commands[2] + "\". Valid options are: JScript, VBScript");
                            menuItem.PrintInvalidOptionError(UserInput);
                            return;
                        }
                    }
                    else if (commands[1].Equals("delay", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(commands[2], out int n);
                        launcher.Delay = n;
                    }
                    else if (commands[1].Equals("jitterpercent", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(commands[2], out int n);
                        launcher.JitterPercent = n;
                    }
                    else if (commands[1].Equals("connectattempts", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(commands[2], out int n);
                        launcher.ConnectAttempts = n;
                    }
                    else if (commands[1].Equals("killdate", StringComparison.OrdinalIgnoreCase))
                    {
                        DateTime.TryParse(commands[2], out DateTime result);
                        launcher.KillDate = result;
                    }
                    else if (commands[1].Equals("launcherstring", StringComparison.OrdinalIgnoreCase))
                    {
                        launcher.LauncherString = commands[2];
                    }
                    await this.CovenantClient.ApiLaunchersCscriptPutAsync(launcher);
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

    public class CscriptLauncherMenuItem : MenuItem
    {
        public CscriptLauncher CscriptLauncher { get; set; }

		public CscriptLauncherMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            try
            {
                this.CscriptLauncher = CovenantClient.ApiLaunchersCscriptGet();
                this.MenuTitle = CscriptLauncher.Name;
                this.MenuDescription = CscriptLauncher.Description;

                this.AdditionalOptions.Add(new MenuCommandCscriptLauncherShow(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandCscriptLauncherGenerate(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandCscriptLauncherCode(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandCscriptLauncherHost(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandCscriptLauncherWriteFile(CovenantClient));
                var setCommand = new MenuCommandCscriptLauncherSet(CovenantClient);
                this.AdditionalOptions.Add(setCommand);
                this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));
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
                this.CscriptLauncher = this.CovenantClient.ApiLaunchersCscriptGet();

                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Set").Parameters
                    .FirstOrDefault(P => P.Name == "Option").Values
                        .FirstOrDefault(V => V.Value == "ListenerName")
                        .NextValueSuggestions = this.CovenantClient.ApiListenersGet()
                            .Where(L => L.Status == ListenerStatus.Active)
                            .Select(L => L.Name)
                            .ToList();

                var filevalues = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder);
                this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Write").Parameters
                    .FirstOrDefault().Values = filevalues;

                this.SetupMenuAutoComplete();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }
}
