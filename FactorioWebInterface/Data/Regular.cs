using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FactorioWebInterface.Data
{
    public class Regular
    {
        [Key]
        public string Name { get; set; }
        public DateTimeOffset Date { get; set; }
        public string PromotedBy { get; set; }
    }
}
