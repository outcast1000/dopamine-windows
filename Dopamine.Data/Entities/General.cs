using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("General")]
    public class General
    {
        public General() { }

        [Column("key"), PrimaryKey()]
        public string Key { get; set; }

        [Column("value")]
        public string Value { get; set; }

    }
}
