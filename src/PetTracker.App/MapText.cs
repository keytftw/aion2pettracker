namespace PetTracker.App;
public static class MapText
{
    private static readonly Dictionary<string,string[]> Texts=new()
    {
        ["showAll"]=["Tüm farmlanabilir konumları göster","Show all farmable locations","Alle farmbaren Orte anzeigen","Показать все места для фарма","Mostrar todos los lugares de farmeo","Afficher tous les lieux à farmer","Mostrar todos os locais de farm","إظهار جميع مواقع التجميع","सभी फ़ार्म स्थान दिखाएँ","显示所有可收集位置"],
        ["hideUnlocated"]=["Konumu olmayan petleri gizle","Hide pets without locations","Pets ohne Fundorte ausblenden","Скрыть питомцев без мест","Ocultar pets sin ubicaciones","Masquer les pets sans lieu","Ocultar pets sem locais","إخفاء الحيوانات بلا مواقع","बिना स्थान वाले पेट छिपाएँ","隐藏无位置的宠物"],
        ["loadFailed"]=["Harita görseli yüklenemedi.","The map image could not be loaded.","Das Kartenbild konnte nicht geladen werden.","Не удалось загрузить изображение карты.","No se pudo cargar la imagen del mapa.","Impossible de charger l’image de la carte.","Não foi possível carregar a imagem do mapa.","تعذر تحميل صورة الخريطة.","मानचित्र चित्र लोड नहीं हो सका।","无法加载地图图像。"],
        ["title"]=["Pet Atlası","Pet Atlas","Pet-Atlas","Атлас питомцев","Atlas de pets","Atlas des pets","Atlas de pets","أطلس الحيوانات","पेट एटलस","宠物地图"],
        ["farmable"]=["Farmlanabilir petler","Farmable pets","Farmbare Pets","Питомцы для фарма","Pets para farmear","Pets à farmer","Pets para farmar","حيوانات للتجميع","फ़ार्म करने योग्य पेट","可收集宠物"],
        ["region"]=["Bölge","Region","Region","Регион","Región","Région","Região","المنطقة","क्षेत्र","区域"],
        ["zoom"]=["Yakınlaştırma","Zoom","Zoom","Масштаб","Zoom","Zoom","Zoom","التكبير","ज़ूम","缩放"],
        ["focus"]=["Konumlara odaklan","Focus locations","Fundorte anzeigen","Показать места","Enfocar ubicaciones","Centrer les lieux","Focar locais","ركّز على المواقع","स्थानों पर जाएँ","聚焦位置"],
        ["locations"]=["{0} kayıtlı konum","{0} recorded locations","{0} gespeicherte Orte","Записанных мест: {0}","{0} ubicaciones registradas","{0} lieux enregistrés","{0} locais registrados","{0} مواقع مسجلة","{0} दर्ज स्थान","{0} 个记录位置"],
        ["noLocations"]=["Bu pet için kayıtlı harita konumu yok.","No recorded map locations for this pet.","Keine Kartenorte für dieses Pet gespeichert.","Для питомца нет записанных мест.","Este pet no tiene ubicaciones registradas.","Aucun lieu enregistré pour ce pet.","Não há locais registrados para este pet.","لا توجد مواقع مسجلة لهذا الحيوان.","इस पेट के लिए कोई दर्ज स्थान नहीं है।","此宠物暂无记录位置。"],
        ["noRegionLocations"]=["Bu bölgede kayıtlı konumu yok; başka bölge seçebilirsin.","No locations in this region; choose another region.","Keine Orte in dieser Region; andere Region wählen.","В этом регионе мест нет. Выберите другой.","No hay ubicaciones en esta región; elige otra.","Aucun lieu dans cette région ; choisissez-en une autre.","Sem locais nesta região; escolha outra.","لا توجد مواقع بهذه المنطقة؛ اختر منطقة أخرى.","इस क्षेत्र में स्थान नहीं हैं; दूसरा क्षेत्र चुनें।","此区域暂无位置，请选择其他区域。"],
        ["hint"]=["Sürükle · Mouse tekerleği ile yakınlaştır · Bir noktaya tıkla","Drag · Scroll to zoom · Click a marker","Ziehen · Mausrad zum Zoomen · Punkt anklicken","Перетаскивайте · Колесо для масштаба · Нажмите точку","Arrastra · Rueda para zoom · Pulsa un punto","Glisser · Molette pour zoomer · Cliquer un point","Arraste · Roda para zoom · Clique num ponto","اسحب · عجلة الماوس للتكبير · انقر نقطة","खींचें · स्क्रॉल से ज़ूम · बिंदु पर क्लिक करें","拖动 · 滚轮缩放 · 点击标记"],
        ["static"]=["Kayıtlı konumlar · canlı spawn bilgisi değildir","Recorded locations · not live spawn information","Gespeicherte Orte · keine Live-Spawns","Записанные места · не текущий спавн","Ubicaciones registradas · no spawns en vivo","Lieux enregistrés · pas des apparitions en direct","Locais registrados · não são spawns ao vivo","مواقع مسجلة · ليست معلومات ظهور مباشرة","दर्ज स्थान · लाइव स्पॉन जानकारी नहीं","记录位置 · 非实时刷新信息"],
        ["select"]=["Üstteki listeden bir pet seç.","Choose a pet from the list above.","Oben ein Pet auswählen.","Выберите питомца в списке выше.","Elige un pet de la lista superior.","Choisissez un pet dans la liste ci-dessus.","Escolha um pet na lista acima.","اختر حيواناً من القائمة أعلاه.","ऊपर की सूची से पेट चुनें।","请从上方列表选择宠物。"],
    };
    public static string Get(string language,string key,params object[] args)
    {
        var index=Array.FindIndex(UiText.Languages,entry=>entry.Code==language);var text=Texts[key][index<0?1:index];
        return args.Length==0?text:string.Format(System.Globalization.CultureInfo.InvariantCulture,text,args);
    }
    public static bool Complete=>Texts.Values.All(entries=>entries.Length==10&&entries.All(value=>!string.IsNullOrWhiteSpace(value)));
}
