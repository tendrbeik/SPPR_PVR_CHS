namespace SPPR_PVR_CHS.Models
{
    //Этот класс описывает данные, которые мы указываем при формировании POST запросов со стороны клиента для большинства методов
    //в MapController.
    public class Coords
    {
        public double latitude { get; set; }
        public double longitude { get; set; }

        public Coords() { }

        public Coords(double latitude, double longitude) {
            this.latitude = latitude;
            this.longitude = longitude;
        }
    }
}
