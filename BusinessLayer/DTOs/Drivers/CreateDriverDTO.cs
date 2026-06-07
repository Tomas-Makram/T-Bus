using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BusinessLayer.DTOs.Drivers
{
    public class CreateDriverDTO
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

    public class ListRegularExpressionAttribute : ValidationAttribute
    {
        private readonly Regex _regex;

        public ListRegularExpressionAttribute(string pattern)
        {
            _regex = new Regex(pattern);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IEnumerable<string> list)
            {
                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item) || !_regex.IsMatch(item))
                    {
                        return new ValidationResult(ErrorMessage);
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}