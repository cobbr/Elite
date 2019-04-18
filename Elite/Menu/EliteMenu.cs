// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu
{
    public class EliteMenu
    {
        private List<MenuItem> MenuStack { get; set; } = new List<MenuItem>();
        private CovenantAPI CovenantClient { get; set; }
        
        public EliteMenu(CovenantAPI CovenantClient)
        {
            this.CovenantClient = CovenantClient;
			this.MenuStack.Add(new CovenantBaseMenuItem(this.CovenantClient));
        }

        public string GetMenuLevelTitleStack()
		{
			StringBuilder builder = new StringBuilder();
			if (this.MenuStack.Count > 1)
			{
				builder.Append(MenuStack[1].MenuTitle);
				for (int i = 2; i < MenuStack.Count; i++)
				{
					builder.Append("\\" + MenuStack[i].MenuTitle);
				}
			}
			return builder.ToString();
		}

        public void PrintMenuLevel()
        {
            if (MenuStack.Count > 1)
            {
                EliteConsole.PrintInfo("(Covenant: ");
				EliteConsole.PrintHighlight(this.GetMenuLevelTitleStack());
                EliteConsole.PrintInfo(") > ");
            }
            else
            {
                EliteConsole.PrintInfo("(Covenant) > ");
            }
        }

        public MenuItem GetCurrentMenuItem()
        {
            return this.MenuStack[this.MenuStack.Count - 1];
        }

        public bool PrintMenu(string UserInput = "")
        {
            try
            {
                UserInput = UserInput.Trim();
                if (UserInput != "")
                {
                    MenuItem currentMenuItem = this.GetCurrentMenuItem();
                    if (UserInput.Equals("back", StringComparison.OrdinalIgnoreCase))
                    {
                        if (this.MenuStack.Count > 1) {
                            currentMenuItem.LeavingMenuItem();
                            this.MenuStack.RemoveAt(this.MenuStack.Count - 1);
                            currentMenuItem = this.GetCurrentMenuItem();
                            currentMenuItem.ValidateMenuParameters(new string[]{}, false);
                        }
                        else { currentMenuItem.PrintInvalidOptionError(UserInput); }
                    }
                    else if (UserInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        EliteConsole.PrintFormattedWarning("Exit Elite console? [y/N] ");
                        string input = EliteConsole.Read();
                        if (input.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        MenuItem newMenuLevelItem = currentMenuItem.GetMenuOption(UserInput);
                        if (newMenuLevelItem != null)
                        {
                            this.MenuStack.Add(newMenuLevelItem);
                            newMenuLevelItem.PrintMenu();
                        }
                        else
                        {
                            MenuCommand menuCommandOption = currentMenuItem.GetMenuCommandOption(UserInput);
                            if (menuCommandOption != null)
                            {
                                menuCommandOption.Command(currentMenuItem, UserInput);
                            }
                            else
                            {
                                currentMenuItem.PrintInvalidOptionError(UserInput);
                            }
                        }
                    }
                    currentMenuItem = this.GetCurrentMenuItem();
                    ReadLine.AutoCompletionHandler = currentMenuItem.TabCompletionHandler;
                }
                this.PrintMenuLevel();

                return true;
            }
            catch (HttpRequestException)
            {
                EliteConsole.PrintFormattedWarning("Covenant has disconnected. Quit? [y/N] ");
                string input = EliteConsole.Read();
                if (input.ToLower().StartsWith('y'))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                EliteConsole.PrintFormattedErrorLine("EliteMenu Exception: " + e.Message);
                EliteConsole.PrintErrorLine(e.StackTrace);
                this.PrintMenuLevel();
                return true;
            }
            this.PrintMenuLevel();
            return true;
        }
    }
}
