using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using SPPR_PVR_CHS.Models;
using static SPPR_PVR_CHS.Controllers.MapController;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SPPR_PVR_CHS.Controllers
{
    public class MapController : Controller
    {
        //Ключ от Geocode API
        private static readonly string apiKey1 = "YOUR_API_KEY";
        //Ключ от Static API
        private static readonly string apiKey2 = "YOUR_API_KEY";
        //Ключ от Geosuggest API
        private static readonly string apiKey3 = "YOUR_API_KEY";
        private static readonly string geocodeUrl = "https://geocode-maps.yandex.ru/1.x/";
        private static readonly string staticApiUrl = "https://static-maps.yandex.ru/v1";
        private static readonly string geosuggestApiUrl = "https://suggest-maps.yandex.ru/v1/suggest?";
        private static readonly int zoom = 18;
        private static readonly string requestStyle = "elements:label|stylers.visibility:off";
        //Это долгота, которую в себя вмещает каждое изображение 450х450 при зуме 18.
        //Данное значение получено экспериментальным путём и возможны правки.
        private static double longWidth18z = 0.002430724220055;

        //Это длина в метрах 0.001 градуса широты
        private static double lengthLat = 111.132;

        [HttpGet]
        public IActionResult Map()
        {
            return View();
        }

        private async Task<DetectionResult> detectObjects(Coords data, int zoom)
        {
            string requestURL = $"{staticApiUrl}?ll={data.longitude.ToString(CultureInfo.InvariantCulture)},{data.latitude.ToString(CultureInfo.InvariantCulture)}&lang=ru_RU&size=450,450&z={zoom}&style={requestStyle}&apikey={apiKey2}";

            var resultObjects = new List<MapObjectData1>();

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(requestURL);

                if (!response.IsSuccessStatusCode)
                {
                    //Возвращаем BadRequest.
                    return new DetectionResult();
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                string filename = $"map_{data.latitude}_{data.longitude}.png";

                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", filename);

                //Сохранить изображение в папку проекта.
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                string fileUrl = $"/images/{filename}";
                //Сделать фейковую обработку изображения и в виде json отправить ответ на сервер.
                ImagePredictor predictor = new ImagePredictor();
                var result = predictor.predict(filePath);
                //Вычислим текущую высоту изображения в градусах широты
                double latHeight = calculateLatHeight(data.latitude);

                foreach (var obj in result)
                {
                    MapObjectData1 objectData = new MapObjectData1();
                    objectData.type = obj.Name.Name.ToString();
                    //Вот тут надо вычислять координаты. Поэтому я пока просто подставлю имеющиеся значения.
                    objectData.X1 = data.longitude + longWidth18z * ((double)obj.Bounds.X / 450 - 1d / 2);
                    objectData.Y1 = data.latitude + latHeight * ((double)(450 - obj.Bounds.Y) / 450 - 1d / 2);
                    objectData.X2 = objectData.X1 + ((double)obj.Bounds.Width / 450) * longWidth18z;
                    objectData.Y2 = objectData.Y1 - ((double)obj.Bounds.Height / 450) * latHeight;
                    resultObjects.Add(objectData);
                }
                return new DetectionResult(resultObjects);
            }
        }

        public async Task<IActionResult> ImageDetection([FromBody] ImageDetectionRequestModel data)
        {
            //Что я должен тут сделать? По сути у меня есть код, который выполняет обнаружение объектов на одной картинке.
            /*
             * Теперь надо сделать выполнение метода гибким. У нас есть два основных сценария. Зум больше или равен 18. В таком случае мы выполняем простой код,
             * который у нас уже есть просто подставляем требуемый зум.
             * Второй сценарий - зум равен 17. В таком случае берём четыре координаты рядом с точкой заданной пользователем и для каждой точки делаем снимок
             * местности с приближением равным 18. После этого данные 4 изображения обрабатываются нейросетью. Такие дела. Дальше надо сложить все 4 Json файла в один
             * и передать эти данные на отрисовку клиенту.
             */
            //Marker: не помню какой зум максимальный в яндекс картах, поэтому пусть пока будет 20-ый.
            if (data.zoom >= 18 && data.zoom <= 20)
            {
                //Проводим детекцию объектов по заданным координатам
                Coords coords = new Coords(data.latitude, data.longitude);
                var result = detectObjects(coords, zoom);
                if (result.Result.badRequest)
                    return BadRequest("Image load error!");
                else
                    return Json(result.Result.detectedObjects);
            }
            else if (data.zoom == 17)
            {
                double latHeight18z = calculateLatHeight(data.latitude);
                //Высчитываем координаты 4-точек
                List<Coords> coords = new List<Coords>();
                //Marker: Пока напишем рассчёт точек вручную, но вообще можно алгоритм забацать. В целом там нет чего-то очень сложного,
                //просто сейчас никаких идей в голове нет.
                Coords c = new Coords();
                c.latitude = data.latitude + 0.5 * latHeight18z;
                c.longitude = data.longitude - 0.5 * longWidth18z;
                coords.Add(c);
                c = new Coords();
                c.latitude = data.latitude + 0.5 * latHeight18z;
                c.longitude = data.longitude + 0.5 * longWidth18z;
                coords.Add(c);
                c = new Coords();
                c.latitude = data.latitude - 0.5 * latHeight18z;
                c.longitude = data.longitude + 0.5 * longWidth18z;
                coords.Add(c);
                c = new Coords();
                c.latitude = data.latitude - 0.5 * latHeight18z;
                c.longitude = data.longitude - 0.5 * longWidth18z;
                coords.Add(c);

                //Сюда будем записывать найденные объекты.
                var resultObjects = new List<MapObjectData1>();

                foreach (var value in coords)
                {
                    /*
                     * Marker: Получается какой-то спагетти-код. Надо бы по хорошему вынести эту функцию поиска объектов на изображении в отдельный метод и вызывать его там, где надо.
                     * Надо только найти способ соединять JSON-файлы в один и тогда всё будет отлично работать.
                     */
                    var result = detectObjects(value, zoom);
                    if (result.Result.badRequest)
                        return BadRequest("Image load error!");
                    else
                        foreach (var obj in result.Result.detectedObjects)
                        {
                            resultObjects.Add(obj);
                        }
                }
                return Json(resultObjects);
            }
            else
            {
                return BadRequest("Invalid zoom value!");
            }
        }

        /*
         * Метод называется "Найти ближайшие объекты"
         * На вход принимает координаты точки и список объектов для поиска
         * На выход даёт координаты и названия ближайших к заданной точке искомых
         * объектов
         */
        public async Task<IActionResult> SearchNearestObjects([FromBody] RequestModel data)
        {
            //Сюда будем сохранять данные о каждом из заданных объектов
            var resultObjects = new List<MapObjectData2>();
            foreach (string obj in data.objects)
            {
                if (data == null)
                {
                    return Json(new { error = "Входные данные не указаны" });
                }
                //Здесь мы должны для каждого элемента списка объектов objects провести обработку
                /*Marker: Пока мы сделаем всё как три отдельных запроса, но как только в голове всё сложится, то переделаем
                 *эту функцию на более гибкий вариант.
                 */

                //Делаем запрос в геосаггест и получаем адрес.
                using (var httpClient = new HttpClient())
                {
                    // Формирование URL запроса
                    string requestURL = $"{geosuggestApiUrl}apikey={apiKey3}&text={obj}&results=10&ll={data.longitude.ToString(CultureInfo.InvariantCulture)},{data.latitude.ToString(CultureInfo.InvariantCulture)}&spn=1,1&strict_bounds=0&print_address=1";

                    var response = await httpClient.GetAsync(requestURL);

                    if (!response.IsSuccessStatusCode)
                    {
                        //TODO: Надо узнать корректно ли на английском моё сообщение об ошибке.
                        return BadRequest("Запрос к Geosuggest API был неуспешен!");
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();

                    //Объявим свойства, которые будем доставать из Geosuggest API
                    string? name;
                    string? distance;
                    string? country;
                    string? formatted_address;

                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement results = root.GetProperty("results");
                        //Здесь мы будем искать ближайший объект.
                        if (results.GetArrayLength() == 0)
                        {
                            return BadRequest("Geosuggest API не нашёл объектов заданного типа.");
                        }
                        int minIndex = 0;
                        double? minDist = results[0]
                            .GetProperty("distance")
                            .GetProperty("value")
                            .GetDouble();
                        for (int i = 1; i < results.GetArrayLength(); i++)
                        {
                            double dist = results[i]
                            .GetProperty("distance")
                            .GetProperty("value")
                            .GetDouble();
                            if (dist < minDist)
                            {
                                minDist = dist;
                                minIndex = i;
                            }
                        }
                        //Итак, по идее мы нашли ближайший объект. Теперь надо для него достать все необходимые нам данные.
                        JsonElement addressComponent = results[minIndex]
                           .GetProperty("address")
                           .GetProperty("component");
                        name = results[minIndex]
                            .GetProperty("title")
                            .GetProperty("text")
                            .GetString();
                        distance = results[minIndex]
                            .GetProperty("distance")
                            .GetProperty("text")
                            .GetString();
                        country = addressComponent[0]
                            .GetProperty("name")
                            .GetString();
                        formatted_address = results[minIndex]
                           .GetProperty("address")
                           .GetProperty("formatted_address")
                           .GetString();
                    }
                    //Делаем запрос в геокодер и получаем название организации и координаты.

                    // Формирование URL запроса
                    requestURL = $"{geocodeUrl}?apikey={apiKey1}&geocode={country}+{formatted_address}+{name}&format=json&results=1";

                    var response2 = await httpClient.GetAsync(requestURL);

                    if (!response2.IsSuccessStatusCode)
                    {
                        //TODO: Надо узнать корректно ли на английском моё сообщение об ошибке.
                        return BadRequest("Запрос к Geocoder API был неуспешен!");
                    }

                    string response2Content = await response2.Content.ReadAsStringAsync();

                    string? longitude;
                    string? latitude;

                    using (JsonDocument doc = JsonDocument.Parse(response2Content))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement featureMember = root.GetProperty("response")
                            .GetProperty("GeoObjectCollection")
                            .GetProperty("featureMember");
                        string? pos = featureMember[0]
                            .GetProperty("GeoObject")
                            .GetProperty("Point")
                            .GetProperty("pos")
                            .GetString();

                        string[] posParts = pos.Split(' ');

                        longitude = posParts[0];
                        latitude = posParts[1];
                    }
                    //Потом все эти вещи надо сохранить и выдать в качестве JSON, как мы это уже делали с объектами для
                    //отображения на карте.

                    MapObjectData2 objData = new MapObjectData2();
                    objData.name = name;
                    objData.formattedAddress = $"{country},{formatted_address},{name}";
                    objData.distance = distance;
                    objData.latitude = double.Parse(latitude, CultureInfo.InvariantCulture);
                    objData.longitude = double.Parse(longitude, CultureInfo.InvariantCulture);
                    resultObjects.Add(objData);
                }
            }
            return Json(resultObjects);
        }

        private double calculateLatHeight(double lat)
        {
            double latHeight = longWidth18z / lengthLat * Math.Cos(lat * Math.PI / 180) * Math.PI / 180 * 6378137 * 0.001;
            return latHeight;
        }
    }
}
