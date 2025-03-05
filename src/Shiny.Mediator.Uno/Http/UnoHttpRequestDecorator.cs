// using System.Globalization;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using Windows.System.Profile;
// using Microsoft.Extensions.Logging;
//
// namespace Shiny.Mediator.Http;
//
//
// public class UnoHttpRequestDecorator<TRequest, TResult>(
// ) : IHttpRequestDecorator<TRequest, TResult> where TRequest : IHttpRequest<TResult>
// {
//     public async Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, TRequest request)
//     {
//         var pkg = Windows.ApplicationModel.Package.Current;
//
//         httpMessage.Headers.Add("AppId", pkg.Id.FullName);
//         httpMessage.Headers.Add("AppVersion", pkg.Id.Version.ToString() ?? "1.0.0");
//         httpMessage.Headers.Add("DevicePlatform", AnalyticsInfo.VersionInfo.DeviceFamily);
//         httpMessage.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentCulture.Name));
//
//         // try
//         // {
//         //     var result = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
//         //     if (result == PermissionStatus.Granted)
//         //     {
//         //         var gps = await geolocation.GetLastKnownLocationAsync();
//         //         if (gps != null)
//         //             httpMessage.Headers.Add("GpsCoords", $"{gps.Latitude},{gps.Longitude}");
//         //     }
//         // }
//         // catch (Exception ex)
//         // {
//         //     logger.LogInformation(ex, "Failed to get GPS");
//         // }
//     }
// }