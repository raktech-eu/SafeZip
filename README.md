# SafeZip v0.5

SafeZip is a streamlined, drag-and-drop Windows utility designed to create and immediately verify ZIP archives. Built with C# and Windows Forms, it acts as a polished graphical frontend for 7-Zip, ensuring that your archived files are safely written and fully intact.

## Download

**[⬇ Download latest release](https://github.com/raktech-eu/SafeZip/releases)**

---

## Features

- **Drag, Drop, Done:** Drop files or folders directly into the app zone to instantly begin archiving.
- **Automatic Verification:** Every ZIP file created is immediately tested for integrity using 7-Zip's built-in testing functionality.
- **Batch Processing:** Select or drop multiple files/folders at once, and SafeZip will queue and process them sequentially.
- **Real-time Activity Log:** A built-in terminal-style log shows you exactly what is happening, including success/failure states and exit codes.
- **Adaptive Theming:** Automatically detects your Windows system theme and applies a custom Light or Dark UI.
- **Store-Only Archiving:** Creates ZIPs with a compression level of 0 (`-mx=0`) to maximize data preservation. If a standard compressed ZIP gets corrupted, the entire archive can fail to extract because the decompression algorithm relies on previous data. Uncompressed archives lay out files sequentially; if a sector gets damaged, you usually only lose the specific file in that sector, while the rest remain perfectly recoverable. This also optimizes for speed by not spending CPU cycles on compression.

---

## Prerequisites

- **Windows x64 OS:** The application relies on Windows Forms and the Windows Registry (for theme detection).
- **7-Zip x64:** SafeZip requires 7-Zip to be installed on your system. By default, it looks for the executable at: `C:\Program Files\7-Zip\7z.exe`

---

## How to Use

1. **Launch the App:** Run `SafeZip.exe`.
2. **Set Output Directory:** Click "Output Dir..." to choose where your generated ZIP files will be saved (defaults to the application's base directory).
3. **Select Files:** Either click "Browse..." to pick files manually, or simply drag and drop files/folders into the dashed drop zone.
4. **Monitor Progress:** Watch the Activity Log. SafeZip will create a `.zip` archive for each item and run a verification test right after.

---

## Technical Details

- **Language/Framework:** C# / .NET Windows Forms
- **Archiving Engine:** `7z.exe` (called via `System.Diagnostics.Process`)
- **Custom UI:** Uses a borderless, artifact-free custom GDI+ drawing system for buttons and drop zones to provide a modern, flat look.
- **Self-Contained:** The application icon and this README are embedded as manifest resources at compile time, so no external files are required to run it.

---

## License

Copyright (c) 2026 Ioannis Karras.

SafeZip is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This software uses [7-Zip](https://www.7-zip.org), which is licensed under the GNU LGPL license by Igor Pavlov. SafeZip is an independent program and is not affiliated with the 7-Zip project.

---

## Disclaimer

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
