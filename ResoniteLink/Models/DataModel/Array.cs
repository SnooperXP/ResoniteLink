using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ResoniteLink
{
    public abstract class SyncArray : Member
    {
        [JsonIgnore]
        public abstract Type ElementType { get; }
    }
}
