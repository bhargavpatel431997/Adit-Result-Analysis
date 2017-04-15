using ARAFFinal.Models;
using LumenWorks.Framework.IO.Csv;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml.Linq;

namespace ARAFFinal.Controllers
{
    public class LoginsController : Controller
    {


        private ADITStudentDataEntities db = new ADITStudentDataEntities();
        
        // GET: Logins
        public ActionResult LoginPage()
        {
            return View();
        }
        
        // authentication process is being done here.
        // this method generates client side cookie session for 2 hours.
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Authorize (Login model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = db.Logins
                 .FirstOrDefault(u => u.Username == model.Username
                              && u.Password == model.Password);
            if (user != null)
            {
                FormsAuthentication.SetAuthCookie(model.Username, false);

                var authTicket = new FormsAuthenticationTicket(1, user.Username, DateTime.Now, DateTime.Now.AddMinutes(120), false, user.Role);
                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                HttpContext.Response.Cookies.Add(authCookie);
                // if admin then redirect to action Login model.
                if (user.Role == "Admin")
                    return View("../Logins/AdminPage");
                else
                {
                    //userDeptId = user.DepartmentId;
                    ViewBag.DID = user.DepartmentId;
                    return View("../Logins/FacultyPage");
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid Username/Password";
                return View("LoginPage");
            }
        }

        //to process csv file and preview the data
        // data can be manipulated on to preview page.
        [Authorize(Roles ="Faculty,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadFile()
        {
            
            var count = Request.Files.Count;
            if (count > 0)
            {
                var files = Request.Files[0];
                if (files.ContentLength > 0)
                {
                    if (files.FileName.EndsWith(".csv"))
                    {
                        Stream stream = files.InputStream;
                        DataTable csvTable = new DataTable();
                        csvTable.Columns.Add("Enrollment", typeof(string));
                        csvTable.Columns.Add("Student Name", typeof(string));
                        csvTable.Columns.Add("Department id", typeof(string));
                        csvTable.Columns.Add("Current Sem", typeof(string));
                        csvTable.Columns.Add("Current Year", typeof(string));

                        using (CsvReader csvReader = new CsvReader(new StreamReader(stream), true))
                        {
                            while (csvReader.ReadNextRecord())
                            {
                                DataRow fields = csvTable.NewRow();
                                for (int i=0;i<csvReader.FieldCount;i++)
                                {
                                    if (i == 0 && csvReader[i].Length != 12)
                                        fields[i]=Regex.Match(csvReader[i], @"(\d{12})").Value;
                                    else
                                        fields[i]=csvReader[i];
                                }
                                csvTable.Rows.Add(fields);
                            }
                        }                     
                        return View("Preview", csvTable);
                    }
                    else
                    {
                        ModelState.AddModelError("File", "This file format is not supported");
                        return View("FacultyPage");
                    }
                }
                else
                    ModelState.AddModelError("File", "Please Upload Your file");
            }
            ViewBag.message = "file not uploaded!";
            return View("FacultyPage");
        }


        //inserts or updates students data into database.
        //also returns message how many rows were affected.
        [Authorize(Roles = "Faculty,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update()
        {
            List<string> oneStudentData = new List<string>();
            int count = 0, updated = 0, inserted = 0 ;
            for( int i=1; count<Request.Form.AllKeys.Length-1;i++)
            {
                if (Request.Form.AllKeys.Contains(string.Format("nm{0}1", i)))
                {
                    oneStudentData.Clear();
                    for (int j = 1; j <= 5; j++)
                    {
                        oneStudentData.Add((Request.Form[string.Format("nm{0}{1}", i, j)]).ToString());
                        count++;
                    }
                    string en_no = oneStudentData[0];
                    var check = db.Students.Any(x => x.StudentId == en_no);
                    if (check)
                    {
                        // update query
                        Student s = (from x in db.Students
                                     where x.StudentId == en_no
                                     select x).First();
                        s.StudentName = oneStudentData[1];
                        s.DepartmentId = oneStudentData[2];
                        s.SemesterId = oneStudentData[3];
                        s.Year = oneStudentData[4];
                        db.SaveChanges();
                        updated++;
                    }
                    else
                    {
                        // insert query
                        Student student = new Student();
                        student.StudentId = oneStudentData[0];
                        student.StudentName = oneStudentData[1];
                        student.DepartmentId = oneStudentData[2];
                        student.SemesterId = oneStudentData[3];
                        student.Year = oneStudentData[4];
                        db.Students.Add(student);
                        db.SaveChanges();
                        inserted++;
                    }
                    
                }
                
            }
            ViewBag.message = String.Format("total students:{0}, updated:{1}, inserted:{2}", updated+inserted, updated, inserted);
            return View("FacultyPage");
        }


        //To result entry semester wise
        // this method returns view to change the data of student
        [Authorize(Roles = "Faculty,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResultEntry()
        {
            //1. take department of faculty get current year student
            //2. read semester value from request
            //3. fetch data and pass the view
            string semesterId = Request["Semester"].ToString();

            string userId = User.Identity.GetUserName().ToString();
            string deptId = (db.Logins.First(i => i.Username == userId)).DepartmentId;

            string year = (((int.Parse(semesterId)-1) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            
            ViewBag.semesterId = semesterId;
            ViewBag.subjectList = GetSubjectList(semesterId, deptId); // subjectid+name format
            ViewBag.enrollmentsAndNames=GetEnrollmentNumbers(semesterId, deptId, year);//first is enrollment, second is name
            return View("ResultEntry");
        }



        // To update result entery students to do analysis.
        // This method updates student information into database.
        [Authorize(Roles = "Faculty,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResultUpdate()
        {
            string semesterId = Request["semesterId"].ToString();
            string userId = User.Identity.GetUserName().ToString();
            string deptId = (db.Logins.First(i => i.Username == userId)).DepartmentId;
            string year = (((int.Parse(semesterId)) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            List<int> existingSubjectIndexs = new List<int>();
            List<int> existingStudentIndexs = new List<int>();

            // to get subject indexs & student index list which exist in posted form.
            for (int i = 0; i < Request.Form.AllKeys.Length; i++)
            {
                if (Request.Form.AllKeys.Contains(string.Format("subject{0}", i)))
                    existingSubjectIndexs.Add(i);
                if (Request.Form.AllKeys.Contains(string.Format("text{0}0", i)))
                    existingStudentIndexs.Add(i);
            }

            // insert data into database
            
            foreach (int i in existingStudentIndexs)
            {
                // update student query
                string check = Request[string.Format("text{0}0", i)].ToString();
                Student s = (from x in db.Students
                             where x.StudentId == check
                             select x).First();
                s.SemesterId = semesterId.ToString();
                s.Year = year;
                db.SaveChanges();

                //insert semester query
                Semester sem = new Semester();
                sem.StudentId = Request[string.Format("text{0}0", i)].ToString().Trim();
                sem.SemesterId = semesterId.ToString();
                sem.Spi = float.Parse(Request[string.Format("text{0}1", i)].ToString().Trim());
                sem.Cpi = float.Parse(Request[string.Format("text{0}2", i)].ToString().Trim());
                sem.Cgpa = float.Parse(Request[string.Format("text{0}3", i)].ToString().Trim());
                sem.Backlog = int.Parse(Request[string.Format("text{0}4", i)].ToString().Trim());
                sem.Year = year;
                db.Semesters.Add(sem);
                db.SaveChanges();
                

                foreach (int j in existingSubjectIndexs)
                {
                    // insert result query
                    if(Request[string.Format("select{0}{1}", i, j)].ToString().Trim()!="NO")
                    {
                        Result rs = new Result();
                        rs.StudentId = Request[string.Format("text{0}0", i)].ToString().Trim();
                        rs.Grade = Request[string.Format("select{0}{1}", i, j)].ToString().Trim();
                        rs.SubjectId = Request[string.Format("subject{0}", j)].ToString().Trim();
                        rs.SemesterId = semesterId;
                        db.Results.Add(rs);
                        db.SaveChanges();
                    }
                }
            }

            return View();
        }

        //GET: subject enrollment numbers list of current sem
        // it will return list object of enrollment numbers and names
        [Authorize(Roles = "Faculty,Admin")]
        private List<Tuple<string,string>> GetEnrollmentNumbers(string semesterId, string deptId, string year)
        {
            List<Tuple<string, string>> enrollmentNos = new List<Tuple<string, string>>();
            semesterId = (int.Parse(semesterId) - 1).ToString();
            var students = from e in db.Students.Where(i => i.SemesterId == semesterId && i.DepartmentId == deptId && i.Year==year)
                           orderby e.StudentId
                           select e;
            foreach (var x in students)
                enrollmentNos.Add(Tuple.Create(x.StudentId,x.StudentName));
            return enrollmentNos;
        }

        //GET: subject List from database
        // it will return list object of subjects
        [Authorize(Roles = "Faculty,Admin")]
        private List<string> GetSubjectList(string semesterId, string deptId)
        {
            List<string> subjectList = new List<string>();
            var subjects = from e in db.Subjects.Where(i => i.SemesterId == semesterId && i.DepartmentId == deptId)
                    orderby e.SubjectId select e;
            foreach(var x in subjects)
                subjectList.Add(x.SubjectId +" ("+ x.SubjectName+")");
            return subjectList;
        }

        [Authorize(Roles="Admin")]
        public ActionResult AdminPage()
        {   
            return View("AdminPage");
        }

        [Authorize(Roles = "Faculty,Admin")]
        public ActionResult FacultyPage()
        {
            return View("FacultyPage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            //AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

    }
}