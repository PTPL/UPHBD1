using System.Linq;
using System.Text;
using System.Web.Mvc;
using UPHBD.Areas.Admin.Controllers;
using UPHBD.Models;

namespace UPHBD.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        readonly UPHBDContext _db = new UPHBDContext();


        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(OfficerLogin user)
        {

            if (ModelState.IsValid)
            {
                using (UPHBDContext dc = new UPHBDContext())
                {
                    var v = dc.OfficerLogins.FirstOrDefault(a => a.UserId.Equals(user.UserId) && a.Password.Equals(user.Password));
                    if (v != null)
                    {
                        Session["LogedUserID"] = v.UserId;
                        Session["userId"] = v.UserId;
                        return RedirectToAction("AfterLogin");
                    }
                }
            }
            return View(user);
        }


        public ActionResult AfterLogin()
        {
            if (Session["LogedUserID"] != null)
            {
                return RedirectToAction("Directory", "Admin", new { area = "Admin" });
            }
            // ReSharper disable once RedundantIfElseBlock
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session["LogedUserID"] = null;
            Session["userId"] = null;
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }



        public class VCard
        {
            public string Name { get; set; }
            public string Fname { get; set; }
            public string Lname { get; set; }
            public string Designation { get; set; }
            public string Cug { get; set; }

        }


        public ActionResult CheckCug(string cug)
        {

           

            var v = _db.Directories.FirstOrDefault(a => a.CUGNo.Replace("-","").Equals(cug));
            if (v != null)
            {
             
              return RedirectToAction("GenerateVCard");
            }

            TempData["msg"] = "<script>alert('CUG Number Not Found');</script>";
            return RedirectToAction("Login");
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
                    var vcMyCard = new AdminController.VCard
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
            return RedirectToAction("Login");
        }


        public static string BuildVCard(AdminController.VCard vCard)
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

    }
}
