using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HOA.Model;

namespace HOA.Util
{
    public static class PageUtils
    {
        public static string GetStatusDisplay(Status status)
        {
            switch (status)
            {
                case Status.CommunityMgrReview:
                    return "Submitted";
                case Status.ARBChairReview:
                    return "ARB Chair Check";
                case Status.CommitteeReview:
                    return "Being reviewed";
                case Status.ARBTallyVotes:
                    return "Tallying votes";
                case Status.HOALiasonReview:
                    return "Sent to HOA Liaison";
                case Status.FinalResponse:
                    return "Preparing approval response";
                case Status.CommunityMgrReturn:
                    return "Returning to homeowner";
                case Status.HOALiasonInput:
                    return "HOA Liason input required";
                case Status.Rejected:
                    return "Rejected";
                case Status.MissingInformation:
                    return "Missing Information";
                case Status.Approved:
                    return "Approved";
                case Status.ConditionallyApproved:
                    return "Conditionally Approved";
                case Status.Retracted:
                    return "Retracted";
                default:
                    throw new Exception("Unknown status");
            }
        }
    }
}
