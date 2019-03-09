using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IPlaguesPlaygroundClientMethods
    {
        Task Send(string message);
    }
}
