// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                string computername = ComputerNameOption.Value();
                string hash = HashOption.Value();
                try
                {
                    if (!ComputerNameOption.HasValue())
                    {
                        EliteConsole.PrintHighlight("Covenant ComputerName: ");
                        computername = EliteConsole.Read();
                    }

                    EliteConsole.PrintFormattedHighlightLine("Connecting to Covenant...");
                    Elite elite = new Elite(new Uri("https://" + computername + ":7443"));
                    bool connected = elite.Connect();
                    if (!connected)
                    {
                        EliteConsole.PrintFormattedErrorLine("Could not connect to Covenant at: " + computername);
                        if (computername.ToLower() == "localhost" || computername == "127.0.0.1")
                        {
                            EliteConsole.PrintFormattedErrorLine("Are you using Docker? Elite cannot connect over the loopback address while using Docker, because Covenant is not running within the Elite docker container.");
                        }
                        return -1;
                    }
                    if (!UserNameOption.HasValue())
                    {
                        EliteConsole.PrintHighlight("Username: ");
                        username = EliteConsole.Read();
                    }
                    if (!PasswordOption.HasValue())
                    {
                        EliteConsole.PrintHighlight("Password: ");
                        password = Utilities.GetPassword();
                        EliteConsole.PrintInfoLine();
                    }
                    if (!HashOption.HasValue())
                    {
                        EliteConsole.PrintHighlight("Covenant CertHash (Empty to trust all): ");
                        hash = EliteConsole.Read();
                    }
                    EliteConsole.PrintFormattedHighlightLine("Logging in to Covenant...");
                    bool login = elite.Login(username, password, hash);
                    if (login)
                    {
                        elite.Launch();
                        elite.CancelEventPoller.Cancel();
                    }
                    else
                    {
                        EliteConsole.PrintFormattedErrorLine("Covenant login failed. Check your username and password again.");
                        EliteConsole.PrintFormattedErrorLine("Incorrect password for user: " + username);
                        return -3;
                    }
                }
                catch (HttpRequestException e) when (e.InnerException.GetType().Name == "SocketException")
                {
                    EliteConsole.PrintFormattedErrorLine("Could not connect to Covenant at: " + computername);
                    if (computername.ToLower() == "localhost" || computername == "127.0.0.1")
                    {
                        EliteConsole.PrintFormattedErrorLine("Are you using Docker? Elite cannot connect over the loopback address while using Docker, because Covenant is not running within the Elite docker container.");
                    }
                    return -1;
                }
                catch (HttpRequestException e) when (e.InnerException.GetType().Name == "AuthenticationException")
                {
                    EliteConsole.PrintFormattedErrorLine("Covenant certificate does not match: " + hash);
                    return -2;
                }
                catch (HttpRequestException)
                {
                    EliteConsole.PrintFormattedErrorLine("Covenant login failed. Check your username and password again.");
                    EliteConsole.PrintFormattedErrorLine("Incorrect password for user: " + username);
                    return -3;
                }
                catch (Exception e)
                {
                    EliteConsole.PrintFormattedErrorLine("Unknown Exception Occured: " + e.Message + Environment.NewLine + e.StackTrace);
                    return -4;
                }
                return 0;
            });
            app.Execute(args);
        }

        public CancellationTokenSource CancelEventPoller { get; } = new CancellationTokenSource();
        private EliteMenu EliteMenu { get; set; }
        private EventPrinter EventPrinter { get; set; }
        private Uri CovenantURI { get; }
        private Task EventPoller { get; set; }

        private Elite(Uri CovenantURI)
        {
            this.CovenantURI = CovenantURI;
            this.EventPrinter = new EventPrinter();
        }

        private bool Connect()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            CovenantAPI LoginCovenantClient = new CovenantAPI(CovenantURI, new BasicAuthenticationCredentials { UserName = "", Password = "" }, clientHandler);
            LoginCovenantClient.HttpClient.Timeout = new TimeSpan(0, 0, 3);
            try
            {
                // Test connection with a blank login request
                CovenantUserLoginResult result = LoginCovenantClient.ApiUsersLoginPost(new CovenantUserLogin { UserName = "", Password = "" });
            }
            catch (HttpRequestException e) when (e.InnerException.GetType().Name == "AuthenticationException")
            {
                // Invalid login is ok, just verifying that we can reach Covenant
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private bool Login(string CovenantUsername, string CovenantPassword, string CovenantHash)
        {
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) =>
                {
                    // Cert Pinning - Trusts only the Covenant API certificate
                    if (CovenantHash == "" || cert.GetCertHashString() == CovenantHash) { return true; }
                    return false;
                }
            };

            CovenantAPI LoginCovenantClient = new CovenantAPI(this.CovenantURI, new BasicAuthenticationCredentials { UserName = "", Password = "" }, clientHandler);
            try
            {
                CovenantUserLoginResult result = LoginCovenantClient.ApiUsersLoginPost(new CovenantUserLogin { UserName = CovenantUsername, Password = CovenantPassword });
                if (result.Success ?? default)
                {
                    TokenCredentials creds = new TokenCredentials(result.Token);
                    CovenantAPI CovenantClient = new CovenantAPI(CovenantURI, creds, clientHandler);
                    this.EliteMenu = new EliteMenu(CovenantClient);
                    ReadLine.AutoCompletionHandler = this.EliteMenu.GetCurrentMenuItem().TabCompletionHandler;
                    this.EventPoller = new Task(() =>
                    {
                        int DelayMilliSeconds = 2000;
                        DateTime toDate = DateTime.FromBinary(CovenantClient.ApiEventsTimeGet().Value);
                        DateTime fromDate;
                        while (true)
                        {
                            try
                            {
                                fromDate = toDate;
                                toDate = DateTime.FromBinary(CovenantClient.ApiEventsTimeGet().Value);
                                IList<EventModel> events = CovenantClient.ApiEventsRangeByFromdateByTodateGet(fromDate.ToBinary(), toDate.ToBinary());
                                foreach (var anEvent in events)
                                {
                                    string context = this.EliteMenu.GetMenuLevelTitleStack();
                                    if (anEvent.Type == EventType.Normal)
                                    {
                                        if (this.EventPrinter.PrintEvent(anEvent, context))
                                        {
                                            this.EliteMenu.PrintMenuLevel();
                                        }
                                    }
                                    else if (anEvent.Type == EventType.Download)
                                    {
                                        DownloadEvent downloadEvent = CovenantClient.ApiEventsDownloadByIdGet(anEvent.Id ?? default);
                                        File.WriteAllBytes(Path.Combine(Common.EliteDownloadsFolder, downloadEvent.FileName), Convert.FromBase64String(downloadEvent.FileContents));
                                    }
                                }
                                Thread.Sleep(DelayMilliSeconds);
                            }
                            catch (Exception) { }
                        }
                    }, this.CancelEventPoller.Token);
                }
                else
                {
                    return false;
                }
            }
            catch (HttpOperationException)
            {
                return false;
            }
            return true;
        }

        private void Launch()
        {
            this.EventPoller.Start();
            this.EliteMenu.PrintMenu("");
            bool EliteStatus = true;
            while (EliteStatus)
            {
                string input = EliteConsole.Read();
                EliteStatus = this.EliteMenu.PrintMenu(input);
            }
        }
    }
}
