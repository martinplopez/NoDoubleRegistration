using System;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Xrm.Sdk.Query;

namespace UniqueEmailRegistrations
{

    public class CheckIfEmailRegistered : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);


            var requestString = context.InputParameters.Contains("msdynmkt_formsubmissionrequest")
                ? (string)context.InputParameters["msdynmkt_formsubmissionrequest"]
                : null;

            if (string.IsNullOrWhiteSpace(requestString))
            {
                tracing.Trace("No msdynmkt_formsubmissionrequest found; skipping validation.");
                context.OutputParameters["msdynmkt_validationresponse"] = Serialize(new ValidateFormSubmissionResponse
                {
                    IsValid = true,
                    ValidationOnlyFields = new List<string>()
                });
                return;
            }

            var submission = Deserialize<FormSubmissionRequest>(requestString);

            string GetField(string key)
            {
                if (submission?.Fields != null && submission.Fields.TryGetValue(key, out var value))
                    return value;
                return null;
            }

            var emailRaw = GetField("emailaddress1");
            var eventRaw = GetField("event");

            if (string.IsNullOrWhiteSpace(emailRaw))
            {
                tracing.Trace("Email field is empty or missing; skipping duplicate validation.");
                context.OutputParameters["msdynmkt_validationresponse"] = Serialize(new ValidateFormSubmissionResponse
                {
                    IsValid = true,
                    ValidationOnlyFields = new List<string>()
                });
                return;
            }


            if (!Guid.TryParse(eventRaw, out var eventId) || eventId == Guid.Empty)
            {
                tracing.Trace($"Event field missing or not a GUID: '{eventRaw}'. Skipping duplicate validation.");
                context.OutputParameters["msdynmkt_validationresponse"] = Serialize(new ValidateFormSubmissionResponse
                {
                    IsValid = true,
                    ValidationOnlyFields = new List<string>()
                });
                return;
            }

            var fetchXml = $@"
                <fetch>
                  <entity name='msevtmgt_eventregistration'>
                    <attribute name='msevtmgt_eventregistrationid' />
                    <filter>
                      <condition attribute='msevtmgt_eventid' operator='eq' value='{eventId}' />
                    </filter>
                    <link-entity name='contact' from='contactid' to='msevtmgt_contactid' link-type='inner'>
                      <filter>
                        <condition attribute='emailaddress1' operator='eq' value='{emailRaw}' />
                      </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            tracing.Trace("Running duplicate check with FetchXML:");
            tracing.Trace(fetchXml);

            bool alreadyRegistered = false;
            try
            {
                var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
                alreadyRegistered = (result != null && result.Entities != null && result.Entities.Count > 0);
                tracing.Trace($"Duplicate check result: alreadyRegistered={alreadyRegistered}");
            }
            catch (Exception ex)
            {
                tracing.Trace("Error during duplicate check: " + ex.ToString());
            }

            var response = new ValidateFormSubmissionResponse
            {
                IsValid = !alreadyRegistered,
                ValidationOnlyFields = alreadyRegistered ? new List<string> { "emailaddress1" } : new List<string>()
            };

            context.OutputParameters["msdynmkt_validationresponse"] = Serialize(response);
        }
        private T Deserialize<T>(string jsonString)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            T result;
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                result = (T)serializer.ReadObject(stream);
            }
            return result;
        }

        private string Serialize<T>(T obj)
        {
            string result;
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, obj);
                result = Encoding.Default.GetString(memoryStream.ToArray());
            }
            return result;
        }
        public class FormSubmissionRequest { public Dictionary<string, string> Fields { get; set; } }
        public class ValidateFormSubmissionResponse { public bool IsValid { get; set; } public List<string> ValidationOnlyFields { get; set; } }

    }
}
