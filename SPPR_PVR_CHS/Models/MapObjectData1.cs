namespace SPPR_PVR_CHS.Models
{
    //Данный класс предназначен для того, чтобы хранить информацию об обнаруженном
    //нейросетью объекте на карте.
    /*
     * type - имя объекта (класс объекта, определённый нейросетью)
     * X1 - долгота левого верхнего угла рамки объекта
     * Y1 - широта левого верхнего угла рамки объекта
     * X2 - долгота правого нижнего угла рамки объекта
     * Y2 - широта правого нижнего угла рамки объекта
     */
    public class MapObjectData1
    {
        public string type { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }
}
