using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Pipelines;
using Sitecore.WFFM.Abstractions.Actions;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Mail;
using Sitecore.WFFM.Abstractions.Shared;
using System;
using System.Net;

namespace Sitecore.Support.Forms.Core.Dependencies
{
  public class DefaultImplMailSender : IMailSender
  {
    private readonly IItemRepository itemRepository;

    [Obsolete]
    public DefaultImplMailSender() : this(DependenciesManager.Resolve<IItemRepository>())
    {
    }

    public DefaultImplMailSender(IItemRepository itemRepository)
    {
      Assert.ArgumentNotNull(itemRepository, "itemRepository");
      this.itemRepository = itemRepository;
    }

    protected ICredentialsByHost GetCredentials(string login, string password)
    {
      ICredentialsByHost host = null;
      string[] strArray = string.IsNullOrEmpty(login) ? new string[] { string.Empty } : login.Split(new char[] { '\\' });
      if ((strArray.Length <= 0) || string.IsNullOrEmpty(strArray[0]))
      {
        return host;
      }
      if ((strArray.Length == 2) && !string.IsNullOrEmpty(strArray[1]))
      {
        return new NetworkCredential(strArray[1], password, strArray[0]);
      }
      return new NetworkCredential(strArray[0], password);
    }

    public void SendMail(IEmailAttributes emailAttributes)
    {
      this.SendMail(emailAttributes, ID.Null, null, null);
    }

    public void SendMail(IEmailAttributes emailAttributes, ID formId, AdaptedResultList fields, object[] data)
    {
      // start of the custom code
      MessageType msgtype = MessageType.Email;

      if (emailAttributes.MessageType == MessageType.Sms || emailAttributes.MessageType == MessageType.Mms)
      {
        msgtype = emailAttributes.MessageType;
      }

      ProcessMessageArgs args = new ProcessMessageArgs(formId, fields, msgtype, emailAttributes)
      {
        //end of the custom code
        IncludeAttachment = emailAttributes.IsIncludeAttachments,
        Recipient = emailAttributes.Recipient,
        RecipientGateway = emailAttributes.RecipientGateway,
        IsBodyHtml = emailAttributes.IsBodyHtml,
        EnableSsl = emailAttributes.EnableSsl,
        Credentials = this.GetCredentials(emailAttributes.Login, emailAttributes.Password)
      };

      args.Data.Add("FromPhone", emailAttributes.FromPhone ?? string.Empty);
      CorePipeline.Run("processMessage", args);
    }

    public void SendMailWithGlobalParameters(IEmailAttributes emailAttributes)
    {
      IActionItem item = this.itemRepository.CreateAction(FormIDs.SendEmailActionID);
      ReflectionUtils.SetXmlProperties(emailAttributes, item.GlobalParameters, true);
      this.SendMail(emailAttributes, ID.Null, null, null);
    }
  }
}
