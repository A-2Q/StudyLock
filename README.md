<p align="center">
  <img src="StudyLock/Assets/logo.png" alt="StudyLock Logo" width="130" />
</p>

<h1 align="center">StudyLock</h1>

<p align="center">
  <a href="https://github.com/A-2Q/StudyLock/releases/tag/v1.0.0">
    <img src="https://img.shields.io/badge/Tag-v1.0.0-10B981?style=for-the-badge&logo=github&logoColor=white" alt="Release Tag v1.0.0" />
  </a>
  <a href="https://github.com/A-2Q/StudyLock/releases">
    <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Android-3B82F6?style=for-the-badge&logo=android&logoColor=white" alt="Platforms" />
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/License-Apache_2.0-F59E0B?style=for-the-badge&logo=apache&logoColor=white" alt="Apache License 2.0" />
  </a>
  <a href="https://discord.gg/PufJHjaAKJ">
    <img src="https://img.shields.io/badge/Discord-Join%20Community-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord Server" />
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET%208-WPF-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8 WPF" />
  <img src="https://img.shields.io/badge/Flutter-3.22-02569B?style=flat-square&logo=flutter&logoColor=white" alt="Flutter" />
  <img src="https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/Dart-0175C2?style=flat-square&logo=dart&logoColor=white" alt="Dart" />
  <img src="https://img.shields.io/badge/Kotlin-7F52FF?style=flat-square&logo=kotlin&logoColor=white" alt="Kotlin" />
  <img src="https://img.shields.io/badge/Tests-15%2F15%20Passing-10B981?style=flat-square&logo=checkmarx&logoColor=white" alt="Tests" />
  <a href="https://github.com/A-2Q">
    <img src="https://img.shields.io/badge/Author-A--2Q-181717?style=flat-square&logo=github&logoColor=white" alt="Author A-2Q" />
  </a>
</p>

**StudyLock** هو تطبيق متكامل متوفر لإصداري **سطح المكتب (Windows PC)** و**الهاتف المحمول (Android)**، صُمّم لتوفير بيئة قفل ذكية وآمنة لكلٍّ من **المذاكرة المركّزة** و**استخدام الأطفال الآمن** دون أي مشتتات.

---

## 📱 الإصدارات المتوفرة (Platforms)

### 1. نسخة الكمبيوتر (Windows Desktop)
مبنية بـ **C# (.NET 8)** و **WPF** ومحرك **Microsoft Edge WebView2** المدمج، متوفرة داخل مجلد `StudyLock/`.

### 2. نسخة الهاتف المحمول (Android Mobile)
مبنية بـ **Flutter (Dart) + Kotlin** مع محرك **`flutter_inappwebview`** وربط أصيل لخدمات قفل النظام (Kiosk Mode / Accessibility / System Alert Window Overlay)، متوفرة داخل مجلد `studylock_mobile/`.

---

## 💡 قصة المشروع والحقوق (About & Credits)

- **الفكرة والتصور:** فكرة هذا المشروع وتحديد ميزاته ومتطلباته بالكامل من قِبلي أنا (**A-2Q**)، وجاءت ببساطة لأن **أكثر مشكلة أواجهها أثناء المذاكرة هي التشتت**، فقررت ابتكار أداة عملية تحصرني داخل بيئة المذاكرة بذكاء ومرونة دون أي مشتّتات.
- **البناء والتنفيذ:** تم بناء هذا المشروع وتطوير كوده بالكامل بواسطة الذكاء الاصطناعي **Gemini 3.8 Flash** عبر أسلوب **فايب كودينق (Vibe Coding)**.
- **الحقوق والمجتمع (Discord):** جميع الحقوق محفوظة لـ **A-2Q** — انضم لسيرفر الديسكورد الرسمي: **[https://discord.gg/PufJHjaAKJ](https://discord.gg/PufJHjaAKJ)**

---

## ✨ أبرز المزايا (Key Features)

### 1. وضع المذاكرة (Study Mode)
- **متصفح مقيد بالنطاق مع تبويبات داخلية:** تصفح موقع المذاكرة الأساسي ونطاقاته الفرعية فقط مع حظر الروابط الخارجية والنوافذ المنبثقة تلقائياً.
- **إرفاق ملفات PDF المحلية + تقسيم الشاشة (`Split View`):** افتح كتبك وملفات الـ PDF بجانب منصة الشرح مع فاصل قابل للسحب، وتعمل ملفات PDF حتى عند انقطاع الإنترنت.
- **المعاينة وتسجيل الدخول قبل القفل + دعم OAuth:** سجّل دخولك عبر Google/Microsoft/Nafath في بروفايل WebView2 دائم قبل بدء القفل، مع قائمة بيضاء تقنية لبوابات الدخول.
- **مهلة تراجع 30 ثانية:** للتأكد من فتح الموقع بشكل سليم قبل إحكام القفل أو تفعيل الوضع الصارم.
- **تحقق إثبات الوجود كل 15 دقيقة (`Anti-AFK`):** يُخرج مشغل الفيديو من وضع ملء الشاشة تلقائياً عند حلول الموعد، مع مهلة 60 ثانية وتجميد عادل للمؤقت بدون عقوبة عند عدم الاستجابة.
- **إيقاف مؤقت بدون عقوبة (`Pause / Resume`):** لتجميد العدّاد عند الصلاة أو الظروف الطارئة مع بقاء الشاشة مقفلة.
- **أدوات مساعدة مدمجة (Airspace-Safe):** مسودة ملاحظات سريعة (تُحفظ تلقائياً بسجل الجلسة)، آلة حاسبة، و**لوحة Wi-Fi مدمجة عبر `wlanapi.dll`** لتبديل الشبكة دون الخروج من القفل.
- **مخرج الطوارئ:** سلسلة عشوائية آمنة من 55 رمزاً مع تعطيل النسخ واللصق بالكامل.

### 2. مستويات الحماية وتعدد الشاشات
- **تعتيم الشاشات المتعددة (`MultiMonitorService`):** يعتّم أي شاشة إضافية تلقائياً (حتى عند توصيلها أثناء الجلسة).
- **حظر اختصارات النظام (`WindowsHookService`):** يعترض `Win`, `Alt+Tab`, `Alt+F4`, `Ctrl+Esc`, `Ctrl+Shift+Esc`, `Win+D/Tab/E/R` مع حارس تركيز كل `500ms`.
- **نموذج الصلاحيات الهجين (`Standard` vs `Strict`):**
  - **قياسي (Standard):** يعمل بصلاحيات مستخدم عادي بدون نافذة UAC.
  - **صارم (Strict):** يرفع الصلاحيات تلقائياً لإغلاق `Taskmgr.exe`، ويشفّر حالة الجلسة عبر **Windows DPAPI** (`session.lock`)، ويسجّل مهمة إقلاع عالية الصلاحية في **Task Scheduler** للاستئناف الإجباري بعد إعادة التشغيل (تُحذف تلقائياً عند انتهاء الجلسة).

### 3. وضع الطفل وبوابة الوالدين (Child Mode & Parent Gate)
- بطاقات مواقع جاهزة قابلة للتخصيص والإضافة، وبروفايل WebView2 معزول تماماً عن حسابات الوالدين.
- حظر صامت للروابط الخارجية والنوافذ المنبثقة، وشاشة هادئة عند انتهاء الوقت لا تُفتح إلا عبر **بوابة الوالدين**.
- **بوابة الوالدين:** مسائل جمع وطرح سهلة بالعشرات والخمسات (مثل `20 - 10 - 5 = ?`) مع حماية تجميد دقيقتين بعد 3 أخطاء، بالإضافة إلى خيار تفعيل **رمز PIN سري** مشفّر بـ `SHA-256 + Salt`.

### 4. تجربة مستخدم احترافية
- تبديل فوري بين **العربية (`RTL`) والإنجليزية (`LTR`)**.
- تبديل فوري بين **الوضع الداكن (`OLED Charcoal & Sunrise Gold`)** و**الوضع الفاتح**.
- تبويب **إحصائيات وإنجاز** متكامل (ساعات اليوم، الأسبوع، الإجمالي، متوسط سرعة الاستجابة، واستعراض/تصدير ملاحظات الجلسات).

---

## 🛠️ المتطلبات والبناء (Build & Run)

### بناء نسخة الكمبيوتر (Windows Desktop)
- Windows 10 / 11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft Edge WebView2 Runtime (مدمج في Windows 10/11)

```powershell
# استعادة الحزم وبناء المشروع
dotnet build StudyLock.sln -c Release

# تشغيل اختبارات الوحدات الآلية (xUnit)
dotnet test StudyLock.Tests/StudyLock.Tests.csproj -c Release

# نشر ملف تنفيذي واحد مستقل (Single-File .exe)
dotnet publish StudyLock/StudyLock.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

### بناء نسخة الأندرويد (Android Mobile)
- [Flutter SDK (>=3.16.0)](https://flutter.dev)
- Android SDK (API 34) & Java JDK 17

```bash
cd studylock_mobile
flutter pub get
flutter test
flutter build apk --release
```
