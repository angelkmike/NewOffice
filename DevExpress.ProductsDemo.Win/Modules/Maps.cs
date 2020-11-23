using System;
using System.Linq;
using System.Xml.Linq;
using DevExpress.XtraMap;
using System.Collections.Generic;
using DevExpress.Demos.OpenWeatherService;
using DevExpress.XtraCharts;

namespace DevExpress.ProductsDemo.Win.Modules {
    public partial class MapsModule : BaseModule {
        readonly double[] celsiusRangeStops = new double[] { -40, -35, -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 };
        readonly double[] fahrenheitRangeStops = new double[] { -40, -31, -22, -13, -4, 5, 14, 23, 32, 41, 50, 59, 68, 77, 86, 94, 103, 112 };
        OpenWeatherMapService _openWeatherMapService;
        CityWeather actualWeatherInfo;
        TemperatureMeasureUnits actualMeasureUnits = TemperatureMeasureUnits.Celsius;

        RangeSegmentColorizer SegmentColorizer { get { return (RangeSegmentColorizer)((LineSeriesView)chartControl1.Series[0].View).SegmentColorizer; } }
        public MapControl MapControl { get { return mapControl1; } }
        protected OpenWeatherMapService OpenWeatherMapService { get { return _openWeatherMapService; } }

        public MapsModule() {
            InitializeComponent();

            TilesLayer.DataProvider = MapUtils.CreateBingDataProvider(BingMapKind.Area);
            mapControl1.SetMapItemFactory(new DemoWeatherItemFactory());

            this._openWeatherMapService = new OpenWeatherMapService(LoadCapitalsFromXML());
            OpenWeatherMapService.ReadCompleted += OpenWeatherMapService_ReadCompleted;
            _openWeatherMapService.GetWeatherAsync();
        }
        protected override bool AutoMergeRibbon { get { return true;  } }

        List<string> LoadCapitalsFromXML() {
            List<string> capitals = new List<string>();
            XDocument document = MapUtils.LoadXml("Capitals.xml");
            if(document != null) {
                foreach(XElement element in document.Element("Capitals").Elements()) {
                    capitals.Add(element.Value);
                }
            }
            return capitals;
        }
        void OpenWeatherMapService_ReadCompleted(object sender, EventArgs e) {
            DataAdapter.DataSource = OpenWeatherMapService.WeatherInCities;
            ItemsLayer.SelectedItem = OpenWeatherMapService.LosAngelesWeather;
            OpenWeatherMapService.ReadCompleted -= OpenWeatherMapService_ReadCompleted;
        }

        void mapControl1_SelectionChanged(object sender, MapSelectionChangedEventArgs e) {
            IList<object> selection = e.Selection;
            if(selection == null || selection.Count != 1)
                return;
            CityWeather cityWeatherInfo = selection[0] as CityWeather;
            this.actualWeatherInfo = cityWeatherInfo;
            if(cityWeatherInfo != null) {
                if(cityWeatherInfo.Forecast == null) {
                    OpenWeatherMapService.GetForecastForCityAsync(cityWeatherInfo);
                    cityWeatherInfo.ForecastUpdated += cityWeatherInfo_ForecastUpdated;
                } else
                    cityWeatherInfo_ForecastUpdated(cityWeatherInfo, null);
            }
        }
        void cityWeatherInfo_ForecastUpdated(object sender, EventArgs e) {
            CityWeather cityWeatherInfo = sender as CityWeather;
            Action<CityWeather> del = LoadWeatherPicture;
            BeginInvoke(del, cityWeatherInfo);
        }
        void LoadWeatherPicture(CityWeather cityWeatherInfo) {
            this.chartControl1.Series[0].DataSource = cityWeatherInfo.Forecast;
            lbCity.Text = cityWeatherInfo.City;
            lbTemperature.Text = cityWeatherInfo.Weather.GetTemperatureString(actualMeasureUnits);
            peWeatherIcon.LoadAsync(cityWeatherInfo.WeatherIconPath);
            UpdateColorizerRangeStops();
        }
        void UpdateColorizerRangeStops() {
            SegmentColorizer.RangeStops.Clear();
            SegmentColorizer.RangeStops.AddRange(actualMeasureUnits == TemperatureMeasureUnits.Celsius ? celsiusRangeStops : fahrenheitRangeStops);
        }
        private void mapControl1_SelectionChanging(object sender, MapSelectionChangingEventArgs e) {
            e.Cancel = e.Selection.Count == 0;
        }

        void chkCelsius_CheckedChanged(object sender, XtraBars.ItemClickEventArgs e) {
            UpdateTemperatureUnit((int)e.Item.Tag);

        }
        void UpdateTemperatureUnit(int temperatureType) {
            if(actualWeatherInfo != null) {
                string member = "Weather.CelsiusTemperature";
                actualMeasureUnits = TemperatureMeasureUnits.Celsius;
                
                if(temperatureType == 0) {
                    actualMeasureUnits = TemperatureMeasureUnits.Fahrenheit;
                    member = "Weather.FahrenheitTemperature";
                }
                UpdateColorizerRangeStops();
                this.chartControl1.Series[0].ValueDataMembers[0] = member;
                lbTemperature.Text = actualWeatherInfo.Weather.GetTemperatureString(actualMeasureUnits);
                DataAdapter.Mappings.Text = actualWeatherInfo.GetTemperatureDataMember(actualMeasureUnits);
            }
        }
        void chkBingRoad_CheckedChanged(object sender, XtraBars.ItemClickEventArgs e) {
            UpdateBingMapKind((int)e.Item.Tag);
        }
        void UpdateBingMapKind(int bingMapKind) {
            BingMapDataProvider provider = (BingMapDataProvider)TilesLayer.DataProvider;
            provider.Kind = (BingMapKind)bingMapKind;
        }


    }
}
