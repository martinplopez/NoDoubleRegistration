# UniqueEmailRegistrations – Dynamics 365 Plugin

Ensure that a contact can register **only once per event** via Real-Time Marketing / form submissions in Dynamics 365.  
This plugin validates a form submission by checking whether the submitted **email address** already has an **event registration** for the specified **event**.

> ✅ If the email is **not** already registered for the event → the submission is valid.  
> 🚫 If the email **is** already registered → the plugin flags the `emailaddress1` field as invalid so the form can show an inline error.

## Installation

To install the plugin in your Dynamics 365 / Dataverse environment:

1. Go to the [Releases](../../releases) section of this repository.
2. Download the latest **Managed Solution (.zip)** file.
3. In your Dynamics 365 environment, navigate to **Advanced Settings → Solutions**.
4. Click **Import**, then upload the managed solution zip file.
5. Follow the prompts to complete the import.

After import, the plugin will automatically be registered and available for use in Real-Time Marketing form validation.
