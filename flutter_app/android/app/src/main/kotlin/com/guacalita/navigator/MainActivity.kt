package com.guacalita.navigator

import com.learntoflutter.flutter_embed_unity_android.unity.FakeUnityPlayerActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

// FakeUnityPlayerActivity (from flutter_embed_unity) is a FlutterActivity that also looks like
// Unity's own player Activity, which some Unity code (AR, permissions) expects to find.
class MainActivity : FakeUnityPlayerActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, "navigator/host")
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    // false when the app was built before exporting Unity into android/unityLibrary
                    "hasUnity" -> result.success(resources.getBoolean(R.bool.navigator_has_unity))
                    else -> result.notImplemented()
                }
            }
    }
}
