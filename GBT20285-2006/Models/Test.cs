using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Models;

[PrimaryKey("Testid", "Specimenid")]
[Table("test")]
public partial class Test
{
    [Key]
    [Column("testid")]
    [StringLength(50)]
    [Unicode(false)]
    public string Testid { get; set; } = null!;

    [Key]
    [Column("specimenid")]
    [StringLength(50)]
    [Unicode(false)]
    public string Specimenid { get; set; } = null!;

    [Column("reportid")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Reportid { get; set; }

    [Column("ambtemp")]
    public byte? Ambtemp { get; set; }

    [Column("ambhumi")]
    public byte? Ambhumi { get; set; }

    [Column("speciweight")]
    public double? Speciweight { get; set; }

    [Column("speciweightpost")]
    public double? Speciweightpost { get; set; }

    [Column("smokerate")]
    public double? Smokerate { get; set; }

    [Column("specilength")]
    public double? Specilength { get; set; }

    [Column("apparatusid")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Apparatusid { get; set; }

    [Column("apparatusname")]
    [Unicode(false)]
    public string? Apparatusname { get; set; }

    [Column("checkdatef", TypeName = "date")]
    public DateTime? Checkdatef { get; set; }

    [Column("checkdatet", TypeName = "date")]
    public DateTime? Checkdatet { get; set; }

    [Column("according")]
    [MaxLength(50)]
    public byte[]? According { get; set; }

    [Column("safetylevel")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Safetylevel { get; set; }

    [Column("gasconcen")]
    public double? Gasconcen { get; set; }

    [Column("heattemp")]
    public double? Heattemp { get; set; }

    [Column("cgasflow")]
    public double? Cgasflow { get; set; }

    [Column("dgasflow")]
    public double? Dgasflow { get; set; }

    [Column("furnacespeed")]
    public double? Furnacespeed { get; set; }

    [Column("mousecnt")]
    public byte? Mousecnt { get; set; }

    [Column("recoveryday")]
    public byte? Recoveryday { get; set; }

    [Column("phenocode")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Phenocode { get; set; }

    [Column("testdate", TypeName = "date")]
    public DateTime? Testdate { get; set; }

    [Column("operator")]
    [Unicode(false)]
    public string? Operator { get; set; }

    [Column("comment")]
    [Unicode(false)]
    public string? Comment { get; set; }

    [Column("nounresult")]
    public bool? Nounresult { get; set; }

    [Column("irriresult")]
    public bool? Irriresult { get; set; }

    [Column("testresult")]
    public bool? Testresult { get; set; }

    [Column("flag")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Flag { get; set; }
}
