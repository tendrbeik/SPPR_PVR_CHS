namespace SPPR_PVR_CHS.Models
{
    /*
     * Данный класс предназначен для того, чтобы хранить данные о муниципальных объектах,
     * обнаруженных в методе SearchNearestObjects.
     * О каждом объекте мы знаем:
     * -Название;
     * -Адрес;
     * -Расстояние до;
     * -Координаты на карте.
     */
    public class MapObjectData2
    {
        public string name { get; set; }
        public string formattedAddress { get; set; }
        public string distance { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
}
