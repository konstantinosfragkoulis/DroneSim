using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class CameraStreamer : MonoBehaviour {

    [SerializeField] private RenderTexture renderTexture;

    private const int O_RDONLY = 0x0000;
    private const int O_CREAT = 0x0040;
    private const int O_RDWR = 0x0002;
    private const int PROT_READ = 0x1;
    private const int PROT_WRITE = 0x2;
    private const int MAP_SHARED = 0x01;

    // Define external functions
    [DllImport("libc", SetLastError = true)]
    private static extern int shm_open(string name, int flag, int mode);

    [DllImport("libc", SetLastError = true)]
    private static extern int ftruncate(int fd, long length);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern IntPtr mmap(IntPtr addr, uint size, int prot, int flags, int fd, int offset);

    [DllImport("libc", SetLastError = true)]
    private static extern int munmap(IntPtr addr, uint size);
    
    public void WriteToSharedMemory(string name, byte[] data) {
        int fd = shm_open(name, O_CREAT | O_RDWR, 0666);
        if (fd == -1) {
            throw new InvalidOperationException("Unable to open shared memory.");
        }

        ftruncate(fd, data.Length);

        IntPtr addr = mmap(IntPtr.Zero, (uint)data.Length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
        if (addr == IntPtr.Zero) {
            throw new InvalidOperationException("Unable to map shared memory.");
        }

        Marshal.Copy(data, 0, addr, data.Length);

        if (munmap(addr, (uint)data.Length) == -1) {
            throw new InvalidOperationException("Unable to unmap shared memory.");
        }

        if (close(fd) == -1) {
            throw new InvalidOperationException("Unable to close shared memory.");
        }
    }

    public byte[] RenderTextureToByteArray(RenderTexture renderTexture) {
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        return texture2D.EncodeToPNG();
    }

    void Update() {
        byte[] data = RenderTextureToByteArray(renderTexture);
        WriteToSharedMemory("/virtCamMem", data);
    }
}