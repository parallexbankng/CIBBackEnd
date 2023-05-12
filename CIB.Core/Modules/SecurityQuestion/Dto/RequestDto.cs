
using CIB.Core.Common;

namespace CIB.Core.Modules.SecurityQuestion.Dto
{
    public class SetSecurityQuestionDto : BaseDto
    {
        public string UserName { get; set; }
        public string CustomerId { get; set; }
        public string Password { get; set; }
        public string SecurityQuestionId { get; set; }
        public string Answer { get; set; }
        public string SecurityQuestionId2 { get; set; }
        public string Answer2 { get; set; }
        public string SecurityQuestionId3 { get; set; }
        public string Answer3 { get; set; }
    }

     public class SetSecurityQuestion: BaseDto
    {
        public string UserName { get; set; }
        public string CustomerId { get; set; }
        public string Password { get; set; }
        public int SecurityQuestionId { get; set; }
        public string Answer { get; set; }
        public int SecurityQuestionId2 { get; set; }
        public string Answer2 { get; set; }
        public int SecurityQuestionId3 { get; set; }
        public string Answer3 { get; set; }
    }
    public class SecurityQuestionDto
    {
        public int Id { get; set; }
        public string Question { get; set; }
    }
}