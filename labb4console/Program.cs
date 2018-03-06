using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Net.Mail;

namespace labb4console
{
    class Program
    {
        private const string EndpointUrl = "https://iagohfs.documents.azure.com:443/";
        private const string PrimaryKey = "HEIXXkPKNV6iw1ldqS8PPxE3uK9FpMej7nAXP0xF3f89q0LSW7ZXGIvbkbUPN0ZMiEdotw7w7iwnJgKpHAsueA==";
        private DocumentClient client;

        private string UserEmail { get; set; }
        private static string databaseName = "Users"; // Default databas och collection namn.
        private static string collectionName = "e-mails";

        private bool Exit { get; set; }

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.Exit = false;
                p.Menu(p);

            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("Good-Bye!");
                Thread.Sleep(1000);
            }
        }

        public void Menu(Program p)
        {
            while (!Exit)
            {
                Console.WriteLine("Welcome, what would you like to do?\n\n" +
                    "1. Add a new e-mail\n" +
                    "2. See existing users\n\n" +
                    "0. Exit\n" +
                    "Delete. Clear Console\n");

                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D1:
                        Console.Write(". Add a new e-mail");
                        Console.WriteLine();
                        p.Start().Wait();
                        break;

                    case ConsoleKey.D2:
                        Console.Write(". See existing users");
                        Console.WriteLine();
                        p.CheckUsersInDatabase();
                        break;

                    case ConsoleKey.Delete:
                        Console.Clear();
                        break;

                    case ConsoleKey.D0:
                        Console.Write(". Exit");
                        Console.WriteLine();
                        Exit = true;
                        break;
                }

            }

        }

        public void CheckUsersInDatabase()
        {
            this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            IQueryable<User> userQuery = this.client.CreateDocumentQuery<User>(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), "SELECT * FROM Users", queryOptions);

            Console.WriteLine("Getting Users..");
            Console.WriteLine();

            foreach (User user in userQuery)
            {
                Console.WriteLine("User email: {0}", user.Id);
            }

            Console.WriteLine();
        }

        private async Task Start()
        {
            User email;

            // Kopplar sig itll azure cosmos

            this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);

            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });

            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), new DocumentCollection { Id = collectionName });

            Console.WriteLine("Please enter your email below:");

            string input = Console.ReadLine();
            Console.WriteLine();

            // Kollar om input är giltig email

            while (!EmailIsValid(input))
            {
                Console.WriteLine("Email is not valid, try again.");

                input = Console.ReadLine();
            }

            email = new User() { Id = input };

            // Lägger till ny user till e-mails collections i Users databas om email stämmer

            await this.CreateUserIfNotExists(databaseName, collectionName, email);
        }

        public bool EmailIsValid(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private async Task CreateUserIfNotExists(string databaseName, string collectionName, User user)
        {
            try
            {
                await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, user.Id));
                this.WriteToConsoleAndContinue("User {0} exists in database.", user.Id);
                Console.WriteLine();
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), user);
                    this.WriteToConsoleAndContinue("User {0} added to database", user.Id);
                    Console.WriteLine();
                }
                else
                {
                    throw;
                }
            }
        }

        private void WriteToConsoleAndContinue(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
