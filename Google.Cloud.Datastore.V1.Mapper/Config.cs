using System;
using System.Collections.Generic;
using System.Text;

namespace Google.Cloud.Datastore.V1.Mapper
{
    /// <summary>
    /// The configuration options
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The mapper used to marshal values and entities.
        /// </summary>
        public static IEntityMapper Mapper { get; set; } = new PropertyMapper();
    }
}
