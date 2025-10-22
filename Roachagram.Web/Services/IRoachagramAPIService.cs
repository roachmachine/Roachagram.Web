using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roachagram.Web.Services
{
    public interface IRoachagramAPIService
    {
        Task<string> GetAnagramsAsync(string input);
    }
}
