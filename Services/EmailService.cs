﻿using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace BookMoth_Api_With_C_.Services
{
    public class EmailService
    {
        private readonly string _confirmEmailContent =
            "<div class='header'>" +
            "   <h1>Xác thực tài khoản</h1>" +
            "</div>" +
            "<div class='content'>" +
            "   <h2>Chào {name},</h2>" +
            "   <p>Cảm ơn bạn đã đăng ký tài khoản với chúng tôi!</p>" +
            "   <p>Để hoàn tất quá trình đăng ký, xin vui lòng nhập mã bên dưới để xác nhận tài khoản của bạn:</p>" +
            "   <a href='{{ConfirmationLink}}' class='button'>{code}</a>" +
            "   <p>Nếu bạn không thực hiện yêu cầu này, bạn có thể bỏ qua email này.</p>" +
            "   <p>Trân trọng,<br>Đội ngũ hỗ trợ</p>" +
            "</div>" +
            "<div class='footer'>" +
            "   <p>&copy; {DateTime.Now.Year} BookMoth. Bảo mật thông tin của bạn là ưu tiên hàng đầu của chúng tôi.</p>" +
            "</div>";

        private readonly string _forgotPasswordContent =
            "<div class='header'>" +
            "   <h1>Đặt lại mật khẩu</h1>" +
            "</div>" +
            "<div class='content'>" +
            "   <h2>Chào {name},</h2>" +
            "   <p>Gần đây bạn đã gửi yêu cầu đặt lại mật khẩu!</p>" +
            "   <p>Để tiếp tục, xin vui lòng nhập mã bên dưới:</p>" +
            "   <a href='{{ConfirmationLink}}' class='button'>{code}</a>" +
            "   <p>Nếu bạn không thực hiện yêu cầu này, bạn có thể bỏ qua email này.</p>" +
            "   <p>Trân trọng,<br>Đội ngũ hỗ trợ</p>" +
            "</div>" +
            "<div class='footer'>" +
            "   <p>&copy; {DateTime.Now.Year} BeA Fashion. Bảo mật thông tin của bạn là ưu tiên hàng đầu của chúng tôi.</p>" +
            "</div>";
        private readonly string _OpenWalletEmailContent =
            "<div class='header'>" +
            "   <h1>Thông báo mở ví thanh toán</h1>" +
            "</div>" +
            "<div class='content'>" +
            "   <h2>Chào {name},</h2>" +
            "   <p>Chúng tôi rất vui mừng thông báo rằng bạn mở thành công ví thanh toán BookMoth!</p>" +
            "  {code} " +
            "   <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>" +
            "   <p>Trân trọng,<br>Đội ngũ hỗ trợ</p>" +
            "</div>" +
            "<div class='footer'>" +
            "   <p>&copy; {DateTime.Now.Year} BookMoth. Bảo mật thông tin của bạn là ưu tiên hàng đầu của chúng tôi.</p>" +
            "</div>";
        private readonly string _paymentSuccessContent =
            "<div class='header'>" +
            "   <h1>Thanh toán thành công</h1>" +
            "</div>" +
            "<div class='content'>" +
            "   <h2>Chào {name},</h2>" +
            "   <p>Cảm ơn bạn đã mua hàng tại BookMoth!</p>" +
            "   <p>Đơn hàng của bạn đã được thanh toán thành công.</p>" +
            "   {code}" +
            "   <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>" +
            "   <p>Trân trọng,<br>Đội ngũ hỗ trợ</p>" +
            "</div>" +
            "<div class='footer'>" +
            "   <p>&copy; {DateTime.Now.Year} BookMoth. Cảm ơn bạn đã tin tưởng chúng tôi.</p>" +
            "</div>";
        private readonly string _newPurchaseContent =
            "<div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>" +
            "<div style='background-color: #4CAF50; padding: 15px; text-align: center; color: white; border-radius: 10px 10px 0 0;'>" +
            "<h1>Thanh toán thành công!</h1>" +
            "</div>"+
            "<div style='padding: 20px;'>"+
            "<h2>Chào {name},</h2>" +
            "<p>Cảm ơn bạn đã mua hàng tại <strong>BookMoth</strong>!</p>" +
            "<p>Đơn hàng của bạn đã được thanh toán thành công.</p>"+
            "<p>Bạn có thể kiểm tra chi tiết đơn hàng trong tài khoản của mình.</p>"+
            "<p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi.</p>"+
            "<p>Trân trọng,<br><strong>Đội ngũ hỗ trợ BookMoth</strong></p>"+
            "{code}"+
            "</div>"+
            "<div style='background-color: #f8f8f8; padding: 10px; text-align: center; font-size: 12px; color: #777; border-radius: 0 0 10px 10px;'>"+
            "<p>&copy; {DateTime.Now.Year} BookMoth. Cảm ơn bạn đã tin tưởng chúng tôi.</p>"+
            "</div>"+
            "</div>";

        private readonly string fromMail;
        private readonly string fromPassword;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            fromMail = emailSettings.Value.FromMail;
            fromPassword = emailSettings.Value.FromPassword;
        }

        public async Task<int> SendEmailAsync(string to, string name, string code, int status)
        {
            MailMessage message = new MailMessage
            {
                From = new MailAddress(fromMail),
                Subject = GetEmailSubject(status),
                Body = GetEmailTemplate(to, name, code, status),
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(to));

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromMail, fromPassword),
                EnableSsl = true,
            };

            try
            {
                await smtpClient.SendMailAsync(message);
                return 1; // Gửi thành công
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
                return 0; // Gửi thất bại
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0; // Gửi thất bại
            }
        }

        private string GetEmailSubject(int status)
        {
            return status switch
            {
                1 => "Xác thực tài khoản",
                2 => "Đặt lại mật khẩu",
                3 => "Thông báo mở ví thanh toán BookMoth",
                4 => "Xác nhận thanh toán thành công",
                5 => "Thông báo đơn hàng",
                _ => "Thông báo từ BeA Fashion"
            };
        }
        private string GetEmailTemplate(string receiver, string name, string code, int status)
        {
            var body =
                @"<!DOCTYPE html>
                <html lang='vi'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {
                            font-family: Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif, Apple Color Emoji, Segoe UI Emoji, Segoe UI Symbol, Noto Color Emoji;
                            background-color: #f7f7f7;
                            margin: 0;
                            padding: 0;
                        }
                        .container {
                            width: 100%;
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                            overflow: hidden;
                        }
                        .header {
                            background-color: #4CAF50;
                            color: white;
                            padding: 20px;
                            text-align: center;
                        }
                        .content {
                            padding: 20px;
                        }
                        .footer {
                            text-align: center;
                            padding: 10px;
                            font-size: 12px;
                            color: #777;
                        }
                        .button {
                            display: inline-block;
                            padding: 10px 20px;
                            margin: 20px 0;
                            color: white;
                            background-color: #4CAF50;
                            text-decoration: none;
                            border-radius: 5px;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                    {content}
                    </div>
                </body>
                </html>";
            string content;
            if (status == 3)
            {
                content = _OpenWalletEmailContent;
            }
            else if (status == 4)
            {
                content = _paymentSuccessContent;
            }
            else if (status == 5)
            {
                content = _newPurchaseContent;
            }
            else
            {
                content = status == 1 ? _confirmEmailContent : _forgotPasswordContent;
            }
            body = content;
            body = body.Replace("{name}", name)
                       .Replace("{code}", code)
                       .Replace("{DateTime.Now.Year}", DateTime.Now.Year.ToString());

            return body;
        }
    }
}
