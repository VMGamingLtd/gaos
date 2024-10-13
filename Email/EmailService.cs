#pragma warning disable 8600, 8602, 8604

using Serilog;
using Gaos.Lang;
using Gaos.Templates;
using Gaos.Templates.Email;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using MailKit;

namespace Gaos.Email
{
    public class EmailService
    {
        public static string CLASS_NAME = typeof(EmailService).Name;

        private static bool IS_DEBUG = false;

        private IConfiguration Configuration;
        private LanguageService LanguageService;
        private TemplateService TemplateService;

        private string emailSmtpServer;
        private string emailSmtpServerPort;
        private string emailSmtpUser;
        private string emailSmtpPassword;

        private string domainName;
        private string domainNamePrefix;

        private string emailInfoUserFullName;
        private string emailInfoUserEmail;

        public EmailService(IConfiguration configuration, LanguageService languageService, TemplateService templateService)
        {
            const string METHOD_NAME = "EmailService";

            this.Configuration = configuration;
            this.LanguageService = languageService;
            this.TemplateService = templateService;

            if (Configuration["email_smtp_server"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_smtp_server");
                throw new Exception("missing configuration value: email_smtp_server");
            }
            emailSmtpServer = Configuration["email_smtp_server"];
            if (Configuration["email_smtp_server_port"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_smtp_server_port");
                throw new Exception("missing configuration value: email_smtp_server_port");
            }
            emailSmtpServerPort = Configuration["email_smtp_server_port"];
            if (Configuration["email_smtp_user"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_smtp_user");
                throw new Exception("missing configuration value: email_smtp_user");
            }
            emailSmtpUser = Configuration["email_smtp_user"];
            if (Configuration["email_smtp_password"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_smtp_password");
                throw new Exception("missing configuration value: email_smtp_password");
            }
            emailSmtpPassword = Configuration["email_smtp_password"];
            emailSmtpPassword = Gaos.Encryption.EncryptionHelper.Decrypt(emailSmtpPassword);

            if (Configuration["domain_name"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: domain_name");
                throw new Exception("missing configuration value: domain_name");
            }
            domainName = Configuration["domain_name"];
            if (Configuration["domain_name_prefix"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: domain_name_prefix");
                throw new Exception("missing configuration value: domain_name_prefix");
            }
            domainNamePrefix = Configuration["domain_name_prefix"];
            if (Configuration["email_info_user_full_name"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_info_user_full_name");
                throw new Exception("missing configuration value: email_info_user_full_name");
            }
            emailInfoUserFullName = Configuration["email_info_user_full_name"];
            if (Configuration["email_info_user_email"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: email_info_user_email");
                throw new Exception("missing configuration value: email_info_user_email");
            }
            emailInfoUserEmail = Configuration["email_info_user_email"];

        }

        private SmtpClient getSmtpClient()
        {
            if (IS_DEBUG)
            {
                return new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
            }
            else
            {
                return new SmtpClient();
            }

        }

        private void SendHtmlEmail(string to, string subject, string htmlBody)
        {
            const string METHOD_NAME = "SendHtmlEmail";

            try
            {

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(emailInfoUserFullName, emailInfoUserEmail));
                message.To.Add(new MailboxAddress(String.Empty, to));
                message.Subject = subject;
                message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                // send email
                using (var smtp = getSmtpClient())
                {
                    smtp.Connect(emailSmtpServer, int.Parse(emailSmtpServerPort), MailKit.Security.SecureSocketOptions.StartTls);
                    smtp.Authenticate(emailSmtpUser, emailSmtpPassword);
                    smtp.Send(message);
                    smtp.Disconnect(true);
                }

            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME}: error: {e.Message}");
                throw new Exception("error sending email");
            }
        }

        private async Task SendHtmlEmailAsync(string to, string subject, string htmlBody)
        {
            await Task.Run(() => SendHtmlEmail(to, subject, htmlBody));

        }

        public async Task SendVerificationEmail(string to, string verificationCode)
        {
            const string METHOD_NAME = "SendVerificationEmail()";

            try
            {
                string domain = $"{domainNamePrefix}.{domainName}";
                string verifyEmailUrl = $"https://{domain}/verifyEmail.html?code={verificationCode}";

                string subject;

                Language lang = LanguageService.GetLanguage();
                if (lang == Language.english)
                {
                    subject = "Verify your email";
                }
                else if (lang == Language.russian)
                {
                    subject = "Подтвердите свой адрес электронной почты";
                }
                else if (lang == Language.chinese)
                {
                    subject = "验证您的电子邮件";
                }
                else if (lang == Language.slovak)
                {
                    subject = "Overte svoj e-mail";
                }
                else
                {
                    subject = "Verify your email";
                }


                string body;

                body = VerifyEmailTemplate.GenerateVerifyEmailTemplate(TemplateService, verifyEmailUrl);

                await SendHtmlEmailAsync(to, subject, body);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME}: error: {e.Message}");
            }
        }

        public async Task SendUserVerificationCode(string to, string verificationCode)
        {
            const string METHOD_NAME = "SendUserVerificationCode()";

            try
            {
                string subject;

                Language lang = LanguageService.GetLanguage();
                if (lang == Language.english)
                {
                    subject = "verification code";
                }
                else if (lang == Language.russian)
                {
                    subject = "проверочный код";
                }
                else if (lang == Language.chinese)
                {
                    subject = "验证码";
                }
                else if (lang == Language.slovak)
                {
                    subject = "overovací kód";
                }
                else
                {
                    subject = "verification code";
                }


                string body;

                body = VerificationCodeTemplate.GenerateVerificationCodeEmailTemplate(TemplateService, verificationCode);

                await SendHtmlEmailAsync(to, subject, body);
            }
            catch (Exception e)
            {
                Log.Error(e, $"{CLASS_NAME}:{METHOD_NAME}: error: {e.Message}");
            }
        }
    }
}
