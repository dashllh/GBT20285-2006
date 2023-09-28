namespace GBT20285_2006.Models
{
    public class AppGlobal
    {
        //传感器集合
        public static Dictionary<int, Sensor> Sensors { get; set; } = null!;

        //public AppGlobal()
        //{
        //    Sensors = new Dictionary<int, Sensor>();
        //}
    }
}
