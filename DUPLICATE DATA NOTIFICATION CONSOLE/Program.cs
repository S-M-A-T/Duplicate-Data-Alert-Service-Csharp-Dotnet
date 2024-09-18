using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Linq;

namespace DuplicateNotificationService
{
    class Program
    {
        //static string strConn = "your_sql_server_connection_string"; // Replace with your actual connection string
        //static string mailEmail = "your_email@gmail.com"; // Replace with your email
        //static string mailPass = "your_email_password"; // Replace with your email password
        //static string mailSmtp = "smtp.gmail.com"; // SMTP server address
        //static int mailSmtpPort = 587; // SMTP server port (587 is common for TLS)
        static void Main(string[] args)
        {
            try
            {
                Console.Title = $"NOTIFICATION CONSOLE | {DateTime.Now}";
                Console.WriteLine($" *****|| DUPLICATE DATA NOTIFICATION SERVICE. PROCESS STARTED AT : {DateTime.Now:hh:mm:ss tt} ||***** ");
                Console.WriteLine("");

                CheckInvoice();

                Console.WriteLine("");
                Console.WriteLine($" *****|| DUPLICATE DATA SERVICE. PROCESS END AT : {DateTime.Now:hh:mm:ss tt} ||***** ");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void CheckInvoice()
        {
            using (SqlConnection con = new SqlConnection(strConn))
            {
                string sql = "SELECT * FROM Devisofts_data WHERE status1='D' AND status1 IS NOT NULL AND NOT EXISTS (SELECT * FROM Devisofts_EMAIL_LOG WHERE id = Devisofts_data.id) ORDER BY id";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    con.Open();
                    using (SqlDataAdapter sda = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        sda.Fill(dt);

                        Console.WriteLine($"{dt.Rows.Count} RECORDS FOUND.");
                        Console.WriteLine();

                        foreach (DataRow row in dt.Rows)
                        {
                            string aStn = row["A_STN"].ToString();
                            string scanDt = row["SCAN_DATE"].ToString();
                            string aNo = row["A_NO"].ToString();

                            string dataRow = $"DUPLICATE DATA ID #{row["id"]} FOUND DURING SECURITY CENTER, DATED: {scanDt.ToUpper()}";

                            Console.WriteLine(dataRow);

                            string emailBody = "<table cellpadding='3' cellspacing='3' style='width: 70%;font-family: Calibri;border-collapse: collapse;border: border:1px solid #C0C0C0;padding: 5px;'>" +
                                               "<tr><td bgcolor='#52178b' style='font-family: Calibri;padding:5px;border:1px solid #C0C0C0;color:White;'>" +
                                               "<h2 style='margin-top: 0px;margin-bottom: 0px;'>DUPLICATE DATA NOTIFICATION</h2></td></tr>" +
                                               $"<tr><td style='font-family: Calibri;padding:5px;border:1px solid #C0C0C0;'>{dataRow}</td></tr>" +
                                               "</table><br/><br/><div><i>Please do not reply to this email. This is a computer-generated message and hence requires no signature</i></div>";

                            string strRecipient = GetRecipientEmail(aStn);

                            if (!string.IsNullOrEmpty(strRecipient))
                            {
                                if (!IsDuplicate(aNo))
                                {
                                    SendMail($"DUPLICATE DATA NOTIFICATION - {aStn}", strRecipient, emailBody, "");
                                    LogData(row["id"].ToString(), aStn, "1", $"SUCCESSFULLY-SENT TO {strRecipient}", dataRow);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"No recipient found for station: {aStn}");
                            }
                        }
                    }
                }
            }
        }

        static bool IsDuplicate(string aNo)
        {
            using (SqlConnection conn = new SqlConnection(strConn))
            {
                conn.Open();

                string strQuery = "SELECT * FROM Devisofts_EMAIL_LOG WHERE id = @A_NO";
                using (SqlCommand cmd = new SqlCommand(strQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@A_NO", aNo);
                    SqlDataReader reader = cmd.ExecuteReader();

                    return reader.HasRows;
                }
            }
        }

        static string GetRecipientEmail(string aStn)
        {
            var recipientMap = new Dictionary<string, string>
            {
                { "Station A", "Support@Devisofts.com" },
                { "Station A", "Support@Devisofts.com" },
                { "Station A", "Support@Devisofts.com" }
            };

            return recipientMap.TryGetValue(aStn, out string email) ? email : "";
        }

        static void LogData(string id, string aStn, string emailStatus, string emailRmks, string emailText)
        {
            using (SqlConnection con = new SqlConnection(strConn))
            {
                string query = "INSERT INTO Devisofts_EMAIL_LOG(id, A_STN, EMAIL_STATUS, EMAIL_RMKS, EMAIL_TEXT) VALUES (@id, @A_STN, @EMAIL_STATUS, @EMAIL_RMKS, @EMAIL_TEXT)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@A_STN", aStn);
                    cmd.Parameters.AddWithValue("@EMAIL_STATUS", emailStatus);
                    cmd.Parameters.AddWithValue("@EMAIL_RMKS", emailRmks);
                    cmd.Parameters.AddWithValue("@EMAIL_TEXT", emailText);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        static void SendMail(string subject, string email, string msg, string cc)
        {
            using (SmtpClient client = new SmtpClient(mailSmtp, mailSmtpPort))
            {
                // Temporarily skip SSL certificate validation
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(mailEmail, mailPass);

                MailMessage message = new MailMessage
                {
                    From = new MailAddress(mailEmail, "NO-REPLY"),
                    Subject = subject,
                    Body = msg,
                    IsBodyHtml = true,
                    Priority = MailPriority.High
                };

                message.To.Add(email);

                if (!string.IsNullOrEmpty(cc))
                {
                    foreach (string to in cc.Split(';'))
                    {
                        message.CC.Add(to);
                    }
                }

                try
                {
                    client.Send(message);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error sending email: {ex.Message}");
                }
            }
        }
    }
}
