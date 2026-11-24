using Foundation;

namespace Sample;

// iOS / Mac Catalyst 27 terminate apps that have not adopted the UIScene lifecycle; UIApplicationSceneManifest in Info.plist names this
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate;
