# OmenHub Pro — Linux (подсветка)

Порт подсветки клавиатуры и нижней световой полосы. Тот же протокол, что в Windows-версии
(HID LampArray, usage page 0x59, VID 0D62 / PID 54BF), только через `/dev/hidraw*` вместо `hid.dll`.
Только Python 3 + (для окна) GTK 3 из пакета `python3-gi`; больше ничего не нужно.

Вентиляторы, питание, оверлей, FPS в порт не входят — на Linux этим занимаются ядро (`hp-wmi`, `platform_profile`) и другие утилиты.

## Установка
```
linux/packaging/install.sh
sudo cp linux/packaging/70-omenhub.rules /etc/udev/rules.d/ && sudo udevadm control --reload && sudo udevadm trigger --subsystem-match=hidraw
systemctl --user enable --now omenhub-lighting      # автозапуск без окна
```
Правило udev нужно, чтобы обычный пользователь мог писать в hidraw клавиатуры (`TAG+="uaccess"`). Без него демон пишет
`no access to the keyboard hidraw node`.

## Использование
```
omenhub-lighting gui                          # окно: эффект, цвета, яркость, скорость, полоса, пресеты
omenhub-lighting set --effect rainbow --brightness 70 --speed 6
omenhub-lighting set --color ff8a1f           # цвет (включает клавиатуру, если она была выключена)
omenhub-lighting bar --zones ff0000 00ff00 0000ff ffffff
omenhub-lighting bar --off
omenhub-lighting preset apply Multi           # пресеты из ../presets (Windows) копируются в ~/.config/omenhub/presets
omenhub-lighting test --seconds 10            # крутит эффект в этом процессе
```
Эффекты: `off static gradient breathing rainbow rainbow-cycle color-wave pulse twinkle scanner fire`.
Настройки — `~/.config/omenhub/lighting.json`; запущенный демон подхватывает изменения сам. Окно запускает движок само —
не держите одновременно окно и демон (оба пишут в одно устройство): остановите демон `systemctl --user stop omenhub-lighting`.

## Отличия от Windows
- Полоса рисуется только через HID-лампы 103..119 (4 зоны растянуты на 17 ламп). На Windows ещё дописывается цветовая таблица
  BIOS (WMI `hpqBIntM`, команда 0x20009), чтобы цвет полосы переживал закрытие приложения; в Linux у пользовательского
  процесса нет доступа к этому WMI-методу (его вызывает только ядро), поэтому после остановки движка полоса возвращается к
  собственному эффекту клавиатуры.
- При остановке клавиатура возвращается в автономный режим (как и в Windows).
