
using System;
using CIB.Core.Common;

namespace CIB.Core.Modules.SpecialFeatures.Dto
{
    public class SpecialFeatureDto : BaseDto
    {
        public Guid? CorporateCustomerId {get;set;}
        public string Feature {get;set;}
    }
}