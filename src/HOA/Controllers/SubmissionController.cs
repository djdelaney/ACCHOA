using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using HOA.Model;
using HOA.Model.ViewModel;
using Microsoft.Data.Entity;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    public class SubmissionController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public SubmissionController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        // GET: /<controller>/
        public IActionResult List()
        {
            var history = _applicationDbContext.Histories.Include(h => h.Submission).FirstOrDefault();
            var sub = _applicationDbContext.Submissions.Include(s => s.Audits).FirstOrDefault();
                //return Content("New submission, view submission");

                return View(new ViewSubmissionsViewModel
            {
                Submissions = _applicationDbContext.Submissions.ToList()
            });
        }

        public ActionResult View(int? id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if(submission == null)
                return HttpNotFound("Submission not found");

            var model = new ViewSubmissionViewModel()
            {
                Submission = submission
            };

            return View(model);
        }

        public IActionResult Test()
        {
            var me = _applicationDbContext.Users.FirstOrDefault();
            var sub = new Submission()
            {
                FirstName = "Ali",
                LastName = "K",
                HouseNumber = 241,
                StreetName = "Sills Ln",
                Email = "Alison.Kolakowski@gmail.com",
                Description = "Hot tubbbb",
                Status = Status.Submitted
            };
            var history = new History
            {
                User = me,
                DateTime = DateTime.Now,
                Action = "To review",
                Submission = sub
            };
            sub.Audits = new List<History>();
            sub.Audits.Add(history);
            _applicationDbContext.Histories.Add(history);
            _applicationDbContext.Submissions.Add(sub);
            _applicationDbContext.SaveChanges();

            /*var me = _applicationDbContext.Users.FirstOrDefault();
            var sub = _applicationDbContext.Submissions.FirstOrDefault();

            var history = new History
            {
                User = me,
                DateTime = DateTime.Now,
                Action = "To review",
                Submission = sub
            };
            sub.Audit = new List<History>();
            sub.Audit.Add(history);
            sub.Status = Status.UnderReview;

            _applicationDbContext.SaveChanges();*/

            return Content("Created");
            //return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {

                var sub = new Submission()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    HouseNumber = model.HouseNumber,
                    StreetName = model.StreetName,
                    Email = model.Email,
                    Description = model.Description,
                    Status = Status.Submitted
                };

                _applicationDbContext.Submissions.Add(sub);
                _applicationDbContext.SaveChanges();
                
                Console.WriteLine("Create");
                return Content("Submission ID: " + sub.Id);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
}
