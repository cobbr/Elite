// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Rest;
using McMaster.Extensions.CommandLineUtils;

using Covenant.API;
using Covenant.API.Models;
using Elite.Menu;

namespace Elite
{
    class Elite
    {
        static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication();
            app.HelpOption("-? | -h | --help");
            var UserNameOption = app.Option(
                "-u | --username <USERNAME>",
                "The UserName to login to the Covenant API.",
                CommandOptionType.SingleValue
            );
            var PasswordOption = app.Option(
                "-p | --password <PASSWORD>",
                "The Password to login to the Covenant API.",
                CommandOptionType.SingleValue
            );
            var HashOption = app.Option(
                "-h | --hash <HASH>",
                "The Covenant API certificate hash to trust.",
                CommandOptionType.SingleValue
            );
            var ComputerNameOption = app.Option(
                "-c | --computername <COMPUTERNAME>",
                "The ComputerName (IPAddress or Hostname) to bind the Covenant API to.",
                CommandOptionType.SingleValue
            );

            app.OnExecute(() =>
            {
				string username = UserNameOption.Value();
				string password = PasswordOption.Value();
                string hash = HashOption.Value();
				if (!UserNameOption.HasValue())
				{
                    EliteConsole.PrintHighlight("Username: ");
					username = EliteConsole.Read();
				}
                if (!PasswordOption.HasValue())
                {
                    EliteConsole.PrintHighlight("Password: ");
					password = GetPassword();
                    Console.WriteLine();
                }
                if (!HashOption.HasValue())
                {
                    EliteConsole.PrintHighlight("Covenant CertHash (Empty to trust all): ");
                    hash = EliteConsole.Read();
                }
                var CovenantComputerName = ComputerNameOption.HasValue() ? ComputerNameOption.Value() : "localhost";
                Elite elite = null;
                try
                {
                    elite = new Elite(new Uri("https://" + CovenantComputerName + ":7443"), username, password, hash);
                }
                catch (HttpRequestException e)
                {
                    if (e.InnerException.GetType().Name == "SocketException")
                    {
                        EliteConsole.PrintFormattedErrorLine("Could not connect to Covenant at: " + CovenantComputerName);
                        if (CovenantComputerName.ToLower() == "localhost" || CovenantComputerName == "127.0.0.1")
                        {
                            EliteConsole.PrintFormattedErrorLine("Are you using Docker? Elite cannot connect over the loopback address while using Docker, because Covenant is not running within the Elite docker container.");
                        }
                        return -1;
                    }
                    else if (e.InnerException.GetType().Name == "AuthenticationException")
                    {
                        EliteConsole.PrintFormattedErrorLine("Covenant certificate does not match: " + hash);
                    }
                    return -2;
                }
                catch (HttpOperationException)
                {
                    EliteConsole.PrintFormattedErrorLine("Incorrect password for user: " + username);
                    return -3;
                }
                elite.Launch();
                return 0;
            });
            app.Execute(args);
        }

        private EliteMenu EliteMenu { get; set; }

        private Elite(Uri CovenantURI, string CovenantUsername, string CovenantPassword, string CovenantHash)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
            {
                // Cert Pinning - Trusts only the Covenant API certificate
                if (CovenantHash == "" || cert.GetCertHashString() == CovenantHash) { return true; }
                else { return false; }
            };

            CovenantAPI LoginCovenantClient = new CovenantAPI(CovenantURI, new BasicAuthenticationCredentials { UserName = "", Password = "" }, clientHandler);
			CovenantUserLoginResult result = LoginCovenantClient.ApiUsersLoginPost(new CovenantUserLogin { UserName = CovenantUsername, Password = CovenantPassword });

            if (result.Success ?? default)
            {
                TokenCredentials creds = new TokenCredentials(result.Token);
                CovenantAPI CovenantClient = new CovenantAPI(CovenantURI, creds, clientHandler);
                this.EliteMenu = new EliteMenu(CovenantClient);
                EventPoller poller = new EventPoller();
                poller.EventOccurred += this.EliteMenu.onEventOccured;
                Task.Run(() => poller.Poll(CovenantClient));

                ReadLine.AutoCompletionHandler = this.EliteMenu.GetCurrentMenuItem().TabCompletionHandler;
            }
            else
            {
                EliteConsole.PrintFormattedErrorLine("Covenant login failed. Check your username and password again.");
            }
        }

        private void Launch()
        {
            this.EliteMenu.PrintMenu("");
            bool EliteStatus = true;
            while (EliteStatus)
            {
                string input = EliteConsole.Read();
                EliteStatus = this.EliteMenu.PrintMenu(input);
            }
        }

        private static string GetPassword()
		{
			string password = "";
			ConsoleKeyInfo nextKey = Console.ReadKey(true);
            while (nextKey.Key != ConsoleKey.Enter)
            {
                if (nextKey.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
						password = password.Substring(0, password.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
					password += nextKey.KeyChar;
                    Console.Write("*");
                }
                nextKey = Console.ReadKey(true);
            }
			return password;
		}
    }
}
