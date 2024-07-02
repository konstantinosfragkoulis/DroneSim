using System;
using System.Runtime.InteropServices;

using UnityEngine;

public class DroneController : MonoBehaviour {
    private const int O_RDONLY = 0x0000;
    private const int PROT_READ = 0x1;
    private const int MAP_SHARED = 0x01;

    [DllImport("libc", SetLastError = true)]
    private static extern int shm_open(string name, int flag, int mode);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern IntPtr mmap(IntPtr addr, uint size, int prot, int flags, int fd, int offset);

    [DllImport("libc", SetLastError = true)]
    private static extern int munmap(IntPtr addr, uint size);


    IntPtr addr;
    int[] values;

    
    short yaw, pitch, roll, throttle;
    bool arm;
    bool lastArmState = false;

    [SerializeField] private Rigidbody rb;

    public const float PI = 3.14159f;
    public const float RHO = 1.225f;

    private double const1 = PI * RHO * Mathf.Pow(0.0762f, 2) * 0.0635 / (3600 * 4 * 30);
    float RPMtoThrust(int val) {
        return (float) (const1 * Mathf.Pow(val, 2));
    }

    float intToDegPerSec(short val) {
        return (float) (0.02534762f*val + 0.02909812f);
    }

    int CRSFtoRPM(short val) {
        return (int) (82452.83 + (-12057.32 - 82452.83)/(1 + Mathf.Pow((float) (val/1776.103), 3.043331f)));
    }

    short intToCRSF(int val) {
        return (short) (0.01890788f*val + 1500.051f);
    }

    void Start() {
        int fd = shm_open("/myshm", O_RDONLY, 0);
        if (fd == -1) {
            Console.WriteLine("Error opening shared memory object");
            return;
        }

        addr = mmap(IntPtr.Zero, sizeof(int) * 16, PROT_READ, MAP_SHARED, fd, 0);
        if (addr == (IntPtr)(-1)) {
            Console.WriteLine("Error mapping shared memory object");
            close(fd);
            return;
        }

        values = new int[16];
    }

    void FixedUpdate() {
        if(arm) {
            float Thrust = 4 * RPMtoThrust(CRSFtoRPM(intToCRSF(throttle)));
            float Pitch = intToDegPerSec(pitch);
            float Roll = intToDegPerSec(roll);
            float Yaw = intToDegPerSec(yaw);

            Debug.Log("Thrust: " + Thrust);
            Debug.Log("Pitch: " + Pitch);
            Debug.Log("Roll: " + Roll);
            Debug.Log("Yaw: " + Yaw);

            // rb.AddRelativeForce(transform.up * Thrust);
            // Rotate the rigidbody by the pitch, roll, and yaw degrees
            rb.rotation = Quaternion.Euler(Pitch, Yaw, Roll); // Z, X, Y

            print("Arm: " + arm);
        }
        if(arm != lastArmState) {
            lastArmState = arm;
            Debug.Log("Arm: " + arm);
        }
    }

    void Update() {
        Marshal.Copy(addr, values, 0, 16);

        yaw = (short)values[0];
        pitch = (short)values[1];
        roll = (short)values[2];
        throttle = (short)values[3];

        arm = values[6] == 1;

        /*
        Debug.Log("Yaw: " + yaw);
        Debug.Log("Pitch: " + pitch);
        Debug.Log("Roll: " + roll);
        Debug.Log("Throttle: " + throttle);
        Debug.Log("Arm: " + arm);
        Debug.Log("\n");
        */
    }
}
