using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HOA.Model;

namespace HOA.Export.Services;

public class PdfGenerator
{
    public PdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateSubmissionPdf(Submission submission, string outputPath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(0.75f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, submission));
                page.Content().Element(c => ComposeContent(c, submission));
                page.Footer().Element(ComposeFooter);
            });
        })
        .GeneratePdf(outputPath);
    }

    private void ComposeHeader(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("HOA Submission Report").Bold().FontSize(18);
            column.Item().Text($"Code: {submission.Code}").FontSize(12).FontColor(Colors.Grey.Darken2);
            column.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeContent(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            // Submission Details Section
            column.Item().Element(c => ComposeSubmissionDetails(c, submission));

            // Flags Section (if any)
            if (submission.LandscapingRelated || submission.PrecedentSetting)
            {
                column.Item().Element(c => ComposeFlags(c, submission));
            }

            // Description Section
            column.Item().Element(c => ComposeDescription(c, submission));

            // Files Section
            if (submission.Files?.Any() == true)
            {
                column.Item().Element(c => ComposeFiles(c, submission));
            }

            // Response Document (if exists)
            if (!string.IsNullOrEmpty(submission.ResponseDocumentFileName))
            {
                column.Item().Element(c => ComposeResponseDocument(c, submission));
            }

            // Reviews Section (current revision)
            var currentReviews = submission.Reviews?
                .Where(r => r.SubmissionRevision == submission.Revision)
                .OrderByDescending(r => r.Created)
                .ToList();
            if (currentReviews?.Any() == true)
            {
                column.Item().Element(c => ComposeReviews(c, currentReviews, "Reviews (Current Revision)"));
            }

            // Old Reviews Section
            var oldReviews = submission.Reviews?
                .Where(r => r.SubmissionRevision != submission.Revision)
                .OrderByDescending(r => r.Created)
                .ToList();
            if (oldReviews?.Any() == true)
            {
                column.Item().Element(c => ComposeReviews(c, oldReviews, "Reviews (Previous Revisions)"));
            }

            // Internal Comments Section
            if (submission.Comments?.Any() == true)
            {
                column.Item().Element(c => ComposeComments(c, submission));
            }

            // Responses Section
            if (submission.Responses?.Any() == true)
            {
                column.Item().Element(c => ComposeResponses(c, submission));
            }

            // Audit History Section
            if (submission.Audits?.Any() == true)
            {
                column.Item().Element(c => ComposeAuditHistory(c, submission));
            }
        });
    }

    private void ComposeSubmissionDetails(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Submission Details").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                });

                AddTableRow(table, "Name:", $"{submission.FirstName} {submission.LastName}");
                AddTableRow(table, "Email:", submission.Email);
                AddTableRow(table, "Address:", submission.Address);
                AddTableRow(table, "Status:", FormatStatus(submission.Status));
                AddTableRow(table, "Submission Date:", FormatDate(submission.SubmissionDate));
                AddTableRow(table, "Last Modified:", FormatDate(submission.LastModified));
                if (submission.Revision > 1)
                {
                    AddTableRow(table, "Revision:", submission.Revision.ToString());
                }
            });
        });
    }

    private void ComposeFlags(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Flags").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(flagsColumn =>
            {
                if (submission.LandscapingRelated)
                {
                    flagsColumn.Item().Text("• Landscaping Related").FontColor(Colors.Blue.Darken2);
                }
                if (submission.PrecedentSetting)
                {
                    flagsColumn.Item().Text("• Precedent Setting").FontColor(Colors.Orange.Darken2);
                }
            });
        });
    }

    private void ComposeDescription(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Description").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Text(submission.Description ?? "(No description provided)");
        });
    }

    private void ComposeFiles(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Attached Files").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(filesColumn =>
            {
                foreach (var file in submission.Files!)
                {
                    filesColumn.Item().Text($"• {file.Name}");
                }
            });
        });
    }

    private void ComposeResponseDocument(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Response Document").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Text($"• {submission.ResponseDocumentFileName}");
        });
    }

    private void ComposeReviews(IContainer container, List<Review> reviews, string title)
    {
        container.Column(column =>
        {
            column.Item().Text(title).Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(reviewsColumn =>
            {
                foreach (var review in reviews)
                {
                    reviewsColumn.Item().PaddingBottom(10).Column(reviewColumn =>
                    {
                        var reviewerName = review.Reviewer?.FullName ?? "Unknown";
                        reviewColumn.Item().Text($"{reviewerName} - {FormatDate(review.Created)}").Bold();
                        reviewColumn.Item().Text($"Vote: {FormatReviewStatus(review.Status)}").FontColor(GetReviewStatusColor(review.Status));
                        if (!string.IsNullOrEmpty(review.Comments))
                        {
                            reviewColumn.Item().PaddingTop(3).Text(review.Comments).FontColor(Colors.Grey.Darken1);
                        }
                    });
                }
            });
        });
    }

    private void ComposeComments(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Internal Comments").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(commentsColumn =>
            {
                foreach (var comment in submission.Comments!.OrderByDescending(c => c.Created))
                {
                    commentsColumn.Item().PaddingBottom(10).Column(commentColumn =>
                    {
                        var userName = comment.User?.FullName ?? "Unknown";
                        commentColumn.Item().Text($"{userName} - {FormatDateTime(comment.Created)}").Bold();
                        commentColumn.Item().PaddingTop(3).Text(comment.Comments ?? "");
                    });
                }
            });
        });
    }

    private void ComposeResponses(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Responses to Applicant").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(responsesColumn =>
            {
                foreach (var response in submission.Responses!.OrderByDescending(r => r.Created))
                {
                    responsesColumn.Item().PaddingBottom(10).Column(responseColumn =>
                    {
                        responseColumn.Item().Text($"{FormatDateTime(response.Created)}").Bold();
                        responseColumn.Item().PaddingTop(3).Text(response.Comments ?? "");
                    });
                }
            });
        });
    }

    private void ComposeAuditHistory(IContainer container, Submission submission)
    {
        container.Column(column =>
        {
            column.Item().Text("Audit History").Bold().FontSize(14);
            column.Item().PaddingTop(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(8).Column(historyColumn =>
            {
                foreach (var audit in submission.Audits!.OrderByDescending(a => a.DateTime))
                {
                    historyColumn.Item().PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Text($"{audit.User} - {FormatDateTime(audit.DateTime)}");
                        row.ConstantItem(200).AlignRight().Text(audit.Action).FontColor(Colors.Grey.Darken1);
                    });
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Generated: {DateTime.Now:MM/dd/yyyy hh:mm tt}").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private static void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Text(label).Bold();
        table.Cell().Text(value ?? "");
    }

    private static string FormatDate(DateTime dt)
    {
        return ConvertToEastern(dt).ToString("MM/dd/yyyy");
    }

    private static string FormatDateTime(DateTime dt)
    {
        return ConvertToEastern(dt).ToString("MM/dd/yyyy hh:mm tt");
    }

    private static DateTime ConvertToEastern(DateTime dt)
    {
        try
        {
            // Try Windows timezone ID first
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(dt, easternZone);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Fall back to IANA timezone ID for macOS/Linux
                var easternZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
                return TimeZoneInfo.ConvertTimeFromUtc(dt, easternZone);
            }
            catch
            {
                // If all else fails, return the original datetime
                return dt;
            }
        }
    }

    private static string FormatStatus(Status status)
    {
        return status switch
        {
            Status.CommunityMgrReview => "Community Manager Review",
            Status.ARBChairReview => "ARB Chair Review",
            Status.CommitteeReview => "Committee Review",
            Status.ARBTallyVotes => "Tallying Votes",
            Status.HOALiasonReview => "HOA Liaison Review",
            Status.FinalResponse => "Final Response",
            Status.CommunityMgrReturn => "Returned to Community Manager",
            Status.HOALiasonInput => "HOA Liaison Input",
            Status.Rejected => "Rejected",
            Status.MissingInformation => "Missing Information",
            Status.Approved => "Approved",
            Status.ConditionallyApproved => "Conditionally Approved",
            Status.Retracted => "Retracted",
            _ => status.ToString()
        };
    }

    private static string FormatReviewStatus(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => "Approved",
            ReviewStatus.Rejected => "Rejected",
            ReviewStatus.ConditionallyApproved => "Conditionally Approved",
            ReviewStatus.MissingInformation => "Missing Information",
            ReviewStatus.Abstain => "Abstain",
            _ => status.ToString()
        };
    }

    private static string GetReviewStatusColor(ReviewStatus status)
    {
        return status switch
        {
            ReviewStatus.Approved => Colors.Green.Darken2,
            ReviewStatus.Rejected => Colors.Red.Darken2,
            ReviewStatus.ConditionallyApproved => Colors.Orange.Darken2,
            ReviewStatus.MissingInformation => Colors.Yellow.Darken3,
            ReviewStatus.Abstain => Colors.Grey.Darken1,
            _ => Colors.Black
        };
    }
}
