using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.ViewModel
{
    public class ViewSubmissionsViewModel
    {
        public IList<Submission> Submissions { get; set; }
    }

    public class ViewSubmissionViewModel
    {
        public Submission Submission { get; set; }
    }
}
