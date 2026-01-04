using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ResoniteLink
{
    public class Reference : Member
    {
        /// <summary>
        /// The ID of the target that this reference should be set to.
        /// It's important to note that the target needs to be a valid type - it's up to the
        /// caller to ensure that target of correct type is being referenced.
        /// Set to Null to set the reference to null.
        /// </summary>
        [JsonPropertyName("targetId")]
        public string TargetID { get; set; }

        /// <summary>
        /// The type of target that this reference accepts.
        /// Note: This is only for reference. It does not need to be provided when setting a value.
        /// However the target must conform to this type.
        /// </summary>
        [JsonPropertyName("targetType")]
        public string TargetType { get; set; }
    }

    [JsonDerivedType(typeof(Reference), "reference")]
    public partial class Member { }
}
