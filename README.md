# Sultan's Ultimate Windows Optimizer - v1.0

[**العربية (Arabic Description Below)**](#أداة-سلطان-لتحسين-الويندوز-الشاملة-sultans-optimizer)

Developed by **0xSultan** (Discord: `5e_d` | GitHub: [0xSultan-dev](https://github.com/0xSultan-dev))

A powerful, light-weight, and **100% reversible** Windows optimization tool written in C#. Designed to maximize gaming, CPU, and GPU performance, stabilize frametimes, reduce input lag, and significantly decrease background resource (CPU/RAM/Disk) consumption.

Unlike typical optimizer tools that make permanent, irreversible changes to your system, this optimizer features a robust, interactive backup and restore engine that captures your exact configuration before any tweak is applied, allowing you to rollback completely with a single click.

---

## 🚀 Key Features

### 1. Extreme CPU & GPU Gaming Tweaks
* **VBS & HVCI Disabling**: Disables Virtualization-Based Security (VBS) and Hypervisor-Protected Code Integrity (HVCI) to release CPU capacity, adding up to 10% raw processor performance in games.
* **GPU MSI Mode & High Priority**: Automatically configures display adapters in MSI (Message Signaled Interrupts) mode and sets their interrupt priority to **High** to reduce DPC latency and eliminate micro-stutters.
* **Fullscreen Optimizations (FSE)**: Globally optimizes Fullscreen behavior to prevent Xbox GameDVR overlays from degrading performance.
* **Core Unparking & Latency Tuning**: Unparks CPU cores, disables HPET (High Precision Event Timer), and disables Dynamic Tick to stabilize processor cycle timings.

### 2. Deep Resource & Background Reduction
* **Telemetry & Diagnostic Disabling**: Stops telemetry services (`DiagTrack`, `dmwappushservice`) and disables compatibility/diagnostic scheduled tasks that cause random background CPU spikes.
* **Windows Search Indexer (`WSearch`) Disabling**: Disables the background search indexer to eliminate idle Disk and CPU usage.
* **Memory & Storage Optimizations**: Disables system memory compression to reduce CPU overhead, disables NTFS last-update logs, and hides widgets/chat/Copilot from the taskbar.

### 3. Integrated RAM & Temp Cleaner
* **Native Standby List Purger**: Employs Windows Native NT APIs (`NtSetSystemInformation`) to flush RAM standby lists and system caches instantly, reducing active memory footprint.
* **Deep Drive Optimizer**: Automates SSD TRIM and HDD defragmentation (`defrag.exe /O`).
* **Safe Temp File Cleaning**: Recursively clears user Temp, system Temp, and Prefetch directories, backing up deleted files before wiping them.

### 4. Symmetrical 100% Symmetrical Backup & Restore
* **Power Plan & Services**: Restores your exact active power plan GUID and service start statuses (Automatic/Manual/Disabled) dynamically.
* **Network & GPU Settings**: Reverts Network Adapter optimizations and restores original GPU MSI modes.
* **Boot Configuration (BCD)**: Restores the BCD boot store to its original state.
* **UWP Apps Restoration**: Automatically re-registers and reinstalls uninstalled pre-loaded UWP apps (e.g. Weather, News) directly from the Windows Store repository.
* **Files Restoration**: Restores deleted temporary files from clean backups back to their original folders.

---

## 🛠️ Build and Compilation

The tool has zero external dependencies and compiles into a single, compact executable using the native .NET Framework compiler (`csc.exe`) pre-installed on Windows.

1. Clone or download this repository.
2. Run `build.bat` as Administrator.
3. The compiled binary `UltimateTweakTool.exe` will be generated in the project directory.

---

## 📖 How to Use

> [!IMPORTANT]
> **Always run the executable as Administrator (Right click -> Run as Administrator) to grant the tool permissions to configure system drivers, services, and memory standby lists.**

1. Run **`UltimateTweakTool.exe`**.
2. Navigate through the sidebar tabs:
   * **Tweaks**: Toggle registry settings, performance tweaks, network configurations, and services disable actions categorized under system areas. Click **Apply Selected Tweaks** to optimize your system.
   * **Deep Cleaner**: Choose temporary files to remove, select **Purge RAM Standby List** and **Run Drive TRIM**, then click **Run System Cleanup** to execute with real-time logs.
   * **Startup Manager**: View active and inactive startup items. Select any item and click **Toggle Status** to enable/disable it.
   * **Game Profiles**: Enter a game executable name (e.g., `cs2.exe`) and set its CPU priority class and I/O priority level permanently.
   * **Backup & Restore**: View list of backups, create a custom backup, or restore system state and deleted temporary files instantly.

---

# أداة سلطان لتحسين الويندوز الشاملة (Sultan's Optimizer)

تطوير المبرمج **0xSultan** (حساب ديسكورد: `5e_d` | غيت هاب: [0xSultan-dev](https://github.com/0xSultan-dev))

أداة قوية، خفيفة الوزن، و**قابلة للتراجع بنسبة 100%** لتحسين نظام التشغيل Windows مكتوبة بلغة C#. صُممت خصيصاً لرفع أداء الألعاب، المعالج (CPU)، وكرت الشاشة (GPU)، وتثبيت سرعة الفريمات، وتقليل زمن الاستجابة (Input Lag)، وتقليص استهلاك الموارد بالخلفية بشكل ملحوظ.

على عكس الأدوات الأخرى التي تجري تغييرات دائمة ومخاطرة، تتميز **UltimateTweakTool** بنظام نسخ احتياطي تفاعلي يحفظ أدق إعدادات جهازك قبل التعديل، مما يتيح لك التراجع الكامل بضغطة زر واحدة.

---

## 🚀 الميزات الرئيسية

### 1. تعديلات الألعاب والأداء الأقصى
* **تعطيل VBS و HVCI**: إيقاف الأمان المستند إلى المحاكاة الافتراضية وسلامة الكود المحمي بمشرف الجلسة لتحرير كامل طاقة المعالج للألعاب، مما يمنح زيادة في أداء المعالج تصل إلى 10%.
* **تفعيل MSI Mode وأولوية كرت الشاشة القصوى**: تحويل معالج الرسوميات للعمل بنظام مقاطعات الرسائل وتخصيص أولوية قصوى له لتقليل زمن استجابة المنافذ والحد من التقطيع والـ Stuttering.
* **تحسين وضع ملء الشاشة (FSE)**: تعطيل تراكب الشاشة الافتراضي وتأثيرات Xbox GameDVR لضمان تركيز كرت الشاشة بالكامل على اللعبة.
* **إلغاء ركن الأنوية (Core Unparking)**: إلغاء ركن الأنوية وتعطيل الـ HPET والـ Dynamic Tick لاستقرار دورات وتوقيتات المعالج.

### 2. تخفيف استهلاك موارد النظام بالخلفية
* **إيقاف خدمات التتبع والتشخيص (Telemetry)**: إيقاف خدمات جمع البيانات والمهام المجدولة للتشخيصات لمنع الارتفاع المفاجئ للـ CPU في الخلفية.
* **تعطيل مفهرس بحث ويندوز (`WSearch`)**: إيقاف عمليات فهرسة البحث بالخلفية لتوفير سرعة المعالجة وقراءة الهارد ديسك.
* **تحسينات الذاكرة والتخزين**: إيقاف ضغط الذاكرة لتقليل الحمل على المعالج، وتعطيل تحديث آخر وقت وصول للملفات (NTFS Last Access)، وتنظيف شريط المهام من الأيقونات غير الهامة (Widgets, Copilot, Chat).

### 3. منظف مدمج للـ RAM والملفات المؤقتة
* **مفرغ الذاكرة المؤقتة (Standby List)**: تفريغ ذاكرة الكاش الخاصة بالـ RAM واسترجاع المساحة الضائعة فوراً باستخدام نظام الـ API المباشر لويندوز.
* **أداة تحسين الأقراص**: تشغيل TRIM التلقائي لأقراص الـ SSD وإعادة تجزئة الـ HDD.
* **تنظيف الملفات المؤقتة الآمن**: مسح ملفات Temp و Prefetch مع أخذ نسخة احتياطية لها قبل المسح.

### 4. استرجاع وتراجع متناظر 100%
* **خدمات النظام ووضع الطاقة**: استعادة خطة الطاقة الأصلية ونوع بدء تشغيل الخدمات بدقة.
* **إعدادات الشبكة وكرت الشاشة**: إلغاء تعديلات كرت الشبكة وإرجاع MSI كرت الشاشة لوضعه السابق.
* **ملفات الإقلاع (BCD)**: استيراد نسخة الإقلاع الاحتياطية وإلغاء تعديلات توقيت النواة.
* **إعادة تثبيت تطبيقات ويندوز (UWP)**: إعادة تسجيل وتثبيت كافة تطبيقات متجر ويندوز المحذوفة تلقائياً.
* **استرجاع الملفات**: إرجاع كافة ملفات Temp الممسوحة لمجلداتها الأصلية.

---

## 📖 طريقة الاستخدام

> [!IMPORTANT]
> **يجب دائماً تشغيل الأداة كمسؤول (Run as Administrator) لضمان عمل كافة صلاحيات النظام وامتيازات الـ RAM.**

1. قم بتشغيل **`UltimateTweakTool.exe`**.
2. تنقل عبر التبويبات في واجهة الأداة الرسومية:
   * **Tweaks (التعديلات)**: اختر التعديلات التي تود تطبيقها. اضغط على **Apply Selected Tweaks** لبدء تطبيق التعديلات مقسمة حسب الفئات.
   * **Deep Cleaner (المنظف العميق)**: حدد المجلدات المؤقتة التي تريد مسحها وتفريغ ذاكرة الـ RAM المؤقتة أو عمل TRIM للقرص. اضغط على **Run System Cleanup** لبدء التنظيف وعرض السجلات بشكل مباشر.
   * **Startup Manager (إدارة التشغيل التلقائي)**: استعرض جميع البرامج التي تعمل مع بداية تشغيل الجهاز وقم بتفعيلها أو تعطيلها بنقرة زر.
   * **Game Profiles (ملفات الألعاب)**: أضف أي ملف تشغيلي للعبة (مثل `cs2.exe`) لتطبيق أولوية CPU عالية وأولوية قراءة/كتابة قرص عالية بشكل دائم.
   * **Backup & Restore (النسخ الاحتياطي والاستعادة)**: استعرض قائمة النسخ الاحتياطية المتاحة، أو قم بإنشاء نسخة احتياطية فورية، أو استرجع إعدادات النظام أو الملفات المحذوفة بضغطة زر.

