using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Housekeeper.Models;

namespace Housekeeper.Controllers
{
    public class UsersMasterController : Controller
    {
        #region//Registration action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        #endregion

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

        #region //Login
        [HttpGet]
        public ActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(UserLogin login, string ReturnUrl)
        {
            string message = "";

            using(HousekeeperDataEntities dc=new HousekeeperDataEntities())
            {
                var v = dc.UsersMasters.Where(a => a.UserName == login.Username).FirstOrDefault();
                if (v != null)
                {
                    if (v.isEmailVerified == false)
                    {
                        message = "Please activate your account to continue login.";
                    }
                    else
                    {
                        if (string.Compare(Crypto.Hash(login.Password), v.Password) == 0)
                        {
                            //Process to save login
                            int timeout = login.SaveLogin ? 525600 : 1; //525600 min = 1 year
                            var ticket = new FormsAuthenticationTicket(login.Username, login.SaveLogin, timeout);
                            string encrypted = FormsAuthentication.Encrypt(ticket);
                            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                            {
                                Expires = DateTime.Now.AddMinutes(timeout),
                                HttpOnly = true
                            };
                            Response.Cookies.Add(cookie);

                            if (Url.IsLocalUrl(ReturnUrl))
                            {
                                return Redirect(ReturnUrl);
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else
                        {
                            message = "Invalid password!";
                            ModelState.AddModelError("InvalidPassword", message);
                        }
                    }
                }
                else
                {
                    message = "Invalid username!";
                    ModelState.AddModelError("InvalidUsername", message);
                }
            }

            ViewBag.Message = message;
            return View();
        }
        #endregion
        
        #region//Logout
        [Authorize]
        [HttpPost]
        public ActionResult logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("UserLogin", "UsersMaster");
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

        #region//Forgot password
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPassword(string Email)
        {
            string message = "";
            bool Status = false;

            //Verify email address
            using (HousekeeperDataEntities dc=new HousekeeperDataEntities())
            {
                var v = dc.UsersMasters.Where(a => a.Email == Email).FirstOrDefault();
                if (v != null)
                {
                    SendForgotPwdEmail(v.UserId, Email, v.UserName);
                    message = "Reset password email has been sent to your registered email account.";
                    Status = true;
                }
                else
                {
                    message = "Email not found.";
                }
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region// Public procedure Email forgot password
        [NonAction]
        public void SendForgotPwdEmail(int userId, string emailAddress, string userName)
        {
            var verifyURL = "/UsersMaster/ResetPassword/" + userId;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyURL);

            var fromEmail = new MailAddress("c.weihon@gmail.com", "Housekeeper");
            var toEmail = new MailAddress(emailAddress, userName);
            var fromEmailPwd = "810821n811204";
            string subject = "Housekeeper Reset Password";
            string body = "<br/><br/>Dear Housekeeper," +
                "<br/>Please click below link for reset your login password." +
                "<br/><br/><a href='" + link + "'>Reset Password</a>";
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

        #region//Reset password
        [HttpGet]
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string id, UsersMaster user)
        {
            bool Status = false;
            string message = "";
            int userID = Convert.ToInt32(id);
            
            if (id != null)
            {
                using (HousekeeperDataEntities dc = new HousekeeperDataEntities())
                {
                    dc.Configuration.ValidateOnSaveEnabled = false;
                    var v = dc.UsersMasters.Where(a => a.UserId == userID).FirstOrDefault();
                    {
                        if (v != null)
                        {
                            v.Password = Crypto.Hash(user.Password);
                            dc.SaveChanges();
                            message = "Password changed!";
                            Status = true;
                        }
                        else
                        {
                            message = "Invalid request!";
                        }
                    }
                }
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return RedirectToAction("Index", "Home");
        }
        #endregion
    }
}