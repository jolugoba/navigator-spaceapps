allprojects {
    repositories {
        google()
        mavenCentral()
        // AARs shipped inside the Unity export
        rootProject.findProject(":unityLibrary")?.let { unity ->
            flatDir { dirs(unity.projectDir.resolve("libs")) }
        }
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
