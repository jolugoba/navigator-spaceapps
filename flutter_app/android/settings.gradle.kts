pluginManagement {
    val flutterSdkPath =
        run {
            val properties = java.util.Properties()
            file("local.properties").inputStream().use { properties.load(it) }
            val flutterSdkPath = properties.getProperty("flutter.sdk")
            require(flutterSdkPath != null) { "flutter.sdk not set in local.properties" }
            flutterSdkPath
        }

    includeBuild("$flutterSdkPath/packages/flutter_tools/gradle")

    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

plugins {
    id("dev.flutter.flutter-plugin-loader") version "1.0.0"
    id("com.android.application") version "9.1.0" apply false
    id("org.jetbrains.kotlin.android") version "2.4.0" apply false
}

include(":app")

// Unity exported as a library (Unity: Navigator → Flutter → Export Android → flutter_app).
// Until it exists the app still builds and shows how to export it.
val unityLibraryDir = file("unityLibrary")
if (unityLibraryDir.resolve("build.gradle").exists() || unityLibraryDir.resolve("build.gradle.kts").exists()) {
    include(":unityLibrary")
    if (unityLibraryDir.resolve("xrmanifest.androidlib").isDirectory) {
        include(":unityLibrary:xrmanifest.androidlib")
    }
}
