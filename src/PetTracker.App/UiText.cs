namespace PetTracker.App;

public static class UiText
{
    public static readonly (string Code,string Name)[] Languages =
    [("tr","Türkçe"),("en","English"),("de","Deutsch"),("ru","Русский"),("es","Español"),("fr","Français"),("pt","Português"),("ar","العربية"),("hi","हिन्दी"),("zh","简体中文")];
    private static readonly Dictionary<string,string[]> Strings = new()
    {
        ["resyncWarning"]=["Sayaç hatalı olabilir.\nDoğrulamak için karakter seçimine dön.","Pet Tracker may be inaccurate.\nReturn to Character Select to resync.","Zähler möglicherweise ungenau.\nZur Charakterauswahl zurückkehren.","Счётчик может быть неточным.\nВернитесь к выбору персонажа.","El contador puede ser inexacto.\nVuelve a seleccionar personaje.","Le compteur peut être inexact.\nRevenez à la sélection du personnage.","O contador pode estar incorreto.\nVolte à seleção de personagem.","قد تكون الأعداد غير دقيقة.\nعد إلى اختيار الشخصية للمزامنة.","गिनती गलत हो सकती है।\nदोबारा सिंक करने के लिए चरित्र चयन पर जाएँ।","计数可能不准确。\n请返回角色选择界面重新同步。"],
        ["unverified"]=["Doğrulanmadı","Unverified","Unbestätigt","Не подтверждено","Sin verificar","Non vérifié","Não verificado","غير مؤكد","अपुष्ट","未验证"],
        ["moveUp"]=["Kartı yukarı taşı","Move card up","Karte nach oben","Поднять карточку","Subir tarjeta","Monter la carte","Mover cartão para cima","نقل البطاقة للأعلى","कार्ड ऊपर ले जाएँ","上移卡片"],
        ["moveDown"]=["Kartı aşağı taşı","Move card down","Karte nach unten","Опустить карточку","Bajar tarjeta","Descendre la carte","Mover cartão para baixo","نقل البطاقة للأسفل","कार्ड नीचे ले जाएँ","下移卡片"],
        ["visibleCards"]=["Görünen pet sayısı","Visible pets","Sichtbare Pets","Видимые питомцы","Pets visibles","Pets visibles","Pets visíveis","الحيوانات الظاهرة","दिखने वाले पेट","显示宠物数量"],
        ["lockHeight"]=["Yüksekliği kilitle","Lock height","Höhe sperren","Зафиксировать высоту","Bloquear altura","Verrouiller la hauteur","Bloquear altura","قفل الارتفاع","ऊँचाई लॉक करें","锁定高度"],
        ["unlockHeight"]=["Otomatik büyümeye izin ver","Allow automatic growth","Automatisches Wachstum zulassen","Разрешить автоматический рост","Permitir crecimiento automático","Autoriser l’agrandissement automatique","Permitir crescimento automático","السماح بالزيادة التلقائية","अपने आप बढ़ने दें","允许自动增高"],
        ["compact"]=["Küçült","Compact","Kompakt","Компактно","Compactar","Réduire","Compactar","تصغير","छोटा करें","收起"],
        ["expand"]=["Tam pencereyi aç","Expand tracker","Tracker vergrößern","Развернуть трекер","Ampliar tracker","Agrandir le tracker","Expandir o tracker","تكبير المتتبع","ट्रैकर बड़ा करें","展开追踪器"],
        ["hideHotkey"]=["Gizle / göster kısayolu","Hide / show shortcut","Tastenkürzel Ausblenden / Anzeigen","Скрыть / показать","Atajo ocultar / mostrar","Raccourci masquer / afficher","Atalho ocultar / mostrar","اختصار إخفاء / إظهار","छिपाएँ / दिखाएँ शॉर्टकट","隐藏 / 显示快捷键"],
        ["compactHotkey"]=["Küçült / büyüt kısayolu","Compact / expand shortcut","Tastenkürzel Kompakt / Erweitern","Свернуть / развернуть","Atajo compactar / ampliar","Raccourci réduire / agrandir","Atalho compactar / expandir","اختصار تصغير / تكبير","छोटा / बड़ा शॉर्टकट","收起 / 展开快捷键"],
        ["pressKey"]=["Bir tuş kombinasyonuna bas…","Press a key combination…","Tastenkombination drücken…","Нажмите сочетание клавиш…","Pulsa una combinación…","Appuyez sur une combinaison…","Pressione uma combinação…","اضغط مجموعة مفاتيح…","कुंजी संयोजन दबाएँ…","请按下快捷键…"],
        ["hotkeysDifferent"]=["İki kısayol farklı olmalı.","The shortcuts must be different.","Die Tastenkürzel müssen verschieden sein.","Сочетания клавиш должны различаться.","Los atajos deben ser distintos.","Les raccourcis doivent être différents.","Os atalhos devem ser diferentes.","يجب أن يكون الاختصاران مختلفين.","दोनों शॉर्टकट अलग होने चाहिए।","两个快捷键必须不同。"],
        ["hotkeyBusy"]=["Bu kısayol başka bir uygulama tarafından kullanılıyor. Başka bir kombinasyon seç.","Another app already uses this shortcut. Choose a different combination.","Diese Tastenkombination wird bereits verwendet. Wähle eine andere.","Это сочетание уже используется. Выберите другое.","Otra app ya usa este atajo. Elige otro.","Ce raccourci est déjà utilisé. Choisissez-en un autre.","Este atalho já está em uso. Escolha outro.","هذا الاختصار مستخدم بالفعل. اختر مجموعة أخرى.","यह शॉर्टकट पहले से उपयोग में है। दूसरा चुनें।","此快捷键已被占用，请换一个。"],
        ["support"]=["Patreon'dan destek ol","Support on Patreon","Auf Patreon unterstützen","Поддержать на Patreon","Apoyar en Patreon","Soutenir sur Patreon","Apoiar no Patreon","ادعم عبر Patreon","Patreon पर समर्थन करें","在 Patreon 上支持"],
        ["browserFailed"]=["Tarayıcı açılamadı. Varsayılan tarayıcını kontrol et.","Could not open your default browser. Check your browser settings.","Browser konnte nicht geöffnet werden. Standardbrowser prüfen.","Не удалось открыть браузер. Проверьте браузер по умолчанию.","No se pudo abrir el navegador. Revisa el navegador predeterminado.","Impossible d’ouvrir le navigateur. Vérifiez le navigateur par défaut.","Não foi possível abrir o navegador. Verifique o navegador padrão.","تعذر فتح المتصفح. تحقق من المتصفح الافتراضي.","ब्राउज़र नहीं खुला। डिफ़ॉल्ट ब्राउज़र जाँचें।","无法打开浏览器，请检查默认浏览器设置。"],
        ["incompletePets"]=["Tamamlanmamış petler","Incomplete pets","Nicht maximierte Pets","Незавершённые питомцы","Pets sin completar","Pets non terminés","Pets incompletos","حيوانات غير مكتملة","अधूरे पेट","未满级宠物"],
        ["maxedPets"]=["MAX petler","Maxed pets","Maximierte Pets","Питомцы MAX","Pets al máximo","Pets au maximum","Pets no máximo","حيوانات بالمستوى الأقصى","अधिकतम स्तर के पेट","满级宠物"],
        ["start"]=["Başlat","Start","Starten","Старт","Iniciar","Démarrer","Iniciar","ابدأ","शुरू","开始"],
        ["stop"]=["Durdur","Stop","Stoppen","Стоп","Detener","Arrêter","Parar","إيقاف","रोकें","停止"],
        ["demo"]=["Demo","Demo","Demo","Демо","Demo","Démo","Demo","تجريبي","डेमो","演示"],
        ["add"]=["Pet ekle","Add pet","Pet hinzufügen","Добавить","Añadir pet","Ajouter","Adicionar","إضافة","पेट जोड़ें","添加宠物"],
        ["character"]=["Karakter bekleniyor","Waiting for character","Charakter wird erwartet","Ожидание персонажа","Esperando personaje","Personnage attendu","Aguardando personagem","بانتظار الشخصية","पात्र की प्रतीक्षा","等待角色"],
        ["empty"]=["Takibi başlatmak için bir pete vur veya yukarıdan seç","Hit a pet or select one above to start tracking","Triff ein Pet oder wähle oben eines aus","Атакуйте питомца или выберите его выше","Golpea un pet o selecciona uno arriba","Frappez un pet ou sélectionnez-en un ci-dessus","Ataque um pet ou selecione um acima","اضرب حيواناً أو اختر واحداً بالأعلى لبدء التتبع","ट्रैकिंग शुरू करने के लिए पेट पर वार करें या ऊपर चुनें","攻击宠物或在上方选择以开始追踪"],
        ["emptyHint"]=["Bir pet pinle veya soul kazan.","Pin a pet or gain a soul.","Pet anheften oder Seele erhalten.","Закрепите питомца или получите душу.","Fija un pet o consigue un alma.","Épinglez un pet ou gagnez une âme.","Fixe um pet ou obtenha uma alma.","ثبّت حيواناً أو احصل على روح.","पेट पिन करें या सोल पाएं।","固定宠物或获得灵魂。"],
        ["remaining"]=["{0} soul kaldı","{0} souls left","{0} Seelen übrig","Осталось душ: {0}","Faltan {0} almas","{0} âmes restantes","Restam {0} almas","{0} أرواح متبقية","{0} सोल बाकी","还需 {0} 灵魂"],
        ["last"]=["Son bilinen","Last known","Zuletzt bekannt","Последние данные","Último dato","Dernière valeur","Último registro","آخر قيمة","अंतिम ज्ञात","上次数据"],
        ["waiting"]=["Sayı bekleniyor","Waiting for count","Zahl erwartet","Ожидание данных","Esperando datos","Valeur attendue","Aguardando dados","بانتظار العدد","गिनती की प्रतीक्षा","等待数据"],
        ["complete"]=["Tamamlandı","Completed","Abgeschlossen","Завершено","Completado","Terminé","Concluído","مكتمل","पूर्ण","已完成"],
        ["target"]=["hedef","target","Ziel","цель","objetivo","cible","alvo","هدف","लक्ष्य","目标"],
        ["exact"]=["Synced","Synced","Synced","Synced","Synced","Synced","Synced","Synced","Synced","Synced"],
        ["uncertain"]=["Son bilinen · doğrula","Last known · verify","Letzter Stand · prüfen","Последние данные · проверьте","Último dato · verifica","Dernier état · vérifier","Último dado · verificar","آخر قيمة · تحقق","अंतिम ज्ञात · जाँचें","上次数据 · 请验证"],
        ["snapshot"]=["Kimlik / collection bekleniyor","Waiting for identity / collection","Identität / Sammlung erwartet","Ожидание персонажа / коллекции","Esperando identidad / colección","Identité / collection attendue","Aguardando identidade / coleção","بانتظار الهوية / المجموعة","पहचान / संग्रह की प्रतीक्षा","等待身份 / 收藏"],
        ["settings"]=["Ayarlar","Settings","Einstellungen","Настройки","Ajustes","Réglages","Configurações","الإعدادات","सेटिंग्स","设置"],
        ["opacity"]=["Opaklık","Opacity","Deckkraft","Непрозрачность","Opacidad","Opacité","Opacidade","العتامة","दृश्यता","不透明度"],
        ["height"]=["Yükseklik","Height","Höhe","Высота","Altura","Hauteur","Altura","الارتفاع","ऊँचाई","高度"],
        ["top"]=["Her zaman üstte","Always on top","Immer im Vordergrund","Поверх всех окон","Siempre encima","Toujours au-dessus","Sempre no topo","دائماً في الأعلى","हमेशा ऊपर","始终置顶"],
        ["lock"]=["Konumu kilitle","Lock position","Position sperren","Закрепить позицию","Bloquear posición","Verrouiller la position","Travar posição","تثبيت الموضع","स्थान लॉक","锁定位置"],
        ["unlock"]=["Konum kilidini aç","Unlock position","Position entsperren","Снять фиксацию","Desbloquear posición","Déverrouiller","Destravar posição","إلغاء التثبيت","स्थान अनलॉक","解锁位置"],
        ["language"]=["Dil ayarları","Language settings","Spracheinstellungen","Настройки языка","Idioma","Langue","Idioma","إعدادات اللغة","भाषा सेटिंग्स","语言设置"],
        ["logs"]=["Tanı logları","Diagnostics","Diagnose","Диагностика","Diagnóstico","Diagnostic","Diagnóstico","التشخيص","निदान","诊断日志"],
        ["done"]=["Tamam","Done","Fertig","Готово","Listo","Terminé","Concluir","تم","ठीक","完成"],
        ["search"]=["Pet adı veya ID ara…","Search pet name or ID…","Pet-Name oder ID suchen…","Имя питомца или ID…","Buscar nombre o ID…","Nom ou ID du pet…","Buscar nome ou ID…","اسم الحيوان أو الرقم…","पेट का नाम या ID…","搜索宠物名称或 ID…"],
        ["pets"]=["{0} pet","{0} pets","{0} Pets","Питомцев: {0}","{0} pets","{0} pets","{0} pets","{0} حيوانات","{0} पेट","{0} 只宠物"],
        ["noResults"]=["Eşleşen pet yok","No matching pets","Keine passenden Pets","Нет совпадений","Sin resultados","Aucun résultat","Nenhum resultado","لا نتائج","कोई पेट नहीं मिला","没有匹配的宠物"],
        ["pin"]=["Pinle / pini kaldır","Pin / unpin","Anheften / lösen","Закрепить / открепить","Fijar / soltar","Épingler / détacher","Fixar / soltar","تثبيت / إلغاء","पिन / हटाएं","固定 / 取消固定"],
        ["remove"]=["Kartı kaldır · ilerleme korunur","Remove card · progress stays","Karte entfernen · Fortschritt bleibt","Убрать карточку · прогресс сохранён","Quitar tarjeta · progreso guardado","Retirer · progression conservée","Remover · progresso salvo","إزالة البطاقة · التقدم محفوظ","कार्ड हटाएं · प्रगति सुरक्षित","移除卡片 · 保留进度"],
        ["minimize"]=["Küçült · tepsiden geri aç","Minimize · restore from tray","Minimieren · im Tray öffnen","Свернуть · открыть из трея","Minimizar · abrir desde bandeja","Réduire · ouvrir via la barre","Minimizar · restaurar na bandeja","تصغير · استعادة من الشريط","छोटा करें · ट्रे से खोलें","最小化 · 从托盘恢复"],
        ["close"]=["Kapat","Close","Schließen","Закрыть","Cerrar","Fermer","Fechar","إغلاق","बंद","关闭"],
        ["show"]=["Overlay'i göster","Show overlay","Overlay anzeigen","Показать окно","Mostrar overlay","Afficher","Mostrar overlay","عرض النافذة","ओवरले दिखाएं","显示悬浮窗"],
        ["characterRequired"]=["Önce karakter kimliği algılanmalı.","Detect your character first.","Zuerst muss der Charakter erkannt werden.","Сначала требуется определить персонажа.","Primero detecta el personaje.","Le personnage doit être détecté.","Detecte o personagem primeiro.","يجب تحديد الشخصية أولاً.","पहले पात्र की पहचान होनी चाहिए।","请先识别角色。"]
    };
    public static string Get(string language,string key,params object[] args)
    {
        var index=Array.FindIndex(Languages,l=>l.Code==language); if(index<0)index=0;
        var value=Strings.TryGetValue(key,out var entries)?entries[index]:key;
        return args.Length==0?value:string.Format(System.Globalization.CultureInfo.InvariantCulture,value,args);
    }
    public static bool IsSupported(string? language)=>Languages.Any(l=>l.Code==language);
    public static bool Complete=>Strings.Values.All(v=>v.Length==Languages.Length&&v.All(s=>!string.IsNullOrWhiteSpace(s)));
    public static string FontFamily(string language)
    {
#if UI_FONTS
        return FontAssets.BodyName(language);
#else
        return language switch { "zh"=>"Microsoft YaHei UI","hi"=>"Nirmala UI",_=>"Segoe UI" };
#endif
    }
}
