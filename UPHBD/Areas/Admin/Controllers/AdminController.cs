using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using UPHBD.Models;



namespace UPHBD.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        readonly UPHBDContext _db = new UPHBDContext();

        [HttpGet]
        public ActionResult Directory()
        {
            try
            {
                if (Session["userId"] != null)
                {
                    ViewData["Action"] = "CreateDirectory";
                    ViewData["btnUpdate"] = "disabled";
                    ViewData["Directory"] = _db.Directories.OrderByDescending(n => n.id).ToList();
                    ViewBag.Designation = new SelectList(_db.Designations, "id", "DesignationName");

                    return View();
                }
            }
            catch (Exception)
            {

            }
            return RedirectToAction("Login", "Home", new { area = "" });
        }



        [HttpPost]
        public ActionResult CreateDirectory([Bind(Exclude = "id")]UPHBD.Models.Directory directory, FormCollection formCollection)
        {
            try
            {
                var text = formCollection["hidText"];

                // var errors = ModelState.Values.SelectMany(v => v.Errors);
                if (ModelState.IsValid)
                {
                    _db.spCreateDirectory(null, directory.Name, text, directory.CUGNo);
                    //                    _db.Directories.Add(directory);



                    TempData["msg"] = "<script>alert('Submitted succesfully');</script>";
                    return RedirectToAction("Directory");
                }
            }
            catch (Exception)
            {

            }
            ViewData["Action"] = "EditDirectory";
            return View("Directory");
        }

        // Edit News  Get
        [HttpGet]
        public ActionResult EditDirectory(int id)
        {

            var des = (from sub in _db.Directories where sub.id == id select sub.Designation).First();
            var selected = (from sub in _db.Designations where sub.DesignationName.Contains(des) select sub.id).First();

            ViewBag.Designation = new SelectList(_db.Designations, "id", "DesignationName", selected);
            Models.Directory directory = _db.Directories.Single(l => l.id == id);
            try
            {
                ViewData["Action"] = "Edit";
                ViewData["btnSubmit"] = "disabled";
                ViewData["Directory"] = _db.Directories.ToList();
                if (directory == null)
                {
                    return HttpNotFound();
                }
            }
            catch (Exception)
            {

            }
            return View("Directory", directory);
        }

        // Edit News Post
        [HttpPost]
        public ActionResult Edit(Models.Directory directory, FormCollection formCollection)
        {
            var text = formCollection["hidText"];
            if (ModelState.IsValid)
            {

                _db.spUpdateDirectory(directory.id, directory.Name, text, directory.CUGNo);
                _db.SaveChanges();

                //----------- Push Notification --------------------

                SendPushNotification();
                //-----------------------------------

                TempData["msg"] = "<script>alert('Updated succesfully');</script>";
                ModelState.Clear();
                ViewData["btnUpdate"] = "disabled";
                ViewData["btnSubmit"] = "enabled";
                return RedirectToAction("Directory");
            }

            return RedirectToAction("Directory");
        }

        [HttpPost]
        public ActionResult DeleteDirectory(int id)
        {
            Models.Directory directory = _db.Directories.Find(id);
            _db.Directories.Remove(directory);
            _db.SaveChanges();
            TempData["msg"] = "<script>alert('Deleted succesfully');</script>";
            return RedirectToAction("Directory");

        }


        public class VCard
        {
            public string Name { get; set; }
            public string Fname { get; set; }
            public string Lname { get; set; }
            public string Designation { get; set; }
            public string Cug { get; set; }

        }


        public ActionResult GenerateVCard()
        {
            Response.Clear();
            Response.ContentType = "text/vcard";
            const string vfilename = "MyContact.VCF";
            Response.AddHeader("content-disposition", "inline;filename=" + vfilename);
            string vcf = null;
            byte[] outputBytes = { };
            for (int i = 1; i <= _db.Directories.Count(); i++)
            {
                var v = _db.Directories.FirstOrDefault(a => a.id.Equals(i));
                if (v != null)
                {

                    string[] n = v.Name.Split(' ');
                    int len = n.Count();

                    string fName = null;
                    for (int j = 0; j < len - 1; j++)
                    {
                        fName = fName + n[j];
                    }

                    string lName = n[len - 1];
                    var vcMyCard = new VCard
                    {
                        Name = v.Name,
                        Fname = fName,
                        Lname = lName,
                        Designation = v.Designation,
                        Cug = v.CUGNo.Replace("-", ""),

                    };
                    var cardString = BuildVCard(vcMyCard);
                    vcf = vcf + cardString;
                    var inputEncoding = Encoding.Default;
                    var outputEncoding = Encoding.GetEncoding("windows-1257");
                    var cardBytes = inputEncoding.GetBytes(vcf);
                    outputBytes = Encoding.Convert(inputEncoding, outputEncoding, cardBytes);


                }



            }
            Response.OutputStream.Write(outputBytes, 0, outputBytes.Length);
            Response.End();
            return RedirectToAction("Directory");
        }


        public static string BuildVCard(VCard vCard)
        {
            // loading froma URL  

            // loading from file system  
            //File.ReadAllBytes(vCard.ImageLink);  
            var vCardBuilder = new StringBuilder();
            vCardBuilder.AppendLine("BEGIN:VCARD");
            vCardBuilder.AppendLine("VERSION:2.1");
            vCardBuilder.AppendLine("N:" + vCard.Lname + ";" + vCard.Fname);
            vCardBuilder.AppendLine("FN:" + vCard.Fname + " " + vCard.Lname);
            vCardBuilder.Append("ADR;HOME;PREF:;;");
            vCardBuilder.Append("" + ";");
            vCardBuilder.Append("" + ";;");
            vCardBuilder.AppendLine("India");
            vCardBuilder.AppendLine("ORG:" + "");
            vCardBuilder.AppendLine("TITLE:" + vCard.Designation);
            vCardBuilder.AppendLine("TEL;HOME;VOICE:" + "");
            vCardBuilder.AppendLine("TEL;CELL;VOICE:" + vCard.Cug);
            vCardBuilder.AppendLine("EMAIL;PREF;INTERNET:" + "");


            vCardBuilder.AppendLine(string.Empty);
            vCardBuilder.AppendLine(string.Empty);
            vCardBuilder.AppendLine(string.Empty);
            vCardBuilder.AppendLine("END:VCARD");
            return vCardBuilder.ToString();
        }

        private void AndroidPush(string reg)
        {
            // your RegistrationID paste here which is received from GCM server.                                                               
            string regId = "fevT3FBkoZI:APA91bFN54T2vZLsOghSxUtW3weTjJ_wNfyXwnW3w-F6U3qfkq3rfS0zkMQX7O8Lx9aQZdCX0F6PChjPU3BYHcTphsjU4RO5VXUgbvKaFcKK57RggpH6ODm3sQCMAROQuLiqfK0XYQU2";
            regId = reg;
            // applicationID means google Api key                                                                                                     
            var applicationID = "AIzaSyA2Wkdnp__rBokCmyloMFfENchJQb59tX8";
            // SENDER_ID is nothing but your ProjectID (from API Console- google code)//                                          
            var SENDER_ID = "925760665756";

            var value = "hello"; //message text box                                                                               

            WebRequest tRequest;

            tRequest = WebRequest.Create("https://android.googleapis.com/gcm/send");

            tRequest.Method = "post";

            tRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            tRequest.Headers.Add(string.Format("Authorization: key={0}", applicationID));

            tRequest.Headers.Add(string.Format("Sender: id={0}", SENDER_ID));

            //Data post to server                                                                                                                                         
            string postData =
                 "collapse_key=score_update&time_to_live=108&delay_while_idle=1&data.message="
                  + value + "&data.time=" + System.DateTime.Now.ToString() + "&registration_id=" +
                     regId + "";




            Byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            tRequest.ContentLength = byteArray.Length;

            Stream dataStream = tRequest.GetRequestStream();

            dataStream.Write(byteArray, 0, byteArray.Length);

            dataStream.Close();

            WebResponse tResponse = tRequest.GetResponse();

            dataStream = tResponse.GetResponseStream();

            StreamReader tReader = new StreamReader(dataStream);

            string sResponseFromServer = tReader.ReadToEnd();   //Get response from GCM server.

            tReader.Close();

            dataStream.Close();
            tResponse.Close();
        }

        public void SendPushNotification()
        {

            string stringregIds = null;
            List<string> regIDs = _db.Directories.Where(i => i.GCM != null && i.GCM != "").Select(i => i.GCM).ToList();
            //Here I add the registrationID that I used in Method #1 to regIDs
            stringregIds = string.Join("\",\"", regIDs);
            //To Join the values (if ever there are more than 1) with quotes and commas for the Json format below

            try
            {
                string GoogleAppID = "AIzaSyA2Wkdnp__rBokCmyloMFfENchJQb59tX8";
                var SENDER_ID = "925760665756";
                var value = "Hello";
                WebRequest tRequest;
                tRequest = WebRequest.Create("https://android.googleapis.com/gcm/send");
                tRequest.Method = "post";
                Request.ContentType = "application/json;charset=UTF-8";
                tRequest.Headers.Add(string.Format("Authorization: key={0}", GoogleAppID));
                //tRequest.Headers.Add(string.Format("Sender: id={0}", SENDER_ID));

                string postData = "{\"collapse_key\":\"score_update\",\"time_to_live\":108,\"delay_while_idle\":true,\"data\": { \"message\" : " + "\"" + value + "\",\"time\": " + "\"" + System.DateTime.Now.ToString() + "\"},\"registration_ids\":[\"" + stringregIds + "\"]}";

                Byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                tRequest.ContentLength = byteArray.Length;

                Stream dataStream = tRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse tResponse = tRequest.GetResponse();

                dataStream = tResponse.GetResponseStream();

                StreamReader tReader = new StreamReader(dataStream);

                string sResponseFromServer = tReader.ReadToEnd();
                TempData["msg1"] = "<script>alert('" + sResponseFromServer + "');</script>";
                HttpWebResponse httpResponse = (HttpWebResponse)tResponse;
                string statusCode = httpResponse.StatusCode.ToString();

                tReader.Close();
                dataStream.Close();
                tResponse.Close();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
