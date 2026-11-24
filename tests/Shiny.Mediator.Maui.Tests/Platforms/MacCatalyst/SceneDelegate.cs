using Foundation;

namespace Shiny.Mediator.Maui.Tests;

// iOS / Mac Catalyst 27 terminate apps that have not adopted the UIScene lifecycle; UIApplicationSceneManifest in Info.plist names this
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate;
