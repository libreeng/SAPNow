using FSMExtension.Models;
using FSMExtension.Models.Fsm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSMExtension.Services
{
    public interface IContactService
    {
        Task<List<Contact>> GetContactsAsync(UserDetails user, ClientCredentials clientCreds, string dtoType, string dtoId);
    }

    public class ContactService : IContactService
    {
        private readonly IFsmApiClient _fsmApi;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly LinkGenerator _linkGenerator;

        public ContactService(IFsmApiClient fsmApi,
            IHttpContextAccessor contextAccessor,
            LinkGenerator linkGenerator)
        {
            _fsmApi = fsmApi;
            _contextAccessor = contextAccessor;
            _linkGenerator = linkGenerator;
        }

        public async Task<List<Contact>> GetContactsAsync(UserDetails user, ClientCredentials clientCreds, string dtoType, string dtoId)
        {
            var contacts = new List<Contact>();
            switch (dtoType)
            {
                case "Activity":
                    {
                        var activity = await _fsmApi.GetActivityAsync(user, clientCreds, dtoId);
                        if (activity == null)
                            return contacts;

                        // First, see if this activity has associated Equipment, and if so, a contact for that Equipment
                        var equipmentContacts = await GetContactsAsync(user, clientCreds, "Equipment", activity.EquipmentId);
                        contacts.AddRange(equipmentContacts);

                        // Next, include the Activity's Contact
                        var activityContact = await _fsmApi.GetContactAsync(user, clientCreds, activity.Contact);
                        var expertEmail = activityContact?.EmailAddress;

                        if (activityContact != null && !string.IsNullOrEmpty(expertEmail))
                        {
                            var expertName = $"{activityContact.FirstName} {activityContact.LastName}";
                            var expertTitle = activityContact.PositionName;
                            var contact = new Contact
                            {
                                Name = expertName,
                                Title = expertTitle,
                                Role = ContactRole.Expert,
                                Email = expertEmail
                            };

                            contacts.Add(contact);
                        }

                        // Get responsible's details from activity.responsibles[]. This is the assigned field worker.
                        var responsibles = await _fsmApi.GetPersonsAsync(user, clientCreds, activity.Responsibles);
                        contacts.AddRange(responsibles.Select(r => new Contact
                        {
                            Name = $"{r.FirstName} {r.LastName}",
                            Title = r.PositionName ?? r.JobTitle,
                            Role = ContactRole.FieldTech,
                            Email = r.EmailAddress
                        }));
                    }
                    break;

                case "Equipment":
                    {
                        var equipment = await _fsmApi.GetEquipmentAsync(user, clientCreds, dtoId);
                        if (equipment == null)
                            return contacts;

                        // Use Equipment's designated expert, if available
                        var equipmentContact = equipment.RemoteExpert;
                        if (equipmentContact != null)
                        {
                            contacts.Add(new Contact
                            {
                                Name = equipmentContact.FirstName,
                                Title = string.Empty,
                                Role = ContactRole.Expert,
                                Email = equipmentContact.EmailAddress
                            });
                        }
                    }
                    break;
            }

            return contacts.Distinct(new ContactComparer()).ToList();
        }
    }
}
