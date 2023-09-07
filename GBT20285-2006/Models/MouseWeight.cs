using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Models;

[PrimaryKey("ProductId", "TestId", "MouseId")]
[Table("mouseweight")]
public partial class MouseWeight
{
    [Key]
    [Column("product_id")]
    [StringLength(50)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Key]
    [Column("test_id")]
    [StringLength(50)]
    [Unicode(false)]
    public string TestId { get; set; } = null!;

    [Key]
    [Column("mouse_id")]
    public short MouseId { get; set; }

    [Column("pre_weight2")]
    public double? PreWeight2 { get; set; }

    [Column("pre_weight1")]
    public double? PreWeight1 { get; set; }

    [Column("exp_weight")]
    public double? ExpWeight { get; set; }

    [Column("post_weight1")]
    public double? PostWeight1 { get; set; }

    [Column("post_weight2")]
    public double? PostWeight2 { get; set; }

    [Column("post_weight3")]
    public double? PostWeight3 { get; set; }

    [Column("status")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }
}
