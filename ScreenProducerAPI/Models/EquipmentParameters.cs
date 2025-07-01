using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScreenProducerAPI.Models
{
    [Table("equipment_parameters")]
    public class EquipmentParameters
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("input_sand_kg")]
        public int InputSandKg { get; set; }

        [Column("input_copper_kg")]
        public int InputCopperKg { get; set; }

        [Column("output_screens_day")]
        public int OutputScreens { get; set; }
    }
}
