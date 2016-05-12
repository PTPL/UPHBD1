using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.UI.WebControls;
using UPHBD.Models;

namespace UPHBD
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        private readonly UPHBDContext _db = new UPHBDContext();

        public Models.Directory LoginAuthentication(Models.Directory dir)
        {
            Models.Directory objLogin = new Models.Directory();

            var v = _db.Directories.FirstOrDefault(a => a.CUGNo.Replace("-", "").Equals(dir.CUGNo));
            if (v != null)
            {
                objLogin = v;

                _db.spUpdateGCM(dir.GCM, dir.IEMEI, dir.CUGNo);
                int saveChanges = _db.SaveChanges();
            }

            else
            {
                objLogin.CUGNo = "notok";
            }
            return objLogin;
        }

        public List<Models.Directory> AllContactsList()
        {
            return _db.Directories.ToList();
        }
    }
}
