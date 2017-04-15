using ARAFFinal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ARAFFinal.Controllers
{
    [Authorize(Roles = "Admin,Faculty")]
    public class StudentInfoController : Controller
    {
        private ADITStudentDataEntities db = new ADITStudentDataEntities();

        // Get: Student details
        [AllowAnonymous]
        [HttpPost]
        public ActionResult SearchByEnrollment()
        {
            string en = Request["SearchBox"].ToString();
            if (en.Length == 12 && System.Text.RegularExpressions.Regex.IsMatch(en, @"^\d+$"))
            {
                Student student = db.Students.Find(en);
                if (student == null)
                {
                    ViewBag.FileContent = "No Data Found!!";
                    return View("~/Views/logins/AdminPage.cshtml");
                }
                else
                {
                    var semesters = from e in db.Semesters.Where(i => i.StudentId == student.StudentId)
                                    orderby e.SemesterId
                                    select e;

                    var results = from e in db.Results.Where(i => i.StudentId == student.StudentId)
                                  orderby e.SemesterId,e.SubjectId
                                  select e;

                    // particular subject selection
                    var subjects = from e in db.Subjects where (from f in db.Results.Where(j => j.StudentId == student.StudentId) select f.SubjectId).Contains(e.SubjectId)
                                   orderby e.SemesterId, e.SubjectId
                                   select e;

                    Dictionary<string, string> subjects2 = new Dictionary<string, string> { { "NILL", "NILL" }};
                    foreach(var x in subjects)
                        subjects2.Add(x.SubjectId.ToString(), x.SubjectName.ToString());

                    //section current backlog,cgpa,cpi
                    string cpi="0.0", cgpa="0.0", backlog="0";
                    foreach(var x in semesters)
                    {
                        if(student.SemesterId==x.SemesterId)
                        {
                            cpi = x.Cpi.ToString();
                            cgpa = x.Cgpa.ToString();
                            backlog = x.Backlog.ToString();
                            break;
                        }
                    }

                    Department departments = db.Departments.Find(student.DepartmentId);
                    ViewBag.semesters = semesters;
                    ViewBag.students = student;
                    ViewBag.subjects = subjects2;
                    ViewBag.results = results;
                    ViewBag.cpi = cpi;
                    ViewBag.cgpa = cgpa;
                    ViewBag.backlog = backlog;
                    ViewBag.departments = departments;
                    ViewBag.cpiRank = GetOverallRank(student);
                    ViewBag.spiRank = GetCurrentRank(student);
                    return View("StudentInfo");
                }
            }
            else
            {
                ViewBag.FileContent = "** Please Enter Valid Number **";
                return View("~/Views/logins/AdminPage.cshtml");
            }
        }

        

        //GET: over all rank based on cpi
        public int GetOverallRank(Student student)
        {
            int counter = 0;
            string year = ((int.Parse(student.SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            var sortedListSemester = from e in db.Semesters.Where(i => i.Year == year && i.SemesterId == student.SemesterId)
                                     orderby e.Cpi descending
                                     select e;
            foreach (var x in sortedListSemester)
            {
                counter++;
                if (x.StudentId == student.StudentId)
                    break;
            }
            return counter;
        }

        //GET: current semester rank
        public int GetCurrentRank(Student student)
        {
            int counter = 0;
            string year = ((int.Parse(student.SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            var sortedListSemester = from e in db.Semesters.Where(i => i.Year == year && i.SemesterId == student.SemesterId)
                                     orderby e.Spi descending
                                     select e;
            foreach (var x in sortedListSemester)
            {
                counter++;
                if (x.StudentId == student.StudentId)
                    break;
            }
            return counter;

        }
    }
}