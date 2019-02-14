// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Tasks
{
    public class MenuCommandTaskShow : MenuCommand
    {
        public MenuCommandTaskShow(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Show";
            this.Description = "Show Task details.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            TaskMenuItem taskMenuItem = (TaskMenuItem)menuItem;
            // Refresh the task object
            taskMenuItem.task = this.CovenantClient.ApiGruntTasksByIdGet(taskMenuItem.task.Id ?? default);
            GruntTask task = taskMenuItem.task;

            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Task: " + task.Name);
            menu.Rows.Add(new List<string> { "Name:", task.Name });
            menu.Rows.Add(new List<string> { "Description:", task.Description });
            menu.Rows.Add(new List<string> { "ReferenceAssemblies:", task.ReferenceAssemblies });
            task.Options.ToList().ForEach(O =>
            {
                menu.Rows.Add(new List<string> { O.Name + ":", O.Value });
            });
            menu.Print();
        }
    }

    public class MenuCommandTaskSet : MenuCommand
    {
        public MenuCommandTaskSet(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Set";
            this.Description = "Set Task option";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Option" },
                new MenuCommandParameter { Name = "Value" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            TaskMenuItem taskMenuItem = ((TaskMenuItem)menuItem);
            List<string> commands = UserInput.Split(" ").ToList();
            GruntTaskOption option = taskMenuItem.task.Options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
            if (commands.Count() < 3 || commands.First().ToLower() != "set")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (option == null)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
            }
            else
            {
                option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                this.CovenantClient.ApiGruntTasksByIdPut(taskMenuItem.task.Id ?? default, taskMenuItem.task);
            }
        }
    }

    public class MenuCommandTaskStart : MenuCommand
    {
        public MenuCommandTaskStart(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Start";
            this.Description = "Start the Task";
            this.Parameters = new List<MenuCommandParameter> { };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            GruntTask task = ((TaskMenuItem)menuItem).task = this.CovenantClient.ApiGruntTasksByIdGet(((TaskMenuItem)menuItem).task.Id ?? default);
            Grunt grunt = ((TaskMenuItem)menuItem).grunt;
            GruntTasking gruntTasking = new GruntTasking { TaskId = task.Id, GruntId = grunt.Id };
            GruntTasking postedGruntTasking = this.CovenantClient.ApiGruntsByIdTaskingsPost(grunt.Id ?? default, gruntTasking);

            if (postedGruntTasking != null)
            {
                EliteConsole.PrintFormattedHighlightLine("Started Task: " + task.Name + " on Grunt: " + grunt.Name + " as GruntTask: " + postedGruntTasking.Name);
            }
        }
    }

    public class TaskMenuItem : MenuItem
    {
        public GruntTask task { get; set; }
        public Grunt grunt { get; set; }
		public TaskMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter, Grunt grunt) : base(CovenantClient, EventPrinter)
        {
            this.grunt = grunt;
            this.MenuTitle = "Task";
            this.MenuDescription = "Task a Grunt to do something.";
            this.MenuItemParameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Task Name",
                    Values = CovenantClient.ApiGruntTasksGet().Select(T => new MenuCommandParameterValue { Value = T.Name }).ToList()
                }
            };

            this.AdditionalOptions.Add(new MenuCommandTaskShow(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandTaskStart(CovenantClient));
            var setCommand = new MenuCommandTaskSet(CovenantClient);
            this.AdditionalOptions.Add(setCommand);
            this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));

            this.SetupMenuAutoComplete();
        }

		public override void Refresh()
		{
            MenuCommand setCommand = GetTaskMenuSetCommand(task, CovenantClient);
            AdditionalOptions[AdditionalOptions.IndexOf(
                this.AdditionalOptions.FirstOrDefault(MC => MC.Name == "Set")
            )] = setCommand;
            AdditionalOptions[AdditionalOptions.IndexOf(
                this.AdditionalOptions.FirstOrDefault(MC => MC.Name == "Unset")
            )] = new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values);

            this.SetupMenuAutoComplete();
		}

		public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            if (forwardEntrance)
            {
                if (parameters.Length != 1)
                {
                    EliteConsole.PrintFormattedErrorLine("Must specify a Task Name.");
                    EliteConsole.PrintFormattedErrorLine("Usage: Task <task_name>");
                    return false;
                }

                GruntTask gruntTask = CovenantClient.ApiGruntTasksByTasknameGet(parameters[0]);
                if (gruntTask == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Specified invalid Task Name: " + parameters[0]);
                    EliteConsole.PrintFormattedErrorLine("Usage: Task <task_name>");
                    return false;
                }
                this.task = gruntTask;
                this.MenuTitle = this.task.Name;

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
            this.MenuTitle = "Task";
        }

        private static MenuCommand GetTaskMenuSetCommand(GruntTask task, CovenantAPI CovenantClient)
        {
            List<MenuCommandParameterValue> DefaultOptions = task.Options.Select(O => new MenuCommandParameterValue { Value = O.Name }).ToList();
            switch (task.Name)
            {
                case "Assembly":
                    return new MenuCommandAssemblyTaskSet(CovenantClient)
                    {
                        Name = "Set",
                        Description = "Set AssemblyTask option",
                        Parameters = new List<MenuCommandParameter> {
                            new MenuCommandParameter {
                                Name = "Option",
                                Values = DefaultOptions.Append(
                                    new MenuCommandParameterValue {
                                        Value = "AssemblyPath",
                                        NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder)
                                    }
                                ).ToList()
                            },
                            new MenuCommandParameter { Name = "Value" }
                        }
                    };
                case "AssemblyReflect":
                    return new MenuCommandAssemblyReflectTaskSet(CovenantClient)
                    {
                        Name = "Set",
                        Description = "Set AssemblyReflectTask option",
                        Parameters = new List<MenuCommandParameter> {
                            new MenuCommandParameter {
                                Name = "Option",
                                Values = DefaultOptions.Append(
                                    new MenuCommandParameterValue {
                                        Value = "AssemblyPath",
                                        NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder)
                                    }
                                ).ToList()
                            },
                            new MenuCommandParameter { Name = "Value" }
                        }
                    };
                case "Upload":
					return new MenuCommandUploadTaskSet(CovenantClient)
					{
                        Name = "Set",
						Description = "Set Upload option",
						Parameters = new List<MenuCommandParameter> {
                            new MenuCommandParameter {
                                Name = "Option",
                                Values = DefaultOptions.Append(
                                    new MenuCommandParameterValue {
                                        Value = "FilePath",
                                        NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder)
                                    }
                                ).ToList()
                            },
							new MenuCommandParameter { Name = "Value" }
						}
					};
                case "ShellCode":
                    return new MenuCommandShellCodeTaskSet(CovenantClient)
                    {
                        Name = "Set",
                        Description = "Set ShellCode option",
                        Parameters = new List<MenuCommandParameter> {
                            new MenuCommandParameter {
                                Name = "Option",
                                Values = DefaultOptions.Append(
                                    new MenuCommandParameterValue {
                                        Value = "ShellcodeBinFilePath",
                                        NextValueSuggestions = Utilities.GetFilesForPath(Common.EliteDataFolder)
                                    }
                                ).ToList()
                            },
                            new MenuCommandParameter { Name = "Value" }
                        }
                    };
                default:
                    return new MenuCommandTaskSet(CovenantClient)
                    {
                        Name = "Set",
                        Description = "Set " + task.Name + " option",
                        Parameters = new List<MenuCommandParameter> {
                            new MenuCommandParameter { Name = "Option", Values = DefaultOptions },
                            new MenuCommandParameter { Name = "Value" }
                        }
                    };
            }
        }
    }
}
