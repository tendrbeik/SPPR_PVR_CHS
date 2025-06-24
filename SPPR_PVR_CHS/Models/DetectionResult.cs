namespace SPPR_PVR_CHS.Models
{
    public class DetectionResult
    {
        public readonly List<MapObjectData1> detectedObjects;
        public readonly bool badRequest;

        public DetectionResult(List<MapObjectData1> detectedObjects)
        {
            this.detectedObjects = detectedObjects;
            this.badRequest = false;
        }

        public DetectionResult()
        {
            this.badRequest = true;
        }
    }
}
