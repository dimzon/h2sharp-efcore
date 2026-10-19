using System.ComponentModel.DataAnnotations;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Metadata.Conventions;

public class H2PostgresModelFinalizingConventionTest
{

    private class Blog
    {
        public int Id { get; set; }

        [Timestamp]
        public uint RowVersion { get; set; }
    }
}
