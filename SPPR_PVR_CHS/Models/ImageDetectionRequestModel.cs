namespace SPPR_PVR_CHS.Models
{
    //Marker: Классы coords, requestModel, imageDetectionRequestModel лучше вынести в model

    //Этот класс описывает входные переменные для метода ImageDetection.
    //Marker: Надо попробовать убрать сеттеры здесь. В целом они нам не нужны в методах. Посмотрим сломается ли программа.
    public class ImageDetectionRequestModel
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int zoom { get; set; }
    }
}
