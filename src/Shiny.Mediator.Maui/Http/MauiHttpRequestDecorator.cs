using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Http;


/// <summary>
/// <see cref="IHttpRequestDecorator"/> that appends MAUI app identity (<c>AppId</c>, <c>AppVersion</c>),
/// device metadata (manufacturer/model/platform/version), <c>Accept-Language</c> from the current
/// culture, and — when <c>LocationWhenInUse</c> permission is granted — a <c>GpsCoords</c> header
/// to outbound HTTP requests issued through the mediator HTTP pipeline.
/// </summary>
public class MauiHttpRequestDecorator(
    ILogger<MauiHttpRequestDecorator> logger,
    IAppInfo appInfo,
    IDeviceInfo deviceInfo,
    IGeolocation geolocation
) : IHttpRequestDecorator
{
    /// <inheritdoc/>
    public async Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, CancellationToken cancellationToken)
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