// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Rest;

using Covenant.API;
using Covenant.API.Models;

namespace Elite.Menu.Indicators
{
    public class MenuCommandCredentialsShow : MenuCommand
    {
        public MenuCommandCredentialsShow()
        {
            this.Name = "Show";
            this.Description = "Show Credentials";
            this.Parameters = new List<MenuCommandParameter>();
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            menuItem.Refresh();
            CredentialsMenuItem credentialsMenu = ((CredentialsMenuItem)menuItem);
            EliteConsoleMenu passwordCredentialsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Password Credentials");
            passwordCredentialsMenu.Columns.Add("Domain");
            passwordCredentialsMenu.Columns.Add("Username");
            passwordCredentialsMenu.Columns.Add("Password");
            credentialsMenu.PasswordCredentials.ToList().ForEach(PC =>
            {
                passwordCredentialsMenu.Rows.Add(new List<string> {
                    PC.Domain,
                    PC.Username,
                    PC.Password
                });
            });
            if (passwordCredentialsMenu.Rows.Count > 0)
            {
                passwordCredentialsMenu.PrintEndBuffer = false;
                passwordCredentialsMenu.Print();
            }

            EliteConsoleMenu hashCredentialsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Hashed Credentials");
            hashCredentialsMenu.Columns.Add("Domain");
            hashCredentialsMenu.Columns.Add("Username");
            hashCredentialsMenu.Columns.Add("Hash");
            hashCredentialsMenu.Columns.Add("HashType");
            credentialsMenu.HashCredentials.ToList().ForEach(HC =>
            {
                hashCredentialsMenu.Rows.Add(new List<string> {
                    HC.Domain,
                    HC.Username,
                    HC.Hash,
                    HC.HashCredentialType.ToString()
                });
            });
            if (hashCredentialsMenu.Rows.Count > 0)
            {
                hashCredentialsMenu.PrintEndBuffer = false;
                hashCredentialsMenu.Print();
            }

            EliteConsoleMenu ticketCredentialsMenu = new EliteConsoleMenu(EliteConsoleMenu.EliteConsoleMenuType.List, "Ticket Credentials");
            ticketCredentialsMenu.Columns.Add("ID");
            ticketCredentialsMenu.Columns.Add("Domain");
            ticketCredentialsMenu.Columns.Add("Username");
            ticketCredentialsMenu.Columns.Add("ServiceName");
            ticketCredentialsMenu.Columns.Add("Ticket");
            ticketCredentialsMenu.Columns.Add("TicketType");
            credentialsMenu.TicketCredentials.ToList().ForEach(TC =>
            {
                ticketCredentialsMenu.Rows.Add(new List<string> {
                    TC.Id.ToString(),
                    TC.Domain,
                    TC.Username,
                    TC.ServiceName,
                    TC.Ticket,
                    TC.TicketCredentialType.ToString()
                });
            });
            if (ticketCredentialsMenu.Rows.Count > 0)
            {
                ticketCredentialsMenu.Print();
            }
        }
    }

    public class MenuCommandCredentialsTicket : MenuCommand
    {
        public MenuCommandCredentialsTicket() : base()
        {
            this.Name = "Ticket";
            this.Description = "Display full Base64EncodedTicket";
            try
            {
                this.Parameters = new List<MenuCommandParameter> {
                    new MenuCommandParameter { Name = "ID" }
                };
            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
        }

        public override void Command(MenuItem menuItem, string UserInput)
        {
            CredentialsMenuItem credentialsMenuItem = (CredentialsMenuItem)menuItem;
            List<string> commands = Utilities.ParseParameters(UserInput);
            if (commands.Count() != 2)
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Ticket command. Usage is: Ticket <ticket_id>");
                EliteConsole.PrintFormattedErrorLine("Valid Ticket IDs are: " + String.Join(", ", credentialsMenuItem.TicketCredentials.Select(T => T.Id.ToString())));
            }
            else if (!credentialsMenuItem.TicketCredentials.Select(T => T.Id.ToString()).Contains(commands[1]))
            {
                EliteConsole.PrintFormattedErrorLine("Invalid Ticket command. Usage is: Ticket <ticket_id>");
                EliteConsole.PrintFormattedErrorLine("Valid Ticket IDs are: " + String.Join(", ", credentialsMenuItem.TicketCredentials.Select(T => T.Id.ToString())));
            }
            else
            {
                EliteConsole.PrintFormattedInfoLine($"Ticket ID: {commands[1]} Base64EncodedTicket:");
                EliteConsole.PrintInfoLine(credentialsMenuItem.TicketCredentials.FirstOrDefault(T => T.Id.ToString().Equals(commands[1], StringComparison.OrdinalIgnoreCase)).Ticket);
            }
        }
    }

    public sealed class CredentialsMenuItem : MenuItem
    {
        public List<CapturedCredential> AllCredentials { get; set; }
        public List<CapturedPasswordCredential> PasswordCredentials { get; set; }
        public List<CapturedHashCredential> HashCredentials { get; set; }
        public List<CapturedTicketCredential> TicketCredentials { get; set; }

        public CredentialsMenuItem(CovenantAPI CovenantClient) : base(CovenantClient)
        {
            this.MenuTitle = "Credentials";
            this.MenuDescription = "Displays list of credentials.";

            this.AdditionalOptions.Add(new MenuCommandCredentialsShow());
            this.AdditionalOptions.Add(new MenuCommandCredentialsTicket());
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

        public override void Refresh()
        {
            try
            {
                this.AllCredentials = this.CovenantClient.ApiCredentialsGet().ToList();
                this.PasswordCredentials = this.CovenantClient.ApiCredentialsPasswordsGet().ToList();
                this.HashCredentials = this.CovenantClient.ApiCredentialsHashesGet().ToList();
                this.TicketCredentials = this.CovenantClient.ApiCredentialsTicketsGet().ToList();

                this.AdditionalOptions.FirstOrDefault(O => O.Name == "Ticket").Parameters
                    .FirstOrDefault(P => P.Name == "ID").Values = this.TicketCredentials
                    .Select(T => new MenuCommandParameterValue { Value = T.Id.ToString() })
                    .ToList();

            }
            catch (HttpOperationException e)
            {
                EliteConsole.PrintFormattedWarningLine("CovenantException: " + e.Response.Content);
            }
            this.SetupMenuAutoComplete();
        }
    }
}
