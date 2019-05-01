// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Rest;

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
            try
            {
                menuItem.Refresh();
                GruntTask task = ((TaskMenuItem)menuItem).Task;

                EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Parameter, "Task: " + task.Name)
                {
                    ShouldShortenFields = false
                };
                menu.Rows.Add(new List<string> { "Name:", task.Name });
                menu.Rows.Add(new List<string> { "Description:", task.Description });
                if (task.ReferenceAssemblies.Any())
                {
                    menu.Rows.Add(new List<string> { "ReferenceAssemblies:", String.Join(",", task.ReferenceAssemblies) });
                }
                if (task.ReferenceSourceLibraries.Any())
                {
                    menu.Rows.Add(new List<string> { "ReferenceSourceLibraries:", String.Join(",", task.ReferenceSourceLibraries) });
                }
                if (task.EmbeddedResources.Any())
                {
                    menu.Rows.Add(new List<string> { "EmbeddedResources:", String.Join(",", task.EmbeddedResources) });
                }
                // string usage = "Usage: " + task.Name;
                // foreach (GruntTaskOption o in task.Options)
                // {
                //     usage += " <" + o.Name.ToLower() + ">";
                // }
                // menu.Rows.Add(new List<string> { "Usage:", usage });
                if (task.Options.Any())
                {
                    foreach (GruntTaskOption o in task.Options)
                    {
                        menu.Rows.Add(new List<string> { "Parameter:" });
                        menu.Rows.Add(new List<string> { "  Name:", o.Name });
                        menu.Rows.Add(new List<string> { "  Description:", o.Description });
                        menu.Rows.Add(new List<string> { "  Value:", o.Value });
                    }
                }
                menu.Print();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
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
            try
            {
                List<string> commands = UserInput.Split(" ").ToList();
                if (commands.Count() < 3 || !commands[0].Equals(this.Name, StringComparison.OrdinalIgnoreCase))
                {
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTaskOption option = task.Options.FirstOrDefault(O => O.Name.ToLower() == commands[1].ToLower());
                if (option == null)
                {
                    EliteConsole.PrintFormattedErrorLine("Invalid Set option: \"" + commands[1] + "\"");
                    menuItem.PrintInvalidOptionError(UserInput);
                    return;
                }
                option.Value = String.Join(" ", commands.GetRange(2, commands.Count() - 2));
                this.CovenantClient.ApiGrunttasksPut(task);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandTaskStart : MenuCommand
    {
        public MenuCommandTaskStart(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Start";
            this.Description = "Start the Task";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            Grunt grunt = ((TaskMenuItem)menuItem).Grunt;
            try
            {
                GruntTask task = ((TaskMenuItem)menuItem).Task;
                GruntTasking gruntTasking = new GruntTasking
                {
                    TaskId = task.Id,
                    GruntId = grunt.Id,
                    Type = GruntTaskingType.Assembly,
                    Status = GruntTaskingStatus.Uninitialized,
                    TokenTask = task.TokenTask,
                    TaskingCommand = UserInput.ToLower() == "Start" ? (task.Name + " " + String.Join(' ', task.Options.Select(O => "/" + O.Name.ToLower() + " " + O.Value).ToList())) : UserInput
                };
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException)
            {
                EliteConsole.PrintFormattedErrorLine("Failed starting task on Grunt: " + grunt.Name);
            }
        }
    }

    public class TaskMenuItem : MenuItem
    {
        public GruntTask Task { get; set; }
        public Grunt Grunt { get; set; }

        public TaskMenuItem(CovenantAPI CovenantClient, Grunt Grunt) : base(CovenantClient)
        {
            try
            {
                this.Grunt = Grunt;
                this.MenuTitle = "Task";
                this.MenuDescription = "Task a Grunt to do something.";
                this.MenuItemParameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                        Name = "Task Name",
                        Values = this.CovenantClient.ApiGrunttasksGet().Select(T => new MenuCommandParameterValue { Value = T.Name }).ToList()
                    }
                };

                this.AdditionalOptions.Add(new MenuCommandTaskShow(CovenantClient));
                this.AdditionalOptions.Add(new MenuCommandTaskStart(CovenantClient));
                var setCommand = new MenuCommandTaskSet(CovenantClient);
                this.AdditionalOptions.Add(setCommand);
                this.AdditionalOptions.Add(new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values));
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void Refresh()
        {
            this.Task = this.CovenantClient.ApiGrunttasksByIdGet(this.Task.Id ?? default);
            this.Grunt = this.CovenantClient.ApiGruntsByIdGet(this.Grunt.Id ?? default);

            var setoptionparam = this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Set")
                                        .Parameters
                                        .FirstOrDefault(P => P.Name == "Option");
            setoptionparam.Values = this.Task.Options.Select(TO => new MenuCommandParameterValue { Value = TO.Name }).ToList();

            List<string> filePathTasks = new List<string> { "Assembly", "AssemblyReflect", "Upload", "ShellCode" };
            if (filePathTasks.Contains(this.Task.Name))
            {
                var filepaths = Utilities.GetFilesForPath(Common.EliteDataFolder);
                if (!setoptionparam.Values.Select(V => V.Value).Contains("LocalFilePath"))
                {
                    setoptionparam.Values.Add(new MenuCommandParameterValue
                    {
                        Value = "LocalFilePath",
                        NextValueSuggestions = filepaths
                    });
                }
                else
                {
                    setoptionparam.Values.FirstOrDefault(V => V.Value == "LocalFilePath").NextValueSuggestions = filepaths;
                }
            }
            this.AdditionalOptions[AdditionalOptions.IndexOf(
                    this.AdditionalOptions.FirstOrDefault(MC => MC.Name == "Unset")
                )] = new MenuCommandGenericUnset(setoptionparam.Values);

            this.SetupMenuAutoComplete();
        }

        public override bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true)
        {
            try
            {
                if (forwardEntrance)
                {
                    if (parameters.Length != 1)
                    {
                        EliteConsole.PrintFormattedErrorLine("Must specify a Task Name.");
                        EliteConsole.PrintFormattedErrorLine("Usage: Task <task_name>");
                        return false;
                    }
                    GruntTask gruntTask = this.CovenantClient.ApiGrunttasksByTasknameGet(parameters[0]);
                    if (gruntTask == null)
                    {
                        EliteConsole.PrintFormattedErrorLine("Specified invalid Task Name: " + parameters[0]);
                        EliteConsole.PrintFormattedErrorLine("Usage: Task <task_name>");
                        return false;
                    }
                    this.Task = gruntTask;
                    this.MenuTitle = this.Task.Name;
                }
                MenuCommand setCommand = GetTaskMenuSetCommand(this.Task.Name, CovenantClient);
                setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values = this.Task.Options
                    .Select(TO => new MenuCommandParameterValue { Value = TO.Name })
                    .ToList();
                this.AdditionalOptions[AdditionalOptions.IndexOf(
                    this.AdditionalOptions.FirstOrDefault(MC => MC.Name == "Set")
                )] = setCommand;
                AdditionalOptions[AdditionalOptions.IndexOf(
                    this.AdditionalOptions.FirstOrDefault(MC => MC.Name == "Unset")
                )] = new MenuCommandGenericUnset(setCommand.Parameters.FirstOrDefault(P => P.Name == "Option").Values);

                this.Refresh();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
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

        private static MenuCommand GetTaskMenuSetCommand(string TaskName, CovenantAPI CovenantClient)
        {
            switch (TaskName)
            {
                case "Assembly":
                    return new MenuCommandAssemblyTaskSet(CovenantClient);
                case "AssemblyReflect":
                    return new MenuCommandAssemblyReflectTaskSet(CovenantClient);
                case "Upload":
                    return new MenuCommandUploadTaskSet(CovenantClient);
                case "ShellCode":
                    return new MenuCommandShellCodeTaskSet(CovenantClient);
                default:
                    return new MenuCommandTaskSet(CovenantClient);
            }
        }
    }
}
