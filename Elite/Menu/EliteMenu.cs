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
		public EventPrinter EventPrinter { get; set; } = new EventPrinter();
        private CovenantAPI CovenantClient { get; set; }
        
        public EliteMenu(CovenantAPI CovenantClient)
        {
            this.CovenantClient = CovenantClient;
			this.MenuStack.Add(new CovenantBaseMenuItem(this.CovenantClient, this.EventPrinter));
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
                    if (UserInput.ToLower() == "back")
                    {
                        if (this.MenuStack.Count > 1) {
                            currentMenuItem.LeavingMenuItem();
                            this.MenuStack.RemoveAt(this.MenuStack.Count - 1);
                            currentMenuItem = this.GetCurrentMenuItem();
                            currentMenuItem.ValidateMenuParameters(new string[]{}, false);
                        }
                        else { currentMenuItem.PrintInvalidOptionError(UserInput); }
                    }
                    else if (UserInput.ToLower() == "exit")
                    {
                        EliteConsole.PrintFormattedWarning("Exit Elite console? [y/N] ");
                        string input = EliteConsole.Read();
                        if (input.ToLower().StartsWith("y"))
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

    public class EventPrinter
	{
		private static object _EventLock = new object();

		public void PrintEvent(EventModel theEvent, string Context = "*")
		{
			lock (_EventLock)
			{
				if (this.WillPrintEvent(theEvent, Context))
				{
					switch (theEvent.Level)
					{
						case EventLevel.Highlight:
							EliteConsole.PrintFormattedHighlightLine(theEvent.MessageHeader);
							break;
						case EventLevel.Info:
                            EliteConsole.PrintFormattedInfoLine(theEvent.MessageHeader);
							break;
						case EventLevel.Warning:
							EliteConsole.PrintFormattedWarningLine(theEvent.MessageHeader);
							break;
						case EventLevel.Error:
							EliteConsole.PrintFormattedErrorLine(theEvent.MessageHeader);
							break;
						default:
							EliteConsole.PrintFormattedInfoLine(theEvent.MessageHeader);
							break;
					}
                    if (!string.IsNullOrWhiteSpace(theEvent.MessageBody))
                    {
                        EliteConsole.PrintInfoLine(theEvent.MessageBody);
                    }
				}
			}
		}

        public bool WillPrintEvent(EventModel theEvent, string Context = "*")
		{
            return this.ContextMatches(theEvent, Context);
		}

        public bool ContextMatches(EventModel theEvent, string Context = "*")
		{
			return theEvent.Context == "*" || Context.ToLower().Contains(theEvent.Context.ToLower());
		}
	}
}
