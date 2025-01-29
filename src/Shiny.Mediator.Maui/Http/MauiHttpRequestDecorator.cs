using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Http;


public class MauiHttpRequestDecorator<TRequest, TResult>(
    ILogger<MauiHttpRequestDecorator<TRequest, TResult>> logger,
    IAppInfo appInfo,
    IDeviceInfo deviceInfo,
    IGeolocation geolocation
) : IHttpRequestDecorator<TRequest, TResult> where TRequest : IHttpRequest<TResult>
{
    public async Task Decorate(HttpRequestMessage httpMessage, TRequest request)
    {
        httpMessage.Headers.Add("AppId", appInfo.PackageName);
        httpMessage.Headers.Add("AppVersion", appInfo.Version.ToString());
        httpMessage.Headers.Add("DeviceManufacturer", deviceInfo.Manufacturer);
        httpMessage.Headers.Add("DeviceModel", deviceInfo.Model);
        httpMessage.Headers.Add("DevicePlatform", deviceInfo.Platform.ToString());
        httpMessage.Headers.Add("DeviceVersion", deviceInfo.Version.ToString());
        httpMessage.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));

        try
        {
            var result = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (result == PermissionStatus.Granted)
            {
                var gps = await geolocation.GetLastKnownLocationAsync();
                if (gps != null)
                    httpMessage.Headers.Add("GpsCoords", $"{gps.Latitude},{gps.Longitude}");
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Failed to get GPS");
        }
    }
}