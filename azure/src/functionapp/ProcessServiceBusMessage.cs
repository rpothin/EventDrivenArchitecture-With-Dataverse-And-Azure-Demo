using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace azd.Dataverse.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder){}
    }

    public class ProcessServiceBusMessage
    {
        private readonly ServiceClient _client;

        private struct ContactMessage {
            public string firstName;
            public string lastName;
            public string email;
        }; 

        public ProcessServiceBusMessage()
        {
            // Initialize variables
            string environmentUrl = System.Environment.GetEnvironmentVariable("ENVIRONMENT-URL", EnvironmentVariableTarget.Process);
            string clientId = System.Environment.GetEnvironmentVariable("CLIENT-ID", EnvironmentVariableTarget.Process);
            string clientSecret = System.Environment.GetEnvironmentVariable("CLIENT-SECRET", EnvironmentVariableTarget.Process);

            // Create a connection (service client) for the considered environment
            string additionalConnectionStringParameters = $";AuthType=ClientSecret;ClientId={clientId};ClientSecret={clientSecret}";
            string connectionString = $"Url={environmentUrl};RedirectUri=http://localhost;LoginPrompt=Auto{additionalConnectionStringParameters}";

            this._client = new ServiceClient(connectionString);
        }

        [FunctionName("ProcessServiceBusMessage")]
        public void Run([ServiceBusTrigger("dataverse-inbound-function", Connection = "ServiceBusConnection")]string myQueueItem, ILogger log)
        {
            /* Expected message format
            {
                "firstName": "Test",
                "lastName": "Rpo 20221023 01",
                "email": "test.rpo.2022102301@test.com"
            }
            */

            // Deserialize the message
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            ContactMessage contactMessage = JsonConvert.DeserializeObject<ContactMessage>(myQueueItem);

            // Send a WhoAmI request to obtain information about the logged on user
            WhoAmIResponse whoAmIResponse = new WhoAmIResponse();
            whoAmIResponse = (WhoAmIResponse)_client.Execute(new WhoAmIRequest());

            string userId = whoAmIResponse.UserId.ToString();

            log.LogInformation($"Logged on user id: {userId}");

            // Create a new contact
            Entity contact = new Entity("contact");
            contact["firstname"] = contactMessage.firstName;
            contact["lastname"] = contactMessage.lastName;
            contact["emailaddress1"] = contactMessage.email;

            Guid contactId = _client.Create(contact);

            log.LogInformation($"Contact created with id: {contactId.ToString()}");
        }
    }
}
