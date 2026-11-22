# استخراج ملف EXE عبر GitHub Actions (بدون تثبيت أدوات على جهازك)

1. أنشئ مستودع GitHub جديداً وارفع محتويات هذا المجلد إليه.
2. افتح تبويب **Actions** في المستودع.
3. اختر **Build Windows EXE** ثم **Run workflow**.
4. بعد نجاح البناء، افتح نتيجة التشغيل (Workflow run).
5. من قسم **Artifacts** نزّل `WiFiHotspotManager-win-x64`.
6. فك ضغط الملف وشغّل `WiFiHotspotManager.exe`.

يمكن أيضاً البناء محلياً بتشغيل `BUILD_EXE_WINDOWS.ps1` بعد تثبيت .NET 8 SDK.

ملاحظة: ملف EXE لم يُبنَ أو يُختبر بعد على جهاز Windows فعلي. هذا الإجراء ينشئه على عامل Windows لدى GitHub، ثم يجب اختباره على جهازك.
