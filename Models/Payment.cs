using System;

namespace StudentEnrollmentSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int StudentProfileId { get; set; }
        public int EnrollmentId { get; set; }
        public int CourseId { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public PaymentMethod Method { get; set; } = PaymentMethod.Unknown;

        public string TransactionId { get; set; } = ""; // Stripe / PayPal 返回
        public string Provider { get; set; } = "";       // Stripe / PayPal

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public StudentProfile StudentProfile { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Succeeded,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        Unknown,
        Card,
        PayPal,
        ApplePay,
        GooglePay,
        Alipay,
        WeChatPay
    }
}