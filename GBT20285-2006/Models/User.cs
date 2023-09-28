using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Models;

[Table("user")]
public partial class User
{
    [Key]
    [Column("userid")]
    [StringLength(50)]
    [Unicode(false)]
    public string Userid { get; set; } = null!;

    [Column("passwd")]
    [StringLength(64)]
    [Unicode(false)]
    public string? Passwd { get; set; }

    [Column("dispname")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Dispname { get; set; }

    [Column("type")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Type { get; set; }
}
