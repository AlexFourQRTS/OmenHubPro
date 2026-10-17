using System;
using System.Collections.Generic;
using OmenHub.Core.Logging;

namespace OmenHub.Hardware.Lighting
{
    /// <summary>The numbered list of lighting experiments. The user presses a variant and reports which one visibly worked.</summary>
    public static class LightingVariantCatalog
    {
        private const int StrobeSeconds = 6;
        private const int QuickOpenTimeoutMilliseconds = 6000;
        private const int SlowOpenTimeoutMilliseconds = 25000;

        public static List<LightingVariant> Build()
        {
            var variants = new List<LightingVariant>();
            Add(variants, "Windows-лампы: виртуальное устройство (4 лампы)",
                () => LampArrayPainter.Strobe(true, StrobeSeconds, QuickOpenTimeoutMilliseconds));
            Add(variants, "Windows-лампы: клавиатура HP",
                () => LampArrayPainter.Strobe(false, StrobeSeconds, SlowOpenTimeoutMilliseconds));
            Add(variants, "Закрыть фон OMEN, затем Windows-лампы: клавиатура",
                () => OmenBackgroundControl.StopOmenBackground() + "; "
                    + LampArrayPainter.Strobe(false, StrobeSeconds, SlowOpenTimeoutMilliseconds));
            Add(variants, "Выключить «окружающее освещение» Windows, затем клавиатура",
                () => StrobeWithoutAmbient());
            Add(variants, "BIOS: 4 зоны, порядок цветов RGB",
                () => BiosKeyboardPainter.StrobeZones(false, 2));
            Add(variants, "BIOS: 4 зоны, порядок цветов BGR",
                () => BiosKeyboardPainter.StrobeZones(true, 2));
            Add(variants, "BIOS: подсветка выкл ↔ вкл",
                () => BiosKeyboardPainter.BlinkBacklight(2));
            Add(variants, "Диагностика BIOS: показать таблицу цветов",
                () => BiosKeyboardPainter.DumpColorTable());
            Add(variants, "Диагностика HID: прочитать отчёты клавиатуры",
                () => HidDiagnostics.ReadFeatureReports());
            Add(variants, "HID напрямую: вся клавиатура жёлтый/синий/белый",
                () => DirectLampExperiments.StrobeWholeKeyboard());
            Add(variants, "HID напрямую: радуга по каждой клавише",
                () => DirectLampExperiments.RainbowPerKey());
            Add(variants, "Закрыть фон OMEN, затем BIOS: 4 зоны RGB",
                () => OmenBackgroundControl.StopOmenBackground() + "; " + BiosKeyboardPainter.StrobeZones(false, 2));
            return variants;
        }

        private static string StrobeWithoutAmbient()
        {
            int previous = OmenBackgroundControl.SetWindowsAmbientLighting(false);
            try { return LampArrayPainter.Strobe(false, StrobeSeconds, SlowOpenTimeoutMilliseconds); }
            finally { OmenBackgroundControl.SetWindowsAmbientLighting(previous != 0); }
        }

        private static void Add(List<LightingVariant> variants, string title, Func<string> execute)
        {
            int number = variants.Count + 1;
            variants.Add(new LightingVariant
            {
                Number = number,
                Title = title,
                Execute = () =>
                {
                    AppLog.Write("LIGHTING variant " + number + " started: " + title);
                    string result;
                    try { result = execute(); }
                    catch (Exception error) { result = "ОШИБКА: " + error.Message; }
                    AppLog.Write("LIGHTING variant " + number + " result: " + result);
                    return result;
                }
            });
        }
    }
}
