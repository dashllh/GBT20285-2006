using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Models;

[Table("apparatus")]
public partial class Apparatus
{
    [Key]
    [Column("apparatusid")]
    [StringLength(50)]
    [Unicode(false)]
    public string Apparatusid { get; set; } = null!;

    [Column("apparatusname")]
    [Unicode(false)]
    public string? Apparatusname { get; set; }

    [Column("checkdatef", TypeName = "date")]
    public DateTime? Checkdatef { get; set; }

    [Column("checkdatet", TypeName = "date")]
    public DateTime? Checkdatet { get; set; }
}
