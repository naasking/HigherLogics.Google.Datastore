using System;
using System.Collections.Generic;
using System.Text;

namespace HigherLogics.Google.Datastore
{
    /// <summary>
    /// Ignore a property when serializing/deserializing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IgnoreDatastoreAttributeAttribute : Attribute
    {
    }
}
