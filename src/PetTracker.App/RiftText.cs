namespace PetTracker.App;
public static class RiftText
{
    private static readonly Dictionary<string,string[]> Texts=new()
    {
        ["times"]=["Spacetime Rift Zamanları","Spacetime Rift Times","Spacetime-Rift-Zeiten","Время Spacetime Rift","Horarios de Spacetime Rift","Horaires Spacetime Rift","Horários de Spacetime Rift","أوقات شق الزمكان","स्पेसटाइम रिफ्ट समय","时空裂隙时间"],
        ["alerts"]=["Spacerift bildirimleri","Spacerift alerts","Spacerift-Benachrichtigungen","Уведомления о разломе","Avisos de Spacerift","Alertes Spacerift","Alertas de Spacerift","تنبيهات الشق","स्पेसरिफ्ट सूचनाएँ","时空裂隙提醒"],
        ["sound"]=["Bildirim sesi","Notification sound","Benachrichtigungston","Звук уведомления","Sonido de aviso","Son de notification","Som da notificação","صوت التنبيه","सूचना ध्वनि","提醒声音"],
        ["lead"]=["Kaç dakika önce?","Minutes before opening","Minuten vor Öffnung","Минут до открытия","Minutos antes de abrir","Minutes avant l’ouverture","Minutos antes de abrir","الدقائق قبل الفتح","खुलने से कितने मिनट पहले","提前几分钟"],
        ["left"]=["{0} dakika kaldı","{0} minutes left","Noch {0} Minuten","Осталось {0} мин.","Faltan {0} minutos","Encore {0} minutes","Faltam {0} minutos","باقي {0} دقائق","{0} मिनट शेष","还剩 {0} 分钟"],
        ["opened"]=["Rift Açıldı","Rift Opened","Rift geöffnet","Разлом открыт","Rift abierto","Rift ouvert","Rift aberto","فُتح الصدع","रिफ्ट खुला","裂隙已开启"],
        ["waiting"]=["Sunucu kimliği bekleniyor; bildirim gönderilmez.","Waiting for a supported server identity; no alerts.","Warte auf unterstützten Server; keine Meldungen.","Ожидание известного сервера; уведомлений нет.","Esperando un servidor compatible; sin avisos.","Serveur compatible attendu ; aucune alerte.","Aguardando servidor compatível; sem alertas.","بانتظار هوية خادم مدعوم؛ لا تنبيهات.","समर्थित सर्वर की पहचान की प्रतीक्षा; कोई सूचना नहीं।","等待支持的服务器身份；不发送提醒。"],
        ["reference"]=["Shugo.GG takvimi · canlı spawn doğrulaması değildir.","Shugo.GG schedule · not a live spawn confirmation.","Shugo.GG-Zeitplan · keine Live-Spawn-Bestätigung.","Расписание Shugo.GG · не подтверждение спавна.","Horario Shugo.GG · no confirma un spawn en vivo.","Calendrier Shugo.GG · pas une apparition confirmée.","Agenda Shugo.GG · não confirma spawn ao vivo.","جدول Shugo.GG؛ ليس تأكيد ظهور مباشر.","Shugo.GG समय-सारणी; लाइव स्पॉन पुष्टि नहीं।","Shugo.GG 时间表 · 非实时刷新确认。"],
        ["test"]=["Ön uyarıyı dene","Test reminder","Erinnerung testen","Проверить напоминание","Probar recordatorio","Tester le rappel","Testar lembrete","اختبار التذكير","रिमाइंडर जाँचें","测试预告"],
        ["testOpen"]=["Açılış uyarısını dene","Test opening alert","Öffnungsalarm testen","Проверить открытие","Probar aviso de apertura","Tester l’ouverture","Testar abertura","اختبار تنبيه الفتح","खुलने की सूचना जाँचें","测试开启提醒"],
    };
    public static string Get(string lang,string key,params object[] args)
    {var i=Array.FindIndex(UiText.Languages,l=>l.Code==lang);return string.Format(Texts[key][i<0?1:i],args);}
}
