﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public enum StatusV1
    {
        Submitted = 0,
        ARBIncoming = 1,
        UnderReview = 2,
        ARBFinal = 3,
        ReviewComplete = 4,
        PrepApproval = 5,
        PrepConditionalApproval = 6,
        Rejected = 7,
        MissingInformation = 8,
        Approved = 9,
        ConditionallyApproved = 10,
        Retracted = 11
    }

    public class SubmissionV1
    {
        public string Code { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Address { get; set; }

        public string Email { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public int Revision { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime StatusChangeTime { get; set; }

        public DateTime SubmissionDate { get; set; }

        public bool PrecedentSetting { get; set; }

        public virtual List<ReviewV1> Reviews { get; set; }

        public virtual List<HistoryV1> Audits { get; set; }

        public virtual List<ResponseV1> Responses { get; set; }

        public virtual List<FileV1> Files { get; set; }

        public virtual List<StateChangeV1> StateHistory { get; set; }

        public virtual List<CommentV1> Comments { get; set; }

        public string FinalApprovalBlob { get; set; }

        public string FinalApprovalFileName { get; set; }
    }
}
