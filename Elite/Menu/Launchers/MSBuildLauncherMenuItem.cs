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
    public class MenuCommandMSBuildLauncherShow : MenuCommand
    {
        public MenuCommandMSBuildLauncherShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show MSBuildLauncher options";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            MSBuildLauncherMenuItem msbuildMenuItem = (MSBuildLauncherMenuItem)menuItem;
            msbuildMenuItem.msbuildLauncher = this.CovenantClient.ApiLaunchersMsbuildGet();
            MSBuildLauncher launcher = msbuildMenuItem.msbuildLauncher;
            Listener listener = this.CovenantClient.ApiListenersGet().FirstOrDefault(L => L.Id == msbuildMenuItem.msbuildLauncher.ListenerId);

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "MSBuildLauncher");
            menu.Rows.Add(new List<string> { "Name:", launcher.Name });
            menu.Rows.Add(new List<string> { "Description:", launcher.Description });
            menu.Rows.Add(new List<string> { "ListenerName:", listener == null ? "" : listener.Name });
            menu.Rows.Add(new List<string> { "TargetName:", launcher.TargetName });
            menu.Rows.Add(new List<string> { "TaskName:", launcher.TaskName });
            menu.Rows.Add(new List<string> { "DotNetFramework:", launcher.DotNetFrameworkVersion.ToString() });
            menu.Rows.Add(new List<string> { "Delay:", (launcher.Delay ?? default).ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", (launcher.Jitter ?? default).ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", (launcher.ConnectAttempts ?? default).ToString() });
            menu.Rows.Add(new List<string> { "LauncherString:", launcher.LauncherString });
            menu.Print();
        }
    }

    public class MenuCommandMSBuildLauncherGenerate : MenuCommand
    {
        public MenuCommandMSBuildLauncherGenerate(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Generate";
            this.Description = "Generate a MSBuildLauncher";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            MSBuildLauncherMenuItem msbuildMenuItem = (MSBuildLauncherMenuItem)menuItem;
            msbuildMenuItem.msbuildLauncher = this.CovenantClient.ApiLaunchersMsbuildPost();
            EliteConsole.PrintFormattedHighlightLine("Generated MSBuildLauncher: " + msbuildMenuItem.msbuildLauncher.LauncherString);
        }
    }

    public class MenuCommandMSBuildLauncherCode : MenuCommand
    {
        public MenuCommandMSBuildLauncherCode() : base()
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
            MSBuildLauncherMenuItem msbuildMenuItem = (MSBuildLauncherMenuItem)menuItem;
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
            msbuildMenuItem.Refresh();
            if (msbuildMenuItem.msbuildLauncher.LauncherString == "")
            {
                msbuildMenuItem.CovenantClient.ApiLaunchersMsbuildPost();
                msbuildMenuItem.Refresh();
                EliteConsole.PrintFormattedHighlightLine("Generated MSBuildLauncher: " + msbuildMenuItem.msbuildLauncher.LauncherString);
            }
            if (commands.Length == 1 || (commands.Length == 2 && (commands[1].ToLower() == "stager" || commands[1].ToLower() == "gruntstager")))
            {
                EliteConsole.PrintInfoLine(msbuildMenuItem.msbuildLauncher.StagerCode);
            }
            else if (commands.Length == 2 && commands[1].ToLower() == "xml")
            {
                EliteConsole.PrintInfoLine(msbuildMenuItem.msbuildLauncher.DiskCode);
            }
        }
    }

    public class MenuCommandMSBuildLauncherHost : MenuCommand
    {
        public MenuCommandMSBuildLauncherHost(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Host";
            this.Description = "Host a MSBuildLauncher on an HTTP Listener";
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
            MSBuildLauncherMenuItem msbuildMenuItem = (MSBuildLauncherMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "host")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            msbuildMenuItem.msbuildLauncher = this.CovenantClient.ApiLaunchersMsbuildPost();
            HttpListener listener = this.CovenantClient.ApiListenersHttpByIdGet(msbuildMenuItem.msbuildLauncher.ListenerId ?? default);
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
                Content = Convert.ToBase64String(Common.CovenantEncoding.GetBytes(msbuildMenuItem.msbuildLauncher.DiskCode))
            };

            fileToHost = this.CovenantClient.ApiListenersByIdHostedfilesPost(listener.Id ?? default, fileToHost);
            msbuildMenuItem.msbuildLauncher = this.CovenantClient.ApiLaunchersMsbuildHostedPost(fileToHost);

            Uri hostedLocation = new Uri(listener.Url + fileToHost.Path);
            EliteConsole.PrintFormattedHighlightLine("MSBuildLauncher hosted at: " + hostedLocation);
            EliteConsole.PrintFormattedWarningLine("msbuild.exe cannot execute remotely hosted files, the payload must first be written to disk");
            EliteConsole.PrintFormattedInfoLine("Launcher: " + msbuildMenuItem.msbuildLauncher.LauncherString);

        }
    }

    public class MenuCommandMSBuildLauncherWriteFile : MenuCommand
    {
        public MenuCommandMSBuildLauncherWriteFile()
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
            MSBuildLauncherMenuItem msbuildLauncherMenuItem = ((MSBuildLauncherMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "write")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                msbuildLauncherMenuItem.Refresh();
                if (msbuildLauncherMenuItem.msbuildLauncher.LauncherString == "")
                {
                    msbuildLauncherMenuItem.CovenantClient.ApiLaunchersBinaryPost();
                    msbuildLauncherMenuItem.Refresh();
                    EliteConsole.PrintFormattedHighlightLine("Generated MSBuildLauncher: " + msbuildLauncherMenuItem.msbuildLauncher.LauncherString);
                }

                string OutputFilePath = Common.EliteDataFolder + String.Concat(commands[1].Split(System.IO.Path.GetInvalidFileNameChars()));
                System.IO.File.WriteAllText(OutputFilePath, msbuildLauncherMenuItem.msbuildLauncher.DiskCode);
                EliteConsole.PrintFormattedHighlightLine("Wrote MSBuildLauncher to: \"" + OutputFilePath + "\"");
            }
        }
    }

    public class MenuCommandMSBuildLauncherSet : MenuCommand
    {
        public MenuCommandMSBuildLauncherSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set MSBuildLauncher option";
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
                        new MenuCommandParameterValue { Value = "TargetName" },
                        new MenuCommandParameterValue { Value = "TaskName" },
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
            MSBuildLauncher msbuildLauncher = ((MSBuildLauncherMenuItem)menuItem).msbuildLauncher;
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
                        msbuildLauncher.ListenerId = listener.Id;
                    }
                }
                else if (commands[1].ToLower() == "targetname")
                {
                    msbuildLauncher.TargetName = commands[2];
                }
                else if (commands[1].ToLower() == "taskname")
                {
                    msbuildLauncher.TaskName = commands[2];
                }
                else if (commands[1].ToLower() == "dotnetframeworkversion")
                {
                    if (commands[2].ToLower().Contains("35") || commands[2].ToLower().Contains("3.5"))
                    {
                        msbuildLauncher.DotNetFrameworkVersion = DotNetVersion.Net35;
                    }
                    else if (commands[2].ToLower().Contains("40") || commands[2].ToLower().Contains("4.0"))
                    {
                        msbuildLauncher.DotNetFrameworkVersion = DotNetVersion.Net40;
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Invalid DotNetFrameworkVersion \"" + commands[2] + "\". Valid options are: v3.5, v4.0");
                        menuItem.PrintInvalidOptionError(UserInput);
                        return;
                    }
                }
                else if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    msbuildLauncher.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    msbuildLauncher.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    msbuildLauncher.ConnectAttempts = n;
                }
                else if (commands[1].ToLower() == "launcherstring")
                {
                    msbuildLauncher.LauncherString = commands[2];
                }
                CovenantAPIExtensions.ApiLaunchersMsbuildPut(this.CovenantClient, msbuildLauncher);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class MSBuildLauncherMenuItem : MenuItem
    {
        public MSBuildLauncher msbuildLauncher { get; set; }

        public MSBuildLauncherMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.msbuildLauncher = CovenantClient.ApiLaunchersMsbuildGet();
            this.MenuTitle = msbuildLauncher.Name;
            this.MenuDescription = msbuildLauncher.Description;

            this.AdditionalOptions.Add(new MenuCommandMSBuildLauncherShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandMSBuildLauncherGenerate(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandMSBuildLauncherCode());
            this.AdditionalOptions.Add(new MenuCommandMSBuildLauncherHost(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandMSBuildLauncherWriteFile());
            var setCommand = new MenuCommandMSBuildLauncherSet(CovenantClient);
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
            this.msbuildLauncher = this.CovenantClient.ApiLaunchersMsbuildGet();
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
