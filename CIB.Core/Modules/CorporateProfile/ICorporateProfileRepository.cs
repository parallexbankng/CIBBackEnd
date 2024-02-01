using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.CorporateProfile
{
	public interface ICorporateProfileRepository : IRepository<TblCorporateProfile>
	{
		TblCorporateProfile GetProfileByPhoneNumber(string phoneNumber);
		TblCorporateProfile GetCorporateCustomerIdByUserName(string userName);
		string GetProfileRoleName(string userName);
		TblCorporateProfile GetProfileByEmail(string email);
		CorporateProfileResponseDto RetrieveProfileByID(Guid id);
		TblCorporateProfile GetProfileByID(Guid id);
		IEnumerable<CorporateProfileResponseDto> GetAllCorporateProfiles();
		IEnumerable<TblCorporateProfile> GetCorporateProfiles(Guid id);
		TblCorporateProfile CheckDuplicateUserName(string userName);
		IEnumerable<CorporateProfileResponseDto> GetAllCorporateProfilesByCorporateCustomerId(Guid id);
		IEnumerable<CorporateProfileResponseDto> GetSingleSignatoryCorporateProfilesByCorporateCustomerId(Guid id);
		bool IsAdminActive(Guid roleId, Guid CorporateCustomerId);
		void UpdateCorporateProfile(TblCorporateProfile update);
		CorporateUserStatus CheckDuplicate(TblCorporateProfile profile, Guid CorporateCustomerId, bool isUpdate = false);
		TblCorporateProfile GetProfileByUserName(string userName);
		TblCorporateProfile GetCorporateProfileByEmail(string email);
		TblCorporateProfile GetProfileByUserNameAndCustomerId(string userName, Guid CorporateCustomerId);
		TblCorporateProfile GetProfileByUserIdAndCustomerId(Guid userId, Guid CorporateCustomerId);
		TblCorporateProfile GetProfileByEmailAndCustomerId(string email, Guid CorporateCustomerId);
		TblCorporateProfile CheckActiveProfile(TblCorporateProfile activeProfile, Guid CorporateCustomerId);
		List<TblCorporateProfile> GetProfileByCorporateCustomerId(Guid CorporateCustomerId);
		IEnumerable<TblCorporateProfile> GetCorporateProfilesByRole(string role);
	}
}