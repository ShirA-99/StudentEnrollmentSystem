using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Data;
using StudentEnrollmentSystem.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using StudentEnrollmentSystem.ViewModels;

namespace StudentEnrollmentSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get current logged-in Identity UserId (string)
        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // 🔥 Convert IdentityUserId → StudentProfileId (int)
        private async Task<int> GetStudentProfileIdAsync()
        {
            var userId = GetUserId();

            return await _context.StudentProfiles
                .Where(s => s.ApplicationUserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
        }

        // 💳 Payment Page
        public async Task<IActionResult> Index()
        {
            var studentProfileId = await GetStudentProfileIdAsync();

            // ✅ 先拿 student + enrollment（跟 Statement 一样）
            var student = await _context.StudentProfiles
                .Include(s => s.CurrentSemester)
                .Include(s => s.EnrollmentRecords)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.Id == studentProfileId);

            if (student == null)
            {
                return NotFound();
            }

            var currentSemesterId = student.CurrentSemesterId;

            // ✅ 已付款记录
            var paidEnrollmentIds = await _context.Payments
                .Where(p => p.StudentProfileId == studentProfileId &&
                            p.Status == PaymentStatus.Succeeded)
                .Select(p => p.EnrollmentId)
                .ToListAsync();

            // ✅ 跟 Statement 一模一样的 filtering
            var records = student.EnrollmentRecords
                .Where(e =>
                    e.Status == EnrollmentStatus.Enrolled &&
                    e.CourseSection != null &&
                    e.CourseSection.Course != null &&
                    e.CourseSection.SemesterId == currentSemesterId &&
                    !paidEnrollmentIds.Contains(e.Id) 
                )
                .Select(e => new
                {
                    EnrollmentId = e.Id,
                    Code = e.CourseSection.Course.Code,
                    Title = e.CourseSection.Course.Title,
                    Fee = e.CourseSection.Course.Fee
                })
                .ToList();

            var totalFee = records.Sum(x => x.Fee);

            ViewBag.Courses = records;
            ViewBag.TotalFee = totalFee;
            ViewBag.Student = student;
            ViewBag.ActiveSemester = student.CurrentSemester?.Name;

            return View();
        }

        // 💰 Handle Payment
        [HttpPost]
        public async Task<IActionResult> Pay(List<int> enrollmentIds, string method)
        {
            var studentProfileId = await GetStudentProfileIdAsync();
            var cardNumber = Request.Form["cardNumber"].ToString();
            var expiry = Request.Form["expiry"].ToString();
            var cvv = Request.Form["cvv"].ToString();
            var cardHolder = Request.Form["cardHolder"].ToString();
            var bank = Request.Form["bank"].ToString();
            var tngPhone = Request.Form["tngPhone"].ToString();

            if (enrollmentIds == null || !enrollmentIds.Any())
            {
                TempData["Error"] = "No course selected!";
                return RedirectToAction("Index");
            }

            if (method == "Card")
            {
                if (string.IsNullOrEmpty(cardNumber) ||
                    string.IsNullOrEmpty(expiry) ||
                    string.IsNullOrEmpty(cvv) ||
                    string.IsNullOrEmpty(cardHolder))
                {
                    TempData["Error"] = "Card details are required!";
                    return RedirectToAction("Index");
                }
            }

            if (method == "OnlineBanking")
            {
                if (string.IsNullOrEmpty(bank))
                {
                    TempData["Error"] = "Bank selection required!";
                    return RedirectToAction("Index");
                }
            }

            if (method == "TNG")
            {
                if (string.IsNullOrEmpty(tngPhone))
                {
                    TempData["Error"] = "TNG phone number required!";
                    return RedirectToAction("Index");
                }
            }

            var validEnrollments = await _context.EnrollmentRecords
                .Where(e => enrollmentIds.Contains(e.Id) && e.StudentProfileId == studentProfileId)
                .Join(_context.CourseSections,
                    e => e.CourseSectionId,
                    cs => cs.Id,
                    (e, cs) => new { e.Id, cs.CourseId })
                .Join(_context.Courses,
                    x => x.CourseId,
                    c => c.Id,
                    (x, c) => new
                    {
                        EnrollmentId = x.Id,
                        CourseId = x.CourseId,
                        c.Fee
                    })
                .ToListAsync();

            if (!validEnrollments.Any())
            {
                TempData["Error"] = "Invalid course selection!";
                return RedirectToAction("Index");
            }

            // ✅ 2. 后端重新计算金额（关键）
            var totalAmount = validEnrollments.Sum(x => x.Fee);

            // ✅ 3. 防重复支付
            var alreadyPaidIds = await _context.Payments
                .Where(p => enrollmentIds.Contains(p.EnrollmentId)
                            && p.StudentProfileId == studentProfileId
                            && p.Status == PaymentStatus.Succeeded)
                .Select(p => p.EnrollmentId)
                .ToListAsync();

            var finalEnrollments = validEnrollments
                .Where(x => !alreadyPaidIds.Contains(x.EnrollmentId))
                .ToList();

            if (!finalEnrollments.Any())
            {
                TempData["Error"] = "Courses already paid!";
                return RedirectToAction("Index");
            }

            // ✅ 4. 创建 Payment（每个 enrollment 一条）
            foreach (var item in finalEnrollments)
            {
                var payment = new Payment
                {
                    StudentProfileId = studentProfileId,
                    EnrollmentId = item.EnrollmentId,
                    CourseId = item.CourseId,

                    Amount = item.Fee, // ✅ 每个真实金额
                    Currency = "MYR",

                    Status = PaymentStatus.Succeeded,
                    Method = Enum.TryParse<PaymentMethod>(method, out var m) ? m : PaymentMethod.Unknown,

                    TransactionId = Guid.NewGuid().ToString(), // 模拟
                    Provider = "Manual",

                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payment Successful! Total: RM {totalAmount}";
            return RedirectToAction("Index");
        }

        // 📜 Payment History
        public async Task<IActionResult> History()
        {
            var studentProfileId = await GetStudentProfileIdAsync();

            var payments = await _context.Payments
                .Where(p => p.StudentProfileId == studentProfileId)
                .Join(_context.Courses,
                    p => p.CourseId,
                    c => c.Id,
                    (p, c) => new PaymentHistoryVM
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        Status = p.Status,
                        Method = p.Method.ToString(),
                        PaidAt = p.PaidAt,
                        CourseCode = c.Code,
                        CourseTitle = c.Title
                    })
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            return View(payments);
        }
    }
}