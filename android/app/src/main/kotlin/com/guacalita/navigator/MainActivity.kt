package com.guacalita.navigator

import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {

    private lateinit var sensorManager: SensorManager
    private var rotationSensor: Sensor? = null
    private var gyroChannel: MethodChannel? = null
    private var gyroActive = false

    private val alpha = 0.15f
    private var smoothAzimuth = 0f
    private var smoothPitch   = 0f
    private var smoothRoll    = 0f

    private val rotationMatrix  = FloatArray(9)
    private val orientationVals = FloatArray(3)

    private val sensorListener = object : SensorEventListener {
        override fun onSensorChanged(event: SensorEvent) {
            if (!gyroActive) return
            SensorManager.getRotationMatrixFromVector(rotationMatrix, event.values)
            SensorManager.getOrientation(rotationMatrix, orientationVals)

            smoothAzimuth += alpha * (orientationVals[0] - smoothAzimuth)
            smoothPitch   += alpha * (orientationVals[1] - smoothPitch)
            smoothRoll    += alpha * (orientationVals[2] - smoothRoll)

            gyroChannel?.invokeMethod("GYRO_UPDATE", mapOf(
                "azimuth" to smoothAzimuth.toDouble(),
                "pitch"   to smoothPitch.toDouble(),
                "roll"    to smoothRoll.toDouble()
            ))
        }
        override fun onAccuracyChanged(sensor: Sensor, accuracy: Int) {}
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        sensorManager = getSystemService(SENSOR_SERVICE) as SensorManager
        rotationSensor = sensorManager.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR)

        gyroChannel = MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            "com.guacalita.navigator/gyro"
        )

        gyroChannel?.setMethodCallHandler { call, result ->
            when (call.method) {
                "ENABLE_GYRO" -> {
                    rotationSensor?.let {
                        sensorManager.registerListener(
                            sensorListener, it,
                            SensorManager.SENSOR_DELAY_GAME
                        )
                    }
                    gyroActive = true
                    result.success(null)
                }
                "DISABLE_GYRO" -> {
                    sensorManager.unregisterListener(sensorListener)
                    gyroActive = false
                    result.success(null)
                }
                else -> result.notImplemented()
            }
        }
    }

    override fun onPause() {
        super.onPause()
        if (gyroActive) sensorManager.unregisterListener(sensorListener)
    }

    override fun onResume() {
        super.onResume()
        if (gyroActive) {
            rotationSensor?.let {
                sensorManager.registerListener(
                    sensorListener, it, SensorManager.SENSOR_DELAY_GAME
                )
            }
        }
    }
}
