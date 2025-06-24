using Compunet.YoloSharp;
//using OpenCvSharp;
using SixLabors.ImageSharp.PixelFormats;
using Compunet.YoloSharp.Plotting;
using SixLabors.ImageSharp;

namespace SPPR_PVR_CHS.Models
{
    public class ImagePredictor
    {
        //Добавим переменную, в которую запишем обученную модель
        private static string modelPath = "./models/yolov11m_trained.onnx";
        private static YoloPredictor predictor = new YoloPredictor(modelPath);

        public ImagePredictor() { }

        public Compunet.YoloSharp.Data.YoloResult<Compunet.YoloSharp.Data.Detection> predict(string filePath)
        {
            //Проведём детекцию объектов на изображении и сформулируем в виде Json список объектов, обнаруженных на изображении.
            var image = Image.Load(filePath);
            var result = predictor.Detect(image);
            

            //Marker: В качестве тестовой функции будем отображать результаты детекции на изображениях,
            //после чего будем сохранять изображения на диске. Потом можно будет закомментировать этот код
            //или вынести его отдельную функцию.

            // Создание изображения с нанесенными предсказаниями
            var plottedImage = result.PlotImage(image);

            // Возьмём имя исходного изображения для сохранения изображения с результатами детекции.
            string imageName = Path.GetFileName(filePath);

            // Сохранение результирующего изображения
            plottedImage.SaveAsPng($"./wwwroot/detectionResults/{imageName}");

            return result;
        }
    }
}
