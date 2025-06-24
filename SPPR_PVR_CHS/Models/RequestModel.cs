namespace SPPR_PVR_CHS.Models
{
    //Этот класс описывает входные переменные для метода SearchNearestObjects в MapController.
    public class RequestModel
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string[] objects { get; set; }
    }
}
