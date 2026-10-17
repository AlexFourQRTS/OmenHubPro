using System;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using Windows.Foundation;
using Windows.UI;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>Paints Windows Dynamic Lighting devices (HID LampArray) with a yellow / blue / white strobe.</summary>
    public static class LampArrayPainter
    {
        private const int StrobeStepMilliseconds = 500;
        private const int PollMilliseconds = 10;
        private const int EnumerationTimeoutMilliseconds = 8000;
        private const string VirtualDeviceMarker = "VHF";

        public static string Strobe(bool virtualDevice, int seconds, int openTimeoutMilliseconds)
        {
            string report = "";
            DeviceInformationCollection devices =
                WaitFor(DeviceInformation.FindAllAsync(LampArray.GetDeviceSelector()), EnumerationTimeoutMilliseconds);
            foreach (DeviceInformation device in devices)
            {
                bool isVirtual = device.Id.IndexOf(VirtualDeviceMarker, StringComparison.OrdinalIgnoreCase) >= 0;
                if (isVirtual != virtualDevice) continue;
                report += "найдено: " + ShortName(device.Id) + "; ";
                LampArray lampArray = WaitFor(LampArray.FromIdAsync(device.Id), openTimeoutMilliseconds);
                lampArray.IsEnabled = true;
                report += "открыто, ламп: " + lampArray.LampCount + "; мигание " + seconds + " с";
                StrobeArray(lampArray, seconds);
                return report;
            }
            return report + "устройство не найдено";
        }

        private static void StrobeArray(LampArray lampArray, int seconds)
        {
            Color[] sequence =
            {
                ColorHelper.FromArgb(255, 255, 220, 0),     // yellow
                ColorHelper.FromArgb(255, 0, 80, 255),      // blue
                ColorHelper.FromArgb(255, 255, 255, 255)    // white
            };
            DateTime endTime = DateTime.Now.AddSeconds(seconds);
            for (int step = 0; DateTime.Now < endTime; step++)
            {
                lampArray.SetColor(sequence[step % sequence.Length]);
                Thread.Sleep(StrobeStepMilliseconds);
            }
        }

        private static T WaitFor<T>(IAsyncOperation<T> operation, int timeoutMilliseconds)
        {
            int waited = 0;
            while (operation.Status == AsyncStatus.Started && waited < timeoutMilliseconds)
            {
                Thread.Sleep(PollMilliseconds);
                waited += PollMilliseconds;
            }
            if (operation.Status != AsyncStatus.Completed)
                throw new Exception("операция Windows не завершилась (" + operation.Status + ", " + waited + " мс)");
            return operation.GetResults();
        }

        private static string ShortName(string deviceId)
        {
            int start = deviceId.IndexOf("HID#", StringComparison.OrdinalIgnoreCase);
            return start < 0 ? deviceId : deviceId.Substring(start, Math.Min(34, deviceId.Length - start));
        }
    }
}
