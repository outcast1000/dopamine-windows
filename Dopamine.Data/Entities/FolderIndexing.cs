using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("FolderIndexing")]
    public class FolderIndexing
    {
        [Column("folder_id"), PrimaryKey()]
        public long FolderId { get; set; }

        [Column("date_indexed"), NotNull()]
        public long DateIndexed { get; set; }

        [Column("total_files")]
        public long TotalFiles { get; set; }

        [Column("max_file_date_modified")]
        public long MaxFileDateModified { get; set; }

        [Column("hash")]
        public string Hash { get; set; }
    }

}
