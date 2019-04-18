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
            try
            {
                menuItem.Refresh();
                PowerShellLauncher launcher = ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher;
                Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == launcher.ListenerId);

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "PowerShellLauncher");
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
                menu.Rows.Add(new List<string> { "ParameterString:", launcher.ParameterString });
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

    public class MenuCommandPowerShellLauncherGenerate : MenuCommand
    {
        public MenuCommandPowerShellLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a PowerShellLauncher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                this.CovenantClient.ApiLaunchersPowershellPost();
                menuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher.LauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandPowerShellLauncherCode : MenuCommand
    {
        public MenuCommandPowerShellLauncherCode(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Code";
            this.Description = "Get the currently generated GruntStager or PowerShell code.";
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
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length < 1 || commands.Length > 2 || commands[0].ToLower() != "code")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                if (commands.Length == 2 && (!new List<string> { "gruntstager", "powershell" }.Contains(commands[1], StringComparer.OrdinalIgnoreCase)))
                {
                    EliteConsole.PrintFormattedErrorLine("Type must be one of: \"GruntStager\" or \"PowerShell\"");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                PowerShellLauncher launcher = ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher;
                if (launcher.LauncherString == "")
                {
                    this.CovenantClient.ApiLaunchersPowershellPost();
                    menuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + launcher.LauncherString);
                }
                if (commands.Length == 1 || (commands.Length == 2 && commands[1].Equals("gruntstager", StringComparison.OrdinalIgnoreCase)))
                {
                    EliteConsole.PrintInfoLine(launcher.StagerCode);
                }
                else if (commands.Length == 2 && commands[1].Equals("powershell", StringComparison.OrdinalIgnoreCase))
                {
                    EliteConsole.PrintInfoLine(launcher.PowerShellCode);
                }
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
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
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                this.CovenantClient.ApiLaunchersPowershellPost();
                menuItem.Refresh();
                PowerShellLauncher launcher = ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher;
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
                    Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(launcher.PowerShellCode))
                };

                fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
                launcher = this.CovenantClient.ApiLaunchersPowershellHostedPost(fileToHost);

                Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
                EliteConsole.PrintFormattedHighlightLine("PowerShellLauncher hosted at: " + hostedLocation);
                EliteConsole.PrintFormattedInfoLine("Launcher (Command):        " + launcher.LauncherString);
                EliteConsole.PrintFormattedInfoLine("Launcher (EncodedCommand): " + launcher.EncodedLauncherString);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandPowerShellLauncherWriteFile : MenuCommand
    {
        public MenuCommandPowerShellLauncherWriteFile(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Write";
            this.Description = "Write PowerShellLauncher to a file";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Output File" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            try
            {
                string[] commands = UserInput.Split(" ");
                if (commands.Length != 2 || commands[0].ToLower() != "write")
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                menuItem.Refresh();
                PowerShellLauncher launcher = ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher;
                if (launcher.LauncherString == "")
                {
                    this.CovenantClient.ApiLaunchersBinaryPost();
                    menuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated PowerShellLauncher: " + launcher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, launcher.LauncherString);
                EliteConsole.PrintFormattedHighlightLine("Wrote PowerShellLauncher to: \"" + OutputFilePath + "\"");
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandPowerShellLauncherSet : MenuCommand
    {
        public MenuCommandPowerShellLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set PowerShellLauncher option";
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
                            new MenuCommandParameterValue { Value = "ParameterString" },
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
                PowerShellLauncher launcher = ((PowerShellLauncherMenuItem)menuItem).PowerShellLauncher;
                if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value).Contains(commands[1], StringComparer.OrdinalIgnoreCase))
                {
                    if (commands[1].Equals("listenername", StringComparison.OrdinalIgnoreCase))
                    {
                        Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Name == commands[2]);
                        if (listener == null || listener.Name != commands[2])
                        {
                            EliteConsole.PrintFormattedErrorLine("Invalid ListenerName");
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
                    }
                    else if (commands[1].Equals("parameterstring", StringComparison.OrdinalIgnoreCase))
                    {
                        launcher.ParameterString = String.Join(" ", commands.TakeLast(commands.Count() - 2).ToList());
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
                    await this.CovenantClient.ApiLaunchersPowershellPutAsync(launcher);
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

    public class PowerShellLauncherMenuItem : MenuItem
    {
        public PowerShellLauncher PowerShellLauncher { get; set; }

		public PowerShellLauncherMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            try
            {
                this.PowerShellLauncher = CovenantClient.ApiLaunchersPowershellGet();
                this.MenuTitle = PowerShellLauncher.Name;
                this.MenuDescription = PowerShellLauncher.Description;

                this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherShow(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherGenerate(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherCode(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherHost(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandPowerShellLauncherWriteFile(CovenantClient));
                var setCommand = new MenuCommandPowerShellLauncherSet(CovenantClient);
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
                this.PowerShellLauncher = this.CovenantClient.ApiLaunchersPowershellGet();

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
