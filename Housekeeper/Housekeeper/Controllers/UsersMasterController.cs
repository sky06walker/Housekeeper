using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Housekeeper.Models;

namespace Housekeeper.Controllers
{
    public class UsersMasterController : Controller
    {
        //Registration action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        #region//Registration POST action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "UserId,isEmailVerified,ActivationCode,RegistrationDate,LastModified")] UsersMaster user)
        {
            bool Status = false;
            String Message = "";

            //Model validation
            if (ModelState.IsValid)
            {
                #region //Email exist validation
                var isExist = IsEmailExist(user.Email);

                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exist!");
                    return View(user);
                }
                #endregion

                #region //Username exist validation
                var isUsernameExist = IsUsernameExist(user.UserName);

                if (isUsernameExist)
                {
                    ModelState.AddModelError("UsernameExist", "Username already exist!");
                    return View(user);
                }
                #endregion

                #region//Generate Activation code
                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region//Password hashing
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);
                #endregion

                #region//Set default value for unshow data
                user.LastModified = DateTime.Now;
                user.RegistrationDate = DateTime.Now;
                user.isEmailVerified = false;
                #endregion

                #region//Save data
                using (HousekeeperDataEntities dc =new HousekeeperDataEntities())
                {
                    dc.UsersMasters.Add(user);
                    dc.SaveChanges();

                    //Send email to user
                    SendActivationEmail(user.Email, user.ActivationCode.ToString(), user.UserName);
                    Message = "Registration done, activation email had sent to your registered email " +
                              "address: " + user.Email;
                    Status = true;
                }
                #endregion
            }
            else
            {
                Message = "Invalid request!";
            }

            ViewBag.Message = Message;
            ViewBag.Status = Status;
            return View(user);
        }
        #endregion

        #region//Account Activation
        [HttpGet]
        public ActionResult AccountActivation (string id)
        {
            bool Status = false;

            using (HousekeeperDataEntities dc=new HousekeeperDataEntities())
            {
                dc.Configuration.ValidateOnSaveEnabled = false; //Without this line dc.SaveChanges will hit error

                var v = dc.UsersMasters.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (v != null)
                {
                    v.isEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid request!";
                }
            }
            ViewBag.Status = Status;
            return View();
        }
        #endregion

        #region//Public function to check duplicate email
        [NonAction]
        public bool IsEmailExist(string EmailAddress)
        {
            using (HousekeeperDataEntities dc =new HousekeeperDataEntities())
            {
                var v = dc.UsersMasters.Where(a => a.Email == EmailAddress).FirstOrDefault();
                return v != null;
            }
        }
        #endregion

        #region//Public function to check duplicate username
        [NonAction]
        public bool IsUsernameExist(string Username)
        {
            using (HousekeeperDataEntities dc =new HousekeeperDataEntities())
            {
                var u = dc.UsersMasters.Where(a => a.UserName == Username).FirstOrDefault();
                return u != null;
            }
        }
        #endregion

        #region//Public procedure to send activation email
        [NonAction]
        public void SendActivationEmail(string emailAddress, string activationCode, string userName)
        {
            var verifyURL = "/UsersMaster/AccountActivation/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyURL);

            var fromEmail = new MailAddress("c.weihon@gmail.com", "Housekeeper");
            var toEmail = new MailAddress(emailAddress, userName);
            var fromEmailPwd = "810821n811204";
            string subject = "Housekeeper Account Activation";
            string body = "<br/><br/>Welcome to Housekeeper." +
                "<br/>Please click below link for account activation." +
                "<br/><br/><a href='" + link + "'>Activate</a>";
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPwd)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }
        #endregion

    }
}