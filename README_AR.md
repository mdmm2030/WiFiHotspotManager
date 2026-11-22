# WiFi Hotspot Manager

نسخة أولية مفتوحة المصدر لبرنامج Windows 11 بواجهة ثنائية اللغة (عربي/إنجليزي) وتصميم WPF حديث.

## المتطلبات
- Windows 11 (يفضل تحديث حديث).
- .NET 8 SDK.
- اتصال إنترنت نشط عبر Wi-Fi أو Ethernet.
- محول Wi-Fi يدعم Windows Mobile Hotspot.
- لتطبيق قواعد Windows Firewall قد تحتاج تشغيل البرنامج بصلاحيات المسؤول.

## البناء
1. ثبّت .NET 8 SDK من Microsoft.
2. افتح `WiFiHotspotManager.sln` في Visual Studio 2022 أو افتح Terminal داخل مجلد المشروع.
3. نفّذ:
   ```powershell
   dotnet restore
   dotnet build -c Release
   ```
4. لتشغيله:
   ```powershell
   dotnet run --project .\WiFiHotspotManager\WiFiHotspotManager.csproj
   ```

## حدود النسخة الأولى (مهم)
- يبدأ ويوقف نقطة الاتصال باستخدام Windows NetworkOperatorTetheringManager API.
- اكتشاف العملاء يعتمد على قائمة Windows Tethering clients، وقد لا تعيد كل المحولات اسم الجهاز أو MAC.
- ربط MAC بعنوان IP يتم بمحاولة مطابقة جدول ARP، وقد لا ينجح دائماً.
- زر الحظر ينشئ قاعدة Windows Firewall صادرة حسب عنوان IP. هذا **ليس حظراً مضموناً على مستوى نقطة الوصول**؛ قد لا يمنع حركة المرور المُمرّرة عبر ICS على بعض تعريفات/إصدارات Windows، وقد يتغير IP عند إعادة الاتصال.
- لا يوجد في واجهة Windows Mobile Hotspot العامة API موثوق لحظر عميل Wi-Fi بعينه بواسطة MAC. لذلك لا نعرض هذا النموذج الأولي على أنه بديل كامل لخاصية MAC filtering في الراوتر.
- لا يوجد حالياً حفظ لإعدادات اسم الشبكة وكلمة المرور في ملف التطبيق؛ تُرسل الإعدادات إلى Windows API عند التطبيق/التشغيل.
- لا تشارك كلمة مرور الشبكة في لقطات الشاشة أو السجلات.

## الخطوة التالية
اختبار Start/Stop وظهور الأجهزة على جهاز Windows 11 الفعلي، ثم تحسين اكتشاف العملاء. إذا كان الحظر الفردي شرطاً أساسياً، فالطريقة الموثوقة تكون عبر نقطة وصول/راوتر يدعم Client isolation أو MAC access control، أو تنفيذ حل شبكي متقدم يعتمد على WFP/driver بعد اختبار توافقه.
