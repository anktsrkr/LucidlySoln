using Lucidly.UI.McpServer.Tools;
using Lucidly.UI.McpServer.WeatherAPI.Client;
using Lucidly.UI.McpServer.WeatherAPI.Client.ForecastJson;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lucidly.UI.McpServer.Tools
{
    public class SimplifiedWeatherForecast
    {
        public string Location { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string Region { get; set; } = default!;
        public string Timezone { get; set; } = default!;
        public string LocalTime { get; set; } = default!;

        public CurrentWeatherData Current { get; set; } = default!;
        public List<ForecastDayData> Forecast { get; set; } = new();
    }

    public class CurrentWeatherData
    {
        public string Condition { get; set; } = default!;
        public double? TempC { get; set; }
        public double? FeelsLikeC { get; set; }
        public double? Humidity { get; set; }
        public double? WindKph { get; set; }
        public string WindDir { get; set; } = default!;
        public double? VisibilityKm { get; set; }
        public double? PressureMb { get; set; }
    }

    public class ForecastDayData
    {
        public string Date { get; set; } = default!;
        public double? MaxTempC { get; set; }
        public double? MinTempC { get; set; }
        public string Condition { get; set; } = default!;
        public double? ChanceOfRain { get; set; }
        public double? TotalPrecipMm { get; set; }
        public List<HourlyForecastData> Hourly { get; set; } = new();
    }

    public class HourlyForecastData
    {
        public string Time { get; set; } = default!;
        public double? TempC { get; set; }
        public string Condition { get; set; } = default!;
        public double? Humidity { get; set; }
        public double? WindKph { get; set; }
        public string WindDir { get; set; } = default!;
        public double? ChanceOfRain { get; set; }
    }
    public class GetWeatherForecastForCityInput
    {
        [Description("The name of the location for which the weather forecast is requested, e.g., 'New York', 'Los Angeles'.")]
        public string CityName { get; set; }

        [Description("The country name for the location, if applicable. e.g., 'United Kingdom', 'India'.")]
        public string CountryName { get; set; }

        [Description("The requested date and time for the weather forecast, in the format 'yyyy-MM-dd'.")]
        public string RequestedDate { get; set; }
    }
    [McpServerToolType]
    public class EchoTool(IHttpContextAccessor contextAccessor, IMcpServer mcpServer, WeatherAPIClient weatherAPIClient)
    {
        private readonly IHttpContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
         
        //[McpServerTool, Description("Echoes the message back to the client.")]
        //public async Task<string> Echo(string message)
        //{
        //    //return $"hello {message}";

        //    var xx = await mcpServer.ElicitAsync(
        //           new ElicitRequestParams()
        //           {
        //               Message = "Please provide more information.",
        //               RequestedSchema = new()
        //               {
        //                   Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>()
        //                   {
        //                       ["prop1"] = new ElicitRequestParams.StringSchema
        //                       {
        //                           Title = "title1",
        //                           MinLength = 1,
        //                           MaxLength = 100,
        //                       },
        //                       ["prop2"] = new ElicitRequestParams.NumberSchema
        //                       {
        //                           Description = "description2",
        //                           Minimum = 0,
        //                           Maximum = 1000,
        //                       },
        //                       ["prop3"] = new ElicitRequestParams.BooleanSchema
        //                       {
        //                           Title = "title3",
        //                           Description = "description4",
        //                           Default = true,
        //                       },
        //                       ["prop4"] = new ElicitRequestParams.EnumSchema
        //                       {
        //                           Enum = ["option1", "option2", "option3"],
        //                           EnumNames = ["Name1", "Name2", "Name3"],
        //                       },
        //                   },
        //               },
        //           },
        //           CancellationToken.None);
        //    if (xx.Action == "accept")
        //    {
        //        return $"hello {message}, {_contextAccessor.HttpContext?.Request.Path ?? "(no HttpContext)"}";
        //    }
        //    else if (xx.Action == "cancel")
        //    {
        //        return "User request cancelled.";
        //    }
        //    else
        //    {
        //        return "Unknown action.";

        //    }
        //}

        [McpServerTool, Description("Gets the weather forecast for the specified location and specified date.")]
        public async Task<SimplifiedWeatherForecast> GetWeatherForecastForCity(GetWeatherForecastForCityInput getWeather)
        {
            var forecast = await weatherAPIClient.ForecastJson.GetAsForecastGetResponseAsync(x =>
            {
                x.QueryParameters.Q = $"{getWeather.CityName}, {getWeather.CountryName}";
                x.QueryParameters.Dt = DateOnly.FromDateTime(DateTime.Parse(getWeather.RequestedDate));
            });
            Console.WriteLine($"City :{getWeather.CityName} Country: {getWeather.CountryName} Date : {getWeather.RequestedDate}");
            var location = forecast.Location;
            var current = forecast.Current;
            var day = forecast.Forecast.Forecastday.FirstOrDefault();

            var response = new SimplifiedWeatherForecast
            {
                Location = location.Name,
                Country = location.Country,
                Region = location.Region,
                Timezone = location.TzId,
                LocalTime = location.Localtime,

                Current = new CurrentWeatherData
                {
                    Condition = current.Condition.Text,
                    TempC = current.TempC,
                    FeelsLikeC = current.FeelslikeC,
                    Humidity = current.Humidity,
                    WindKph = current.WindKph,
                    WindDir = current.WindDir,
                    VisibilityKm = current.VisKm,
                    PressureMb = current.PressureMb
                },

                Forecast =
                [
                    new() {
                        Date = day.Date.ToString(),
                        MaxTempC = day.Day.MaxtempC,
                        MinTempC = day.Day.MintempC,
                        Condition = day.Day.Condition.Text,
                        ChanceOfRain = day.Day.DailyChanceOfRain,
                        TotalPrecipMm = day.Day.TotalprecipMm,
                        Hourly = [.. day.Hour.Take(12).Select(h => new HourlyForecastData
                        {
                            Time = h.Time,
                            TempC = h.TempC,
                            Condition = h.Condition.Text,
                            Humidity = h.Humidity,
                            WindKph = h.WindKph,
                            WindDir = h.WindDir,
                            ChanceOfRain = h.ChanceOfRain
                        })]
                    }
                ]
            };
            Console.WriteLine(JsonSerializer.Serialize(response));
            return response;
        }

        //[McpServerTool, Description("Gets the current weather for the specified city and specified date time.")]
        //public async Task<string> GetWeatherForCity(string cityName, string currentDateTimeInUtc)
        //{

        //        return cityName switch
        //        {
        //            "Boston" => "61 and rainy",
        //            "London" => "55 and cloudy",
        //            "Miami" => "80 and sunny",
        //            "Paris" => "60 and rainy",
        //            "Tokyo" => "50 and sunny",
        //            "Sydney" => "75 and sunny",
        //            "Tel Aviv" => "80 and sunny",
        //            _ => "31 and snowing",
        //        };


        //}
        [McpServerTool, Description("Retrieves the current date time in UTC.")]
        public string GetCurrentDateTimeInUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
