// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System.Linq;
using System.Collections.Generic;

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
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            gruntsMenuItem.ValidateMenuParameters();
            List<Grunt> displayGrunts = gruntsMenuItem.Grunts.Where(G => G.Status != GruntStatus.Uninitialized && !gruntsMenuItem.HiddenGruntNames.Contains(G.Name)).ToList();
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Grunts");
            menu.Columns.Add("Name");
            menu.Columns.Add("User");
            menu.Columns.Add("Integrity");
            menu.Columns.Add("ComputerName");
            menu.Columns.Add("OperatingSystem");
            menu.Columns.Add("Process");
            menu.Columns.Add("Status");
            menu.Columns.Add("Last Check In");
            displayGrunts.ForEach(G =>
            {
                menu.Rows.Add(new List<string> { G.Name, G.UserDomainName + "\\" + G.UserName, G.Integrity.ToString(), G.IpAddress, G.OperatingSystem, G.Process, G.Status.ToString(), G.LastCheckIn });
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
                new MenuCommandParameter{ Name = "Old Name" },
                new MenuCommandParameter{ Name = "New Name" }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            GruntsMenuItem gruntsMenu = ((GruntsMenuItem)menuItem);
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 3 || commands[0].ToLower() != "rename")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }

            Grunt grunt = gruntsMenu.Grunts.FirstOrDefault(G => G.Name.ToLower() == commands[1]);
            if (grunt == null)
            {
                EliteConsole.PrintFormattedErrorLine("Grunt with name: " + commands[1] + " does not exist.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (gruntsMenu.Grunts.Where(G => G.Name.ToLower() == commands[2].ToLower()).Any())
            {
                EliteConsole.PrintFormattedErrorLine("Grunt with name: " + commands[2] + " already exists.");
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else
            {
                grunt.Name = commands[2];
                this.CovenantClient.ApiGruntsPut(grunt);
            }
        }
    }

    public class MenuCommandGruntsKill : MenuCommand
    {
        public MenuCommandGruntsKill(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Kill";
            this.Description = "Kill an active Grunt.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Grunt Name",
                    Values = CovenantClient.ApiGruntsGet().Where(G => G.Status != GruntStatus.Uninitialized)
                                           .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList()
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "kill")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].ToLower() == "all")
            {
                EliteConsole.PrintFormattedWarning("Kill all Grunts? [y/N] ");
                string input1 = EliteConsole.Read();
                if (!input1.ToLower().StartsWith("y"))
                {
                    return;
                }
                gruntsMenuItem.HiddenGruntNames.AddRange(gruntsMenuItem.Grunts.Select(G => G.Name));
                foreach (Grunt g in gruntsMenuItem.Grunts)
                {
                    GruntTasking gt = new GruntTasking { Type = GruntTaskingType.Kill, GruntId = g.Id };
                    this.CovenantClient.ApiGruntsByIdTaskingsPost(g.Id ?? default, gt);
                }
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
            if (!input2.ToLower().StartsWith("y"))
            {
                return;
            }
            GruntTasking gruntTasking = new GruntTasking { Type = GruntTaskingType.Kill, GruntId = grunt.Id };
            this.CovenantClient.ApiGruntsByIdTaskingsPost(grunt.Id ?? default, gruntTasking);
        }
    }

    public class MenuCommandGruntsHide : MenuCommand
    {
        public MenuCommandGruntsHide(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.Name = "Hide";
            this.Description = "Hide an inactive Grunt.";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter {
                    Name = "Grunt Name",
                    Values = CovenantClient.ApiGruntsGet().Where(G => G.Status != GruntStatus.Uninitialized)
                                           .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList()
                }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            GruntsMenuItem gruntsMenuItem = (GruntsMenuItem)menuItem;
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "hide")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].ToLower() == "all")
            {
                gruntsMenuItem.HiddenGruntNames.AddRange(gruntsMenuItem.Grunts.Select(G => G.Name));
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
            if (!input.ToLower().StartsWith("y"))
            {
                return;
            }
            gruntsMenuItem.HiddenGruntNames.Add(grunt.Name);
        }
    }

    public class MenuCommandGruntsUnhide : MenuCommand
    {
        public MenuCommandGruntsUnhide(CovenantAPI CovenantClient) : base(CovenantClient)
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
            if (commands.Length != 2 || commands[0].ToLower() != "unhide")
            {
                menuItem.PrintInvalidOptionError(UserInput);
                return;
            }
            if (commands[1].ToLower() == "all")
            {
                gruntsMenuItem.HiddenGruntNames.Clear();
            }
            string gruntName = gruntsMenuItem.HiddenGruntNames.FirstOrDefault(HGN => HGN == commands[1]);
            if (gruntName == null || gruntName == "")
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

		public GruntsMenuItem(CovenantAPI CovenantClient, EventPrinter EventPrinter) : base(CovenantClient, EventPrinter)
        {
            this.MenuTitle = "Grunts";
            this.MenuDescription = "Displays list of connected grunts.";
			this.MenuOptions.Add(new GruntInteractMenuItem(this.CovenantClient, this.EventPrinter));
            this.AdditionalOptions.Add(new MenuCommandGruntsShow());
            this.AdditionalOptions.Add(new MenuCommandGruntsRename(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsKill(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsHide(CovenantClient));
            this.AdditionalOptions.Add(new MenuCommandGruntsUnhide(CovenantClient));

            this.SetupMenuAutoComplete();
        }

		public override void Refresh()
		{
            this.Grunts = CovenantClient.ApiGruntsGet().ToList();
            List<MenuCommandParameterValue> gruntNames = Grunts.Where(G => G.Status != GruntStatus.Uninitialized)
                                                               .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();
            List<MenuCommandParameterValue> killableGruntNames = Grunts.Where(G => G.Status != GruntStatus.Uninitialized && G.Status != GruntStatus.Killed)
                                                                       .Select(G => new MenuCommandParameterValue { Value = G.Name }).ToList();

            this.MenuOptions.FirstOrDefault(M => M.MenuTitle == "Interact")
                            .MenuItemParameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = gruntNames;
            this.AdditionalOptions.FirstOrDefault(O => O.Name == "Kill")
                                  .Parameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = killableGruntNames;
            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Rename")
                                  .Parameters.FirstOrDefault(P => P.Name == "Old Name").Values = gruntNames;
            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Hide")
                                  .Parameters.FirstOrDefault(P => P.Name == "Grunt Name").Values = gruntNames.Where(GN => !this.HiddenGruntNames.Contains(GN.Value)).ToList();
            this.AdditionalOptions.FirstOrDefault(AO => AO.Name == "Unhide")
                                  .Parameters.FirstOrDefault(P => P.Name == "Hidden Grunt Name").Values = this.HiddenGruntNames
                                  .Select(G => new MenuCommandParameterValue { Value = G}).ToList();
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
