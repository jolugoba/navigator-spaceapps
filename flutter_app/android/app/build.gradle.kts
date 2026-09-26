plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

val hasUnity = rootProject.findProject(":unityLibrary") != null
if (!hasUnity) {
    logger.warn("Navigator: android/unityLibrary not found - building WITHOUT the Unity game. " +
        "In Unity run: Navigator -> Flutter -> Export Android -> flutter_app")
}

android {
    namespace = "com.guacalita.navigator"
    compileSdk = flutter.compileSdkVersion
    // Must be >= the NDK Unity 6 builds with (r27c); Flutter's default (r28) is newer
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "com.guacalita.navigator"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = 26   // = Unity Player Settings "Minimum API Level" (the library can't be lower than this)
        targetSdk = flutter.targetSdkVersion
        // Uses the version code from pubspec.yaml. When using split APKs, 1000 * ABI_VERSION
        // is added automatically by Flutter. (https://developer.android.com/studio/build/configure-apk-splits#configure-APK-versions)
        // You can force using the value of versionCode by specifying the `-P force-version-code-ignoring-abi=true`
        // flag during build.
        versionCode = flutter.versionCode
        versionName = flutter.versionName
        // Lets the Flutter side know whether Unity is linked into this build
        resValue("bool", "navigator_has_unity", hasUnity.toString())
    }

    buildFeatures {
        resValues = true   // navigator_has_unity (off by default in newer AGP)
    }

    // Unity's data files are already compressed: don't compress them again (faster start)
    androidResources {
        val unityStreamingAssets = (project.findProperty("unityStreamingAssets") as? String)
            ?.split(",")?.map { it.trim() }?.filter { it.isNotEmpty() } ?: emptyList()
        noCompress += listOf(".unity3d", ".ress", ".resource", ".obb", ".bundle", ".unityexp") + unityStreamingAssets
        ignoreAssetsPattern = "!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~"
    }

    buildTypes {
        release {
            // TODO: Add your own signing config for the release build.
            // Signing with the debug keys for now, so `flutter run --release` works.
            signingConfig = signingConfigs.getByName("debug")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}

dependencies {
    if (hasUnity) implementation(project(":unityLibrary"))
}
