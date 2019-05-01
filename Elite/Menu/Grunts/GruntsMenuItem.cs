// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Grunts
{
    public class MenuCommandGruntsShow : MenuCommand
    {
        public MenuCommandGruntsShow() : base()
        {
            this.Name = "Show";
            this.Description = "Displays list of connected grunts.";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            List<Grunt> displayGrunts = gruntsMenuItem.Grunts.Where(G => G.Status != GruntStatus.Uninitialized && !gruntsMenuItem.HiddenGruntNames.Contains(G.Name)).ToList();
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Grunts");
            menu.Columns.Add("Name");
            menu.Columns.Add("CommType");
            menu.Columns.Add("ComputerName");
            menu.Columns.Add("User");
            menu.Columns.Add("Status");
            menu.Columns.Add("Last Check In");
            menu.Columns.Add("Integrity");
            menu.Columns.Add("OperatingSystem");
            menu.Columns.Add("Process");
            displayGrunts.ForEach(G =>
            {
                menu.Rows.Add(new List<string> { G.Name, G.CommType.ToString(), G.Hostname, G.UserDomainName + "\\" + G.UserName, G.Status.ToString(), G.LastCheckIn.ToString(), G.Integrity.ToString(), G.OperatingSystem, G.Process });
            });
            menu.Print();
        }
    }

    public class MenuCommandGruntsRename : MenuCommand
    {
        public MenuCommandGruntsRename(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Rename";
            this.Description = "Rename a Grunt";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Old Name" },
                new MenuCommandParameter { Name = "New Name" }
            };
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenu = ((GruntsMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || !commands[0].Equals("rename", StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            Grunt grunt = gruntsMenu.Grunts.FirstOrDefault(G => G.Name.Equals(commands[1], StringComparison.OrdinalIgnoreCase));
            if (grunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Grunt with name: " + commands[1] + " does not exist.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (gruntsMenu.Grunts.Any(G => G.Name.Equals(commands[2], StringComparison.OrdinalIgnoreCase)))
            {
                EliteConsole.PrintFormattedErrorLine("Grunt with name: " + commands[2] + " already exists.");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            grunt.Name = commands[2];
            try
            {
                await this.CovenantClient.ApiGruntsPutAsync(grunt);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntsDelay : MenuCommand
    {
        public MenuCommandGruntsDelay(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Delay";
            this.Description = "Delay an active Grunt.";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "Grunt Name" },
                    new MenuCommandParameter { Name = "Seconds" }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || !commands[0].Equals("delay", StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (!int.TryParse(commands[2], out int n))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedWarning("Delay all Grunts? [y/N] ");
                string input1 = EliteConsole.Read();
                if (!input1.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                foreach (Grunt g in gruntsMenuItem.Grunts)
                {
                    GruntTasking gt = new GruntTasking
                    {
                        Id = 0,
                        GruntId = g.Id,
                        TaskId = 1,
                        Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.SetDelay,
                        TaskingMessage = n.ToString(),
                        TaskingCommand = UserInput,
                        TokenTask = false
                    };
                    try
                    {
                        await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(g.Id ?? default, gt);
                    }
                    catch (HttpOperationException e)
                    {
                        EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                    }
                }
                return;
            }
            Grunt grunt = gruntsMenuItem.Grunts.FirstOrDefault(G => G.Name == commands[1]);
            if (grunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid GruntName: \"" + commands[1] + "\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            GruntTasking gruntTasking = new GruntTasking
            {
                Id = 0,
                GruntId = grunt.Id,
                TaskId = 1,
                Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                Status = GruntTaskingStatus.Uninitialized,
                Type = GruntTaskingType.SetDelay,
                TaskingMessage = n.ToString(),
                TaskingCommand = UserInput,
                TokenTask = false
            };
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntsKill : MenuCommand
    {
        public MenuCommandGruntsKill(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Kill";
            this.Description = "Kill an active Grunt.";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "Grunt Name" }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override async void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || !commands[0].Equals("kill", StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                EliteConsole.PrintFormattedWarning("Kill all Grunts? [y/N] ");
                string input1 = EliteConsole.Read();
                if (!input1.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                foreach (Grunt g in gruntsMenuItem.Grunts)
                {
                    GruntTasking gt = new GruntTasking
                    {
                        Id = 0,
                        GruntId = g.Id,
                        TaskId = 1,
                        Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                        Status = GruntTaskingStatus.Uninitialized,
                        Type = GruntTaskingType.Kill,
                        TaskingCommand = UserInput,
                        TokenTask = false
                    };
                    try
                    {
                        await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(g.Id ?? default, gt);
                    }
                    catch (HttpOperationException e)
                    {
                        EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
                    }
                }
                return;
            }
            Grunt grunt = gruntsMenuItem.Grunts.FirstOrDefault(G => G.Name == commands[1]);
            if (grunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid GruntName: \"" + commands[1] + "\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            EliteConsole.PrintFormattedWarning("Kill Grunt: " + commands[1] + "? [y/N] ");
            string input2 = EliteConsole.Read();
            if (!input2.StartsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            GruntTasking gruntTasking = new GruntTasking
            {
                Id = 0,
                GruntId = grunt.Id,
                TaskId = 1,
                Name = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10),
                Status = GruntTaskingStatus.Uninitialized,
                Type = GruntTaskingType.Kill,
                TaskingCommand = UserInput,
                TokenTask = false
            };
            try
            {
                await this.CovenantClient.ApiGruntsByIdTaskingsPostAsync(grunt.Id ?? default, gruntTasking);
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }
    }

    public class MenuCommandGruntsHide : MenuCommand
    {
        public MenuCommandGruntsHide() : base()
        {
            this.Name = "Hide";
            this.Description = "Hide an inactive Grunt.";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "Grunt Name" }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || !commands[0].Equals("hide", StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                gruntsMenuItem.HiddenGruntNames.AddRange(gruntsMenuItem.Grunts.Select(G => G.Name));
                return;
            }
            Grunt grunt = gruntsMenuItem.Grunts.FirstOrDefault(G => G.Name == commands[1]);
            if (grunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid GruntName: \"" + commands[1] + "\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            EliteConsole.PrintFormattedWarning("Hide Grunt: " + commands[1] + "? [y/N] ");
            string input = EliteConsole.Read();
            if (!input.StartsWith("y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            gruntsMenuItem.HiddenGruntNames.Add(grunt.Name);
        }
    }

    public class MenuCommandGruntsUnhide : MenuCommand
    {
        public MenuCommandGruntsUnhide() : base()
        {
            this.Name = "Unhide";
            this.Description = "Unhide a hidden Grunt.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter { Name = "Hidden Grunt Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || !commands[0].Equals("unhide", StringComparison.OrdinalIgnoreCase))
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                gruntsMenuItem.HiddenGruntNames.Clear();
                return;
            }
            string gruntName = gruntsMenuItem.HiddenGruntNames.FirstOrDefault(HGN => HGN == commands[1]);
            if (string.IsNullOrEmpty(gruntName))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid GruntName: \"" + commands[1] + "\"");
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            gruntsMenuItem.HiddenGruntNames.Remove(gruntName);
        }
    }

    public class GruntsMenuItem : MenuItem
    {
        public List<Grunt> Grunts { get; set; }
        public List<string> HiddenGruntNames { get; set; } = new List<string>();

		public GruntsMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Grunts";
            this.MenuDescription = "Displays list of connected grunts.";
			this.MenuOptions.Add(new GruntInteractMenuItem(this.CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsShow());
            this.AdditionalOptions.Add(new MenuCommandGruntsRename(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsDelay(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsKill(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsHide());
            this.AdditionalOptions.Add(new MenuCommandGruntsUnhide());
        }

		public override void Refresh()
		{
            try
            {
                this.Grunts = this.CovenantClient.ApiGruntsGet().ToList();
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
            List<MenuCommandParameterValue> gruntNames = Grunts.Where(G => G.Status != GruntStatus.Uninitialized)
                                                               .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();
            List<MenuCommandParameterValue> killableGruntNames = Grunts.Where(G => G.Status != GruntStatus.Uninitialized && G.Status != GruntStatus.Killed)
                                                                       .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();

            this.MenuOptions.FirstOrDefault(M => M.MenuTitle == "Interact")
                            .MenuItemParameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = gruntNames;

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Rename")
                .Parameters.FirstOrDefault(P => P.Name == "Old Name").Values = gruntNames;

            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Delay")
                .Parameters.FirstOrDefault(P => P.Name == "Grunt Name").Values =
                    gruntNames.AsEnumerable()
                        .Append(new MenuCommandParameterValue { Value = "all" })
                        .ToList();

            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Kill")
                .Parameters.FirstOrDefault(P => P.Name == "Grunt Name").Values =
                    killableGruntNames.AsEnumerable()
                        .Append(new MenuCommandParameterValue { Value = "all" })
                        .ToList();

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Hide")
                .Parameters.FirstOrDefault(P => P.Name == "Grunt Name").Values =
                    gruntNames.Where(GN => !this.HiddenGruntNames.Contains(GN.Value))
                        .Append(new MenuCommandParameterValue { Value = "all" })
                        .ToList();

            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Unhide")
                .Parameters.FirstOrDefault(P => P.Name == "Hidden Grunt Name").Values =
                    this.HiddenGruntNames
                        .Select(G => new MenuCommandParameterValue { Value = G })
                        .Append(new MenuCommandParameterValue { Value = "all" })
                        .ToList();

            this.SetupMenuAutoComplete();
		}

		public override bool ValidateMenuParameters(string[] parameters = null, bool forwardEntrance = true)
        {
            this.Refresh();
            return true;
        }

        public override void PrintMenu()
        {
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Show").Command(this, "");
        }
    }
}
