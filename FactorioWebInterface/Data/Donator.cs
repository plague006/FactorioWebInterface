using System;
using System.ComponentModel.DataAnnotations;

namespace FactorioWebInterface.Data
{
    [Flags]
    public enum DonatorPerks
    {
        Rank,
        Train
    }

    public class Donator
    {
        [Key]
        public string Name { get; set; }
        public DonatorPerks Perks { get; set; }
        public string WelcomeMessage { get; set; }
    }
}
