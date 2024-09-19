﻿using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Widgets.Zoom;
using Map = Mapsui.Map;
using MyLocationLayer = Mapsui.Layers.MyLocationLayer;

namespace MFASeeker.Model
{
    public static class MapManager
    {
        private static Map? Map;

        private static readonly MPoint MOSCOWLOCATION = new(37.6156, 55.7522);
        private static MyLocationLayer? _myLocationLayer;
        private static CancellationToken cancellationToken;
        private static MPoint? currentLocationMPoint;

        // Работа с картой
        public static Map CreateMap()
        {
            Map = new() { CRS = "EPSG:3857" };
            //
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            // Добавление виджета масштабирование + и -
            Map.Widgets.Add(CreateZoomInOutWidget(Orientation.Vertical, Mapsui.Widgets.VerticalAlignment.Bottom, Mapsui.Widgets.HorizontalAlignment.Right));

            // Переобразование под сферический тип координат для OSM
            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(MOSCOWLOCATION.X, MOSCOWLOCATION.Y).ToMPoint();
            Map.Home = n => n.CenterOnAndZoomTo(sphericalMercatorCoordinate, n.Resolutions[19]);
            return Map;
        }
        public static void EnableCompassMode()
        {
            // Мониторинг компаса
            Geolocator.OnCompassChangedAction += (newReading) =>
            {
                if (Map == null || _myLocationLayer == null)
                    return;
                if (newReading == -1)
                    _myLocationLayer.UpdateMyViewDirection(-1, Map.Navigator.Viewport.Rotation, true);
                else
                    _myLocationLayer.UpdateMyViewDirection(newReading, Map.Navigator.Viewport.Rotation, true);
            };
            Geolocator.ToggleCompass();
        }
        public static async Task EnableSpectateModeAsync(CancellationToken token)
        {
            //
            if (Map == null) return;
            if (_myLocationLayer == null)
            {
                _myLocationLayer = new MyLocationLayer(Map);
                Map.Layers.Add(_myLocationLayer);
            }
            //
            cancellationToken = token;

            // Мониторинг текущего местоположения
            var progress = new Progress<Location>(location =>
            {
                // конвертируется МПоинт в сферические координаты
                currentLocationMPoint = SphericalMercator.FromLonLat(location.Longitude, location.Latitude).ToMPoint();
                if (!cancellationToken.IsCancellationRequested && _myLocationLayer != null)
                    _myLocationLayer.UpdateMyLocation(currentLocationMPoint, true);
            });
            // При изменении локции прогресс прогоняется заново
            await Geolocator.Default.StartListening(progress, cancellationToken);
        }
        // Виджеты
        private static ZoomInOutWidget CreateZoomInOutWidget(Orientation orientation,
        Mapsui.Widgets.VerticalAlignment verticalAlignment, Mapsui.Widgets.HorizontalAlignment horizontalAlignment)
        {
            return new ZoomInOutWidget
            {
                Orientation = orientation,
                VerticalAlignment = verticalAlignment,
                HorizontalAlignment = horizontalAlignment,
                MarginX = 25,
                MarginY = 150,
                BackColor = new Mapsui.Styles.Color(0, 0, 0, 240),
                Size = 45f
            };
        }
    }
}
