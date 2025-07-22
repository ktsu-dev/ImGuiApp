# Font Update Script with Interactive TUI + Emoji Support

This PowerShell script automates downloading and installing Nerd Fonts for the ImGuiApp project, featuring a beautiful **Text User Interface (TUI)** for easy font selection. The script simplifies font management by selecting **one font variant** and installing it as `NerdFont.ttf` for automatic build detection. **Bonus:** Preserves manually placed emoji fonts like `NotoEmoji.ttf` for full emoji support! 😀🚀

## 🎨 Interactive Mode (TUI)

### Launch Interactive Font Selector
```powershell
.\scripts\Update-NerdFont.ps1
```

The interactive TUI provides:
- 📋 **Categorized font list** with descriptions and ratings  
- 🔍 **Font discovery** - browse all available Nerd Fonts
- 📊 **Current font info** - see what's installed and backed up
- 🌟 **Popularity ratings** and feature descriptions
- 🎯 **Smart recommendations** based on use cases
- 🎯 **Single variant selection** - choose one font file from all available weights/styles

### TUI Features

#### Font Categories:
- **Programming** - Optimized for coding (JetBrains Mono, Fira Code, Hack, Source Code Pro)
- **Modern** - Contemporary designs (Cascadia Code)  
- **Classic** - Traditional favorites (DejaVu Sans Mono, Inconsolata)
- **Linux** - OS-specific fonts (Ubuntu Mono)

#### Font Variant Selection:
After choosing a font family, you'll see all available variants (Regular, Medium, Bold, SemiBold, Light, etc.) and can select the one that suits your preference. The selected font will be installed as `NerdFont.ttf` for automatic build detection.

#### Additional Options:
- 🔄 **Refresh from GitHub** - See all available fonts in real-time
- 📋 **Current font info** - View installed fonts and backups
- ❌ **Exit** - Quit the application

## 🚀 Direct Installation (Non-Interactive)

### Install Specific Fonts
```powershell
# JetBrains Mono (recommended)
.\scripts\Update-NerdFont.ps1 -FontName "JetBrainsMono"

# Fira Code (with ligatures)
.\scripts\Update-NerdFont.ps1 -FontName "FiraCode"

# Cascadia Code (Microsoft)
.\scripts\Update-NerdFont.ps1 -FontName "CascadiaCode"

# Source Code Pro (Adobe)  
.\scripts\Update-NerdFont.ps1 -FontName "SourceCodePro"

# Hack (clean & readable)
.\scripts\Update-NerdFont.ps1 -FontName "Hack"
```

### Force Reinstall
```powershell
.\scripts\Update-NerdFont.ps1 -FontName "JetBrainsMono" -Force
```

### Force Interactive Mode
```powershell
.\scripts\Update-NerdFont.ps1 -Interactive
```

## ✨ What It Does

1. **📥 Downloads** the latest Nerd Font release from GitHub
2. **📦 Extracts** the font archive and presents filtered, useful variants
3. **🎯 Lets you select** one font variant (Regular, Medium, Bold, etc.)
4. **💾 Backs up** your current fonts to a timestamped folder
5. **📁 Installs** the selected font as `NerdFont.ttf` 
6. **😀 Preserves** any manually placed `NotoEmoji.ttf` for emoji support
7. **🧪 Tests** that the project still builds correctly
8. **🧹 Cleans up** temporary files

**Note:** The build system automatically handles both font files! Place monochrome emoji fonts manually.

## 🏗️ Standardized Build System

The script uses a **standardized naming convention** for fonts:

**Main Font:** `NerdFont.ttf` - Your selected programming font (auto-installed)
**Emoji Font:** `NotoEmoji.ttf` - Monochrome emoji font (manually placed)

This means:
- ✅ **Automatic Detection** - The build system looks for both standardized files
- ✅ **Simplified Resource Management** - Consistent resource entries
- ✅ **Easy Font Switching** - Replace main font, preserve emoji font
- ✅ **Complete Typography** - Programming symbols + emoji support
- ✅ **Consistent References** - `Resources.NerdFont` + `Resources.NotoEmoji`

## 🎯 Available Fonts

### Programming Fonts ⭐⭐⭐⭐⭐
- **JetBrains Mono** - Modern, clear character distinction (0/O, 1/l/I)
- **Fira Code** - Beautiful programming ligatures (-> != >=)
- **Source Code Pro** - Adobe's professional coding font
- **Hack** - Clean, highly legible, screen-optimized

### Modern Fonts ⭐⭐⭐⭐
- **Cascadia Code** - Microsoft's font with ligatures (Windows Terminal default)

### Classic Fonts ⭐⭐⭐
- **DejaVu Sans Mono** - Extensive Unicode support, familiar design
- **Inconsolata** - Humanist design, distinctive character shapes

### Linux Fonts ⭐⭐⭐
- **Ubuntu Mono** - Distinctive rounded design, Linux-native

## 🔧 Requirements

- PowerShell 5.1 or newer (PowerShell Core recommended)
- Internet connection for downloading
- Write access to `ImGuiApp/Resources/` folder

## 📁 Files Updated

The script replaces this file:
- `ImGuiApp/Resources/NerdFont.ttf` - Your selected programming font

**Preserves:** Manually placed fonts:
- `ImGuiApp/Resources/NotoEmoji.ttf` - Your monochrome emoji font (if present)

**Automatic:** The build system automatically handles everything else:
- `ImGuiApp/Resources/Resources.resx` - Resource references for both fonts
- `ImGuiApp/Resources/Resources.Designer.cs` - Generated resource code
- Application configuration already references the standardized font names

## 💾 Backup & Recovery

- Fonts are automatically backed up to `ImGuiApp/Resources/backup_YYYYMMDD_HHMMSS/`
- View backup info through the TUI's "Show current font info" option
- If something goes wrong, copy files from the backup folder back to `Resources/`
- Run `dotnet build` to regenerate resources if needed

## 🐛 Troubleshooting

### Script won't run
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Download fails
- Check internet connection
- Try again (GitHub API rate limiting)
- Use TUI's "Refresh from GitHub" to see available fonts
- Manually download from: https://github.com/ryanoasis/nerd-fonts/releases

### Build fails after font change
1. Use TUI to check current font info and backups
2. Restore from backup folder
3. Run `dotnet clean` then `dotnet build`
4. Check that `NerdFont.ttf` exists in `Resources/` folder

### Manual Font Installation
If the script fails, you can manually install fonts:
1. Download any Nerd Font TTF file
2. Rename it to `NerdFont.ttf`
3. Copy it to `ImGuiApp/Resources/`
4. **Optional:** Download a monochrome emoji font (like Noto Emoji monochrome)
5. **Optional:** Rename emoji font to `NotoEmoji.ttf` and place in `ImGuiApp/Resources/`
6. Run `dotnet build` (automatically handles everything else!)

## 🎮 TUI Navigation

### Interactive Mode Commands:
- **Numbers** - Select fonts and variants by number
- **Enter** - Use default selection (typically Medium or Regular)
- **Y/n** - Confirm installation
- **Special Options:**
  - 🔄 Refresh fonts from GitHub
  - 📋 Show current font information  
  - ❌ Exit application

### Font Selection Tips:
- **For general programming**: JetBrains Mono Medium or Fira Code Regular
- **For ligature lovers**: Fira Code Regular or Cascadia Code Regular  
- **For clean minimalism**: Hack Regular or Source Code Pro Regular
- **For readability**: Choose Medium weight variants when available
- **For Windows Terminal**: Cascadia Code Regular
- **For Linux environments**: Ubuntu Mono Regular

## 📘 Examples

### Interactive Mode (Recommended)
```powershell
# Launch beautiful TUI
.\scripts\Update-NerdFont.ps1

# Force interactive mode
.\scripts\Update-NerdFont.ps1 -Interactive
```

### Direct Installation
```powershell
# Get help
Get-Help .\scripts\Update-NerdFont.ps1 -Examples

# Quick install with variant selection
.\scripts\Update-NerdFont.ps1 -FontName "JetBrainsMono"

# Install and keep temp files for debugging
.\scripts\Update-NerdFont.ps1 -FontName "FiraCode" -Cleanup:$false

# Force reinstall even if font exists
.\scripts\Update-NerdFont.ps1 -FontName "Hack" -Force

# See what fonts are supported (shows error with list)
.\scripts\Update-NerdFont.ps1 -FontName "InvalidFont"
```

## 🎯 After Installation

1. **Build the project**: `dotnet build`
2. **Run the demo**: `dotnet run --project ImGuiAppDemo`  
3. **Check the "Nerd Fonts" tab** to see your new icons and emojis! 😀🚀
4. **Enjoy beautiful programming fonts** with full Nerd Font icon support + emojis!

## 🌟 TUI Experience

When you run the interactive mode, you'll see:

```
╔══════════════════════════════════════════════════════════════════════════════╗
║                        🎨 ImGuiApp Nerd Font Selector 🎨                     ║
║                         Choose Your Programming Font                         ║
╚══════════════════════════════════════════════════════════════════════════════╝
```

Then after selecting a font family:

```
╔══════════════════════════════════════════════════════════════════════════════╗
║                          📦 Font Variant Selection                           ║
║                    Select ONE variant to install for JetBrains Mono          ║
║                   The selected font will be saved as NerdFont.ttf            ║
╚══════════════════════════════════════════════════════════════════════════════╝

Available font variants:

   1. JetBrainsMonoNerdFont-Medium.ttf (142.3 KB) [recommended]
   2. JetBrainsMonoNerdFont-Regular.ttf (139.8 KB)
   3. JetBrainsMonoNerdFont-Bold.ttf (145.1 KB)
   4. JetBrainsMonoNerdFont-Light.ttf (137.2 KB)

💡 The selected font will be installed as 'NerdFont.ttf' for automatic build detection
😀 Any existing 'NotoEmoji.ttf' will be preserved for emoji support
```

Your new setup includes a beautiful programming font (`NerdFont.ttf`) and preserves any manually placed emoji fonts (`NotoEmoji.ttf`) - the perfect combination for modern development! 🚀✨😀 
