using System;
using System.ComponentModel.DataAnnotations;

namespace FactorioWebInterface.Data
{
    public class Ban
    {
        [Key]
        public string Username { get; set; }
        public string Reason { get; set; }
        public string Address { get; set; }
        public string Admin { get; set; }
        public DateTime DateTime { get; set; }
    }
}
