using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tedd.ChiaPlotter.Enums
{
    internal enum JobStatusEnum
    {
        Queued,
        Running,
        Done,
        Cancelled,
        Failed
    }
}
