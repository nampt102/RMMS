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
    // Version matrix (verified stable with Flutter 3.44 + AndroidX libs as of May 2026):
    //   - AGP 8.11.x  (8.9.1+ required by androidx.activity 1.12.x, core-ktx 1.18.x)
    //   - Kotlin 2.2.20  (Flutter has deprecated <2.2.20 in recent stables)
    //   - Gradle 8.11.x  (paired with AGP 8.11)
    // Going higher (AGP 9, Kotlin 2.3) was tried and hit Flutter#169475.
    id("com.android.application") version "8.11.0" apply false
    id("org.jetbrains.kotlin.android") version "2.2.20" apply false
}

include(":app")
