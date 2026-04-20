using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.ViewModels
{
    public class PaymentHistoryVM
    {
        public DateTime? PaidAt { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public PaymentStatus Status { get; set; }
        public int Id { get; set; }
    }
}