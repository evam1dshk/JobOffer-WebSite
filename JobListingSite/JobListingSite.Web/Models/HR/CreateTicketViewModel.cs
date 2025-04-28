namespace JobListingSite.Web.Models.HR
{
    public class CreateTicketViewModel
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public JobListingSite.Data.Enums.TicketPriority Priority { get; set; }
    }
}
