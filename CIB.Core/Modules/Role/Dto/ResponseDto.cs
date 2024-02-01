using System;
namespace CIB.Core.Modules.Role.Dto
{
    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public int Sn { get; set; }
        public string RoleName { get; set; }
        public int? Grade { get; set; }
        public int? Status { get; set; }
    }
}