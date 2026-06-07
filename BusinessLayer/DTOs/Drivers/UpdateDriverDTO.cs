using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Drivers
{
    public class UpdateDriverDTO
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, ErrorMessage = "الرقم القومي لازم يكون 14 رقم")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي لازم يكون 14 رقم")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب إضافة رقم هاتف واحد على الأقل")]
        [ListRegularExpression(@"^01[0-2,5]{1}[0-9]{8}$", ErrorMessage = "يوجد رقم هاتف غير صحيح")]
        public List<string> PhoneNumbers { get; set; } = new();
    }
}