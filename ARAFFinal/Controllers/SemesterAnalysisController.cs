using ARAFFinal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ARAFFinal.Controllers
{
    public class SemesterAnalysisController : Controller
    {
        private ADITStudentDataEntities db = new ADITStudentDataEntities();
        private Dictionary<string, List<string>> topper = new Dictionary<string, List<string>> { { "topperExample", new List<string> { "Name", "Enrollment", "grade spi/cpi" } } };
                
        // GET: SemesterAnalysis
        public ActionResult Index()
        {
            return View();
        }

        
        // Generate data for semester analysis page
        // pass it into SemAnalysis view
        [HttpPost]
        [AllowAnonymous]
        public ActionResult fetch()
        {
            string DepartmentId = Request["Department"].ToString();
            string SemesterId = Request["Semester"].ToString();
            if((DepartmentId!=null&& SemesterId!=null)&&CheckNull(DepartmentId,SemesterId))
            {
                ViewBag.department =(db.Departments.Find(DepartmentId)).DepartmentName.ToString();
                ViewBag.semester = SemesterId;
                ViewBag.spiCount = GetSpiCount(DepartmentId,SemesterId);
                ViewBag.cpiCount = GetCpiCount(DepartmentId, SemesterId);
                ViewBag.topper = topper;
                ViewBag.grades = GetGradesData(DepartmentId, SemesterId);
                ViewBag.totalFailedResult = GetTFR(DepartmentId, SemesterId);
                ViewBag.departmentId = DepartmentId;
                return View("SemAnalysis");
            }
            else
            {
                ViewBag.ErrorMessage = "Oooops No Data Found!!";
                return View("Index");
            }
        }

        
        //GET: total number of student totalnumber of students ,failed, result
        //returns list[0]=number of students, list[1]=numberofstudent failed,list[2]=result
        private List<double> GetTFR(string DepartmentId, string SemesterId)
        {
            List<double> TFR = new List<double>();
            string year = ((int.Parse(SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            var totalStudent = from u in db.Semesters
                               let student = from stu in db.Students.Where(i => i.Year == year && i.SemesterId == SemesterId && i.DepartmentId == DepartmentId)
                                             select stu.StudentId
                               where student.Contains(u.StudentId) && u.Year == year && u.SemesterId == SemesterId
                               select u;
            int totalStudents = 0,failedStudents=0;
            foreach(var s in totalStudent)
            {
                if (s.Backlog != 0)
                    failedStudents++;
                totalStudents++;
            }
            TFR.Add(totalStudents);
            TFR.Add(failedStudents);
            TFR.Add(((totalStudents - failedStudents)/(totalStudents*1.0))*100.00);
            return TFR;
        }
        
        // GET: get grades data from inform of "subject_id" key and value list of subjectname, AA AB..... grades
        // this method returns data for second table in SemAnalysis.cshtml
        private Dictionary<string,List<string>> GetGradesData(string DepartmentId,string SemesterId) 
        {
            string year = ((int.Parse(SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            Dictionary<string, List<string>> subjectGrades = new Dictionary<string, List<string>> { { "subject_id", new List<string> {"subject_Name","AA_count", "AB_count", "BB_count", "BC_count","CC_count","CD_count","DD_count","FF_count","Total" } } };
            var gradeList = from student in db.Students
                            join result in db.Results on student.StudentId equals result.StudentId
                            where student.Year == year && student.SemesterId == SemesterId && student.DepartmentId == DepartmentId
                            && (from f in db.Subjects.Where(j => j.SemesterId == SemesterId) select f.SubjectId).Contains(result.SubjectId)
                            select new { Student = student, Result = result};

            foreach(var subject in gradeList)
            {
                if(subjectGrades.ContainsKey(subject.Result.SubjectId))
                {
                    int temp = 0;
                    switch(subject.Result.Grade)
                    {
                        case "AA":
                            temp=int.Parse(subjectGrades[subject.Result.SubjectId][1]) + 1;
                            subjectGrades[subject.Result.SubjectId][1] = temp.ToString();
                            break;
                        case "AB":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][2]) + 1;
                            subjectGrades[subject.Result.SubjectId][2] = temp.ToString();
                            break;
                        case "BB":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][3]) + 1;
                            subjectGrades[subject.Result.SubjectId][3] = temp.ToString();
                            break;
                        case "BC":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][4]) + 1;
                            subjectGrades[subject.Result.SubjectId][4] = temp.ToString();
                            break;
                        case "CC":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][5]) + 1;
                            subjectGrades[subject.Result.SubjectId][5] = temp.ToString();
                            break;
                        case "CD":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][6]) + 1;
                            subjectGrades[subject.Result.SubjectId][6] = temp.ToString();
                            break;
                        case "DD":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][7]) + 1;
                            subjectGrades[subject.Result.SubjectId][7] = temp.ToString();
                            break;
                        case "FF":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][8]) + 1;
                            subjectGrades[subject.Result.SubjectId][8] = temp.ToString();
                            break;
                    }
                    temp = int.Parse(subjectGrades[subject.Result.SubjectId][9]) + 1;
                    subjectGrades[subject.Result.SubjectId][9] = temp.ToString();
                }
                else
                {
                    subjectGrades.Add(subject.Result.SubjectId, new List<string> { (db.Subjects.Find(subject.Result.SubjectId)).SubjectName, "0", "0", "0", "0", "0", "0", "0", "0", "0" });
                    int temp = 0;
                    switch (subject.Result.Grade)
                    {
                        case "AA":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][1]) + 1;
                            subjectGrades[subject.Result.SubjectId][1] = temp.ToString();
                            break;
                        case "AB":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][2]) + 1;
                            subjectGrades[subject.Result.SubjectId][2] = temp.ToString();
                            break;
                        case "BB":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][3]) + 1;
                            subjectGrades[subject.Result.SubjectId][3] = temp.ToString();
                            break;
                        case "BC":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][4]) + 1;
                            subjectGrades[subject.Result.SubjectId][4] = temp.ToString();
                            break;
                        case "CC":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][5]) + 1;
                            subjectGrades[subject.Result.SubjectId][5] = temp.ToString();
                            break;
                        case "CD":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][6]) + 1;
                            subjectGrades[subject.Result.SubjectId][6] = temp.ToString();
                            break;
                        case "DD":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][7]) + 1;
                            subjectGrades[subject.Result.SubjectId][7] = temp.ToString();
                            break;
                        case "FF":
                            temp = int.Parse(subjectGrades[subject.Result.SubjectId][8]) + 1;
                            subjectGrades[subject.Result.SubjectId][8] = temp.ToString();
                            break;
                    }
                    temp = int.Parse(subjectGrades[subject.Result.SubjectId][9]) + 1;
                    subjectGrades[subject.Result.SubjectId][9] = temp.ToString();
                } 
            }
            return subjectGrades;
        }

        // GET: data from database and check whether data is available or not for current semester
        // to provide particular output this method is used
        private bool CheckNull(string DepartmentId,string SemesterId)
        {
            string year = ((int.Parse(SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            var x = from e in db.Students.Where(i => i.SemesterId == SemesterId && i.DepartmentId == DepartmentId && i.Year==year)
                    select e;
            if (x != null)
                return true;
            else
                return false;
        }

        // GET: List of SPI >8 (between 8-7) (between 7-6) (between 6-5) below 5 based on spi
        //above8  between78 between67 between56 below5    <- spi key values
        private Dictionary<string,int> GetSpiCount(string DepartmentId, string SemesterId)
        {
            // above8  between78 between67 between56 below5    <- spi
            string year = ((int.Parse(SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            Dictionary<string, int> spiCount = new Dictionary<string, int> { { "above8", 0 }, { "between78", 0 }, { "between67", 0 }, { "between56", 0 }, { "below5", 0 }, { "topper", 0 } };
            var studentList = from student in db.Students
                              join semester in db.Semesters on student.StudentId equals semester.StudentId
                              where semester.Year == year && semester.SemesterId == student.SemesterId && semester.SemesterId == SemesterId && student.DepartmentId == DepartmentId
                              select new { Student = student, Semester = semester };
            string topperName = null;
            string topperEnroll = null;
            double highestSpi = 0.00;
            List<string> addValues = new List<string>();
            foreach(var studentInfo in studentList)
            {
                if (studentInfo.Semester.Spi >= 8.00)
                    spiCount["above8"]++;
                else if (studentInfo.Semester.Spi < 8.00 && studentInfo.Semester.Spi >= 7.00)
                    spiCount["between78"]++;
                else if (studentInfo.Semester.Spi < 7.00 && studentInfo.Semester.Spi >= 6.00)
                    spiCount["between67"]++;
                else if (studentInfo.Semester.Spi < 6.00 && studentInfo.Semester.Spi >= 5.00)
                    spiCount["between56"]++;
                else
                    spiCount["below5"]++;

                if(studentInfo.Semester.Spi>highestSpi)
                {
                    topperName = studentInfo.Student.StudentName;
                    topperEnroll = studentInfo.Student.StudentId;
                    highestSpi = studentInfo.Semester.Spi;
                }
            }
            addValues.Add(topperName);
            addValues.Add(topperEnroll);
            addValues.Add(highestSpi.ToString());
            topper.Add("topperSpi",addValues);
            return spiCount;
        }

        // GET: List of SPI >8 (between 8-7) (between 7-6) (between 6-5) below 5 based on spi
        //above8  between78 between67 between56 below5    <- cpi key values
        private Dictionary<string, int> GetCpiCount(string DepartmentId, string SemesterId)
        {
            string year = ((int.Parse(SemesterId) % 2 == 0) ? DateTime.Now.Year.ToString() : (int.Parse(DateTime.Now.Year.ToString()) - 1).ToString());
            Dictionary<string, int> cpiCount = new Dictionary<string, int> { { "above8", 0 }, { "between78", 0 }, { "between67", 0 }, { "between56", 0 }, { "below5", 0 } };
            var studentList = from student in db.Students
                              join semester in db.Semesters on student.StudentId equals semester.StudentId
                              where semester.Year == year && semester.SemesterId == student.SemesterId && semester.SemesterId == SemesterId && student.DepartmentId == DepartmentId
                              select new { Student = student, Semester = semester };
            string topperName = null;
            string topperEnroll = null;
            double highestCpi = 0.00;
            List<string> addValues = new List<string>();
            foreach (var studentInfo in studentList)
            {
                if (studentInfo.Semester.Cpi >= 8.00)
                    cpiCount["above8"]++;
                else if (studentInfo.Semester.Cpi < 8.00 && studentInfo.Semester.Cpi >= 7.00)
                    cpiCount["between78"]++;
                else if (studentInfo.Semester.Cpi < 7.00 && studentInfo.Semester.Cpi >= 6.00)
                    cpiCount["between67"]++;
                else if (studentInfo.Semester.Cpi < 6.00 && studentInfo.Semester.Cpi >= 5.00)
                    cpiCount["between56"]++;
                else
                    cpiCount["below5"]++;
                if (studentInfo.Semester.Cpi > highestCpi)
                {
                    topperName = studentInfo.Student.StudentName;
                    topperEnroll = studentInfo.Student.StudentId;
                    highestCpi = studentInfo.Semester.Cpi;
                }
            }
            addValues.Add(topperName);
            addValues.Add(topperEnroll);
            addValues.Add(highestCpi.ToString());
            topper.Add("topperCpi", addValues);
            return cpiCount;
        }
    }
}
 