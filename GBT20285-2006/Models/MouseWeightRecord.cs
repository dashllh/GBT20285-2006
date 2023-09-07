using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GBT20285_2006.Models
{
    public class MouseWeightRecord
    {
        public short MouseId { get; set; }

        public double? PreWeight2 { get; set; }

        public double? PreWeight1 { get; set; }

        public double? ExpWeight { get; set; }

        public double? PostWeight1 { get; set; }

        public double? PostWeight2 { get; set; }

        public double? PostWeight3 { get; set; }

        public string? Status { get; set; }
    }
}
