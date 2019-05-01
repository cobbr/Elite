// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Covenant.API;

namespace Elite.Menu
{
    public abstract class MenuCommand
    {
		protected readonly CovenantAPI CovenantClient;

		public MenuCommand(CovenantAPI CovenantClient = null)
        {
            this.CovenantClient = CovenantClient;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<MenuCommandParameter> Parameters { get; set; } = new List<MenuCommandParameter>();
        public abstract void Command(MenuItem menuItem, string UserInput);
    }

    public class MenuCommandParameter
    {
        public string Name { get; set; } = "";
        public List<MenuCommandParameterValue> Values { get; set; } = new List<MenuCommandParameterValue>();
    }

    public class MenuCommandParameterValue
    {
        public string Value { get; set; } = "";
        public List<string> NextValueSuggestions { get; set; } = new List<string>();
    }

    public class MenuCommandParameterValuesFromFilePath : List<MenuCommandParameterValue>
    {
        private string FilePath { get; set; } = "";
        public MenuCommandParameterValuesFromFilePath(string FilePath)
        {
            this.FilePath = FilePath;
            this.AddRange(Utilities.GetFilesForPath(FilePath).Select(F => new MenuCommandParameterValue { Value = F }).ToList());
        }
    }

    public class MenuCommandHelp : MenuCommand
    {
		public MenuCommandHelp()
        {
            this.Name = "Help";
            this.Description = "Display Help for this menu.";
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            EliteConsoleMenu menu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.Menu, "Help");
            menu.Columns.Add("Command");
            menu.Columns.Add("Options");
            menu.Columns.Add("Description");
            menuItem.MenuOptions.ForEach(M =>
            {
                menu.Rows.Add(new List<string>
                {
                    M.MenuTitle,
                    String.Join(" ", M.MenuItemParameters.Select(P => "<" + P.Name.ToLower().Replace(" ", "_") + ">").ToList()),
                    M.MenuDescription
                });
            });
            menuItem.AdditionalOptions.ForEach(O =>
            {
                menu.Rows.Add(new List<string>
                {
                    O.Name,
                    String.Join(" ", O.Parameters.Select(P => "<" + P.Name.ToLower().Replace(" ", "_") + ">")),
                    O.Description
                });
            });
            menu.Print();
        }
    }

    public class MenuCommandBack : MenuCommand
    {
		public MenuCommandBack() : base()
        {
            this.Name = "Back";
            this.Description = "Navigate Back one menu level.";
        }

        public override void Command(MenuItem menuItem, string UserInput) {
            menuItem.LeavingMenuItem();
        }
    }

    public class MenuCommandGenericUnset : MenuCommand
    {
        public MenuCommandGenericUnset(List<MenuCommandParameterValue> Values) : base()
        {
            this.Name = "Unset";
            this.Description = "Unset an option";
            this.Parameters = new List<MenuCommandParameter> {
                new MenuCommandParameter{ Name = "Option", Values = Values }
            };
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            string[] commands = UserInput.Split(" ");
            if (commands.Length != 2 || commands[0].ToLower() != "unset")
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
            else if (this.Parameters.FirstOrDefault(P => P.Name == "Option").Values.Select(V => V.Value.ToLower()).Contains(commands[1].ToLower()))
            {
                MenuCommand setCommand = menuItem.AdditionalOptions.FirstOrDefault(O => O.Name == "Set");
                setCommand.Command(menuItem, "Set " + commands[1] + " ");
            }
            else
            {
                menuItem.PrintInvalidOptionError(UserInput);
            }
        }
    }

    public class MenuCommandExit : MenuCommand
    {
		public MenuCommandExit() : base()
        {
            this.Name = "Exit";
            this.Description = "Exit the Elite console.";
        }

        public override void Command(MenuItem menuItem, string UserInput) { }
    }

    public abstract class MenuItem
    {
        public List<MenuItem> MenuOptions { get; }
        public List<MenuCommandParameter> MenuItemParameters { get; set; } = new List<MenuCommandParameter>();

        public List<MenuCommand> AdditionalOptions { get; set; }
        public AutoCompletionHandler TabCompletionHandler { get; set; }

        public string MenuTitle { get; set; }
        public string MenuDescription { get; set; }

		public readonly CovenantAPI CovenantClient;

		public MenuItem(CovenantAPI CovenantClient)
        {
            this.CovenantClient = CovenantClient;
            this.MenuOptions = new List<MenuItem>();
            this.AdditionalOptions = new List<MenuCommand> {
                new MenuCommandHelp(),
                new MenuCommandBack(),
                new MenuCommandExit()
            };
        }

        public MenuItem GetMenuOption(string UserInput)
        {
            string userInputMenuTitle = UserInput;
            if (userInputMenuTitle.Contains(" "))
            {
                userInputMenuTitle = UserInput.Split(" ")[0];
            }
            MenuItem item = MenuOptions.FirstOrDefault(M => M.MenuTitle.ToLower() == userInputMenuTitle.ToLower());
            if (item != null)
            {
                // Get any parameters given to this MenuOption
                string[] parameters = UserInput.ToLower().Split(" ").Where(S => S != item.MenuTitle.ToLower()).ToArray();
                // Validate parameters before switching menu levels
                if (item.ValidateMenuParameters(parameters))
                {
                    return item;
                }
            }
            return null;
        }

        public MenuCommand GetMenuCommandOption(string MenuCommandName)
        {
            if (MenuCommandName.Contains(" ")) { MenuCommandName = MenuCommandName.Substring(0, MenuCommandName.IndexOf(" ")); }
            return AdditionalOptions.FirstOrDefault(O => MenuCommandName.ToLower() == O.Name.ToLower());
        }

        public virtual bool ValidateMenuParameters(string[] parameters, bool forwardEntrance = true) { this.Refresh(); return true; }
        public virtual void PrintMenu() {}
        public virtual void LeavingMenuItem() {}
        public virtual void Refresh() {}

        public void PrintInvalidOptionError(string UserInput)
        {
            EliteConsole.PrintFormattedErrorLine("Invalid option \"" + UserInput + "\" selected. Try \"help\" to see a list of valid options.");
        }

        protected void SetupMenuAutoComplete()
        {
            List<Tuple<string, string[]>> suggestions = new List<Tuple<string, string[]>> {
                new Tuple<string, string[]> ("", this.MenuOptions.Select(M => M.MenuTitle).Concat(this.AdditionalOptions.Select(O => O.Name)).ToArray())
            };

            this.MenuOptions.ForEach(O => {
                string StartString = O.MenuTitle + " ";

                for (int i = 0; i < O.MenuItemParameters.Count; i++)
                {
                    suggestions.Add(
                        new Tuple<string, string[]>(StartString, O.MenuItemParameters[i].Values.Select(V => V.Value).ToArray())
                    );
                    if (i != O.MenuItemParameters.Count - 1)
                    {
                        foreach (MenuCommandParameterValue value in O.MenuItemParameters[i].Values)
                        {
                            string ParameterStartString = StartString + value.Value + " ";
                            suggestions.Add(
                                new Tuple<string, string[]>(ParameterStartString, value.NextValueSuggestions.ToArray())
                            );
                        }
                    }
                }
            });

            this.AdditionalOptions.ForEach(O => {
                string StartString = O.Name + " ";

                for (int i = 0; i < O.Parameters.Count; i++)
                {
                    suggestions.Add(
                        new Tuple<string, string[]>(StartString, O.Parameters[i].Values.Select(V => V.Value).ToArray())
                    );
                    if (i != O.Parameters.Count - 1)
                    {
                        foreach (MenuCommandParameterValue value in O.Parameters[i].Values)
                        {
                            string ParameterStartString = StartString + value.Value + " ";
                            suggestions.Add(
                                new Tuple<string, string[]>(ParameterStartString, value.NextValueSuggestions.ToArray())
                            );
                        }
                    }
                }
            });
            this.TabCompletionHandler = new AutoCompletionHandler(suggestions);
        }
    }

    public class AutoCompletionHandler : IAutoCompleteHandler
    {
        public char[] Separators { get; set; } = new char[] { ' ' };
        private List<Tuple<string, string[]>> suggestions;

        public AutoCompletionHandler(List<Tuple<string, string[]>> suggestions)
        {
            this.suggestions = suggestions;
        }

        public string[] GetSuggestions(string text, int index)
        {
            if (text.Contains(" "))
            {
                string lastWord = text.Split(' ').Last();
                // Get suggestions
                return suggestions.Where(
                    // Where text starts with the correct matching StartString
                    S => text.ToLower().StartsWith(S.Item1.ToLower())
                    // Order by descending length of matching StartStrings (to get most relavent/specific StartString), and get First one
                ).OrderBy(S => -S.Item1.Length).Select(S => S.Item2).FirstOrDefault().Where(
                    // Get suggestions for StartString that match the current word being typed 
                    S => S.ToLower().StartsWith(lastWord.ToLower())
                    // Order by descending length (to bubble most relevant/specific suggestions to the top)
                ).Select(S => {
                    if (S.Contains(Path.DirectorySeparatorChar))
                    {
                        List<string> LastWordParts = lastWord.Split(Path.DirectorySeparatorChar).ToList();
                        string subs = S.Substring(lastWord.Length);
                        int subsendindex = subs.Contains(Path.DirectorySeparatorChar) ? subs.IndexOf(Path.DirectorySeparatorChar) : (subs.Length - 1);
                        return S.Substring(0, subsendindex + lastWord.Length + 1);
                    }
                    else { return S; }
                }).OrderBy(S => -S.Length).ToArray();
            }
            else
            {
                return suggestions.Where(
                    S => S.Item1 == ""
                ).OrderBy(S => -S.Item1.Length).Select(S => S.Item2).FirstOrDefault().Where(
                    S => S.ToLower().StartsWith(text.ToLower())
                ).ToArray();
            }
        }
    }
}
