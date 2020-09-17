using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public interface ICleanUpImagesUnitOfWork : IDisposable
    {
        //
        // Summary:
        //     Clean up all Images from albums who have zero tracks (deleted or ignored are not counted)
        //
        // Parameters:
        //   none
        //
        // Returns:
        //     The number of Images that were deleted from the db
        long CleanUp();
    }
}
