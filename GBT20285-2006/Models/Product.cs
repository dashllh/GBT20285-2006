using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Models;

[Table("product")]
public partial class Product
{
    [Key]
    [Column("productid")]
    [StringLength(50)]
    [Unicode(false)]
    public string Productid { get; set; } = null!;

    [Column("productname")]
    [Unicode(false)]
    public string? Productname { get; set; }

    [Column("specification")]
    [Unicode(false)]
    public string? Specification { get; set; }

    [Column("shape")]
    [Unicode(false)]
    public string? Shape { get; set; }
}
